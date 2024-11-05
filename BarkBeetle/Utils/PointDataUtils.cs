using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarkBeetle.Utils
{
    internal class PointDataUtils
    {
        public static double SinOfTwoVectors(Vector3d v1, Vector3d v2)
        {
            double angleRadians = Vector3d.VectorAngle(v1, v2);
            double sinValue = Math.Sin(angleRadians);
            return sinValue;
        }

        public static double CosOfTwoVectors(Vector3d v1, Vector3d v2)
        {
            double angleRadians = Vector3d.VectorAngle(v1, v2);
            double sinValue = Math.Cos(angleRadians);
            return sinValue;
        }

        public static int DetermineVectorDirection(Vector3d A, Vector3d B)
        {
            double crossProduct = A.X * B.Y - A.Y * B.X;

            if (crossProduct > 0)
            {
                return 1;
            }
            else if (crossProduct < 0)
            {
                return -1;
            }
            else
            {
                return 0; 
            }
        }

        public static int FindSpiralLastLineCount(int cnt1, int cnt2)
        {
            int countRemain = cnt1 * cnt2; // 总点数

            int index1 = 1, index2 = 0;
            cnt2 -= 1;

            while (countRemain > 0)
            {
                countRemain -= cnt1 * index1 + cnt2 * index2;

                cnt1 = cnt1 - index1;
                cnt2 = cnt2 - index2;

                int temp = index1;
                index1 = index2;
                index2 = temp;

                if (countRemain <= 0 || cnt1 == 0 || cnt2 == 0) break;
            }

            return countRemain;
        }

        public static List<Point3d> GetExplodedCurveVertices(Curve curve)
        {
            List<Point3d> vertices = new List<Point3d>();
            vertices.Add(curve.PointAtStart);
            // 检查曲线是否是 PolyCurve 类型
            if (curve is PolyCurve polyCurve)
            {
                // Explode PolyCurve 并获取每段的起点和终点
                for (int i = 0; i < polyCurve.SegmentCount; i++)
                {
                    Curve segment = polyCurve.SegmentCurve(i);

                    double halfLength = segment.GetLength() / 2.0;
                    double midParameter;
                    segment.LengthParameter(halfLength, out midParameter);
                    vertices.Add(segment.PointAt(midParameter));

                    vertices.Add(segment.PointAtEnd);
                }
            }
            else
            {
                double halfLength = curve.GetLength() / 2.0;
                double midParameter;
                curve.LengthParameter(halfLength, out midParameter);
                vertices.Add(curve.PointAt(midParameter));
                vertices.Add(curve.PointAtEnd);
            }

            return vertices;
        }

        // Pull points on the surface
        public static GH_Structure<GH_Point> SurfaceClosestPtTree(Surface surface, GH_Structure<GH_Point> pointsTree)
        {
            GH_Structure<GH_Point> closestPtTree = new GH_Structure<GH_Point>();

            foreach (GH_Path path in pointsTree.Paths)
            {
                // Get points under current path
                IList ghPoints = pointsTree.get_Branch(path);
                List<GH_Point> closestPoints = new List<GH_Point>();

                // Go through and pull all the points on the surface
                foreach (GH_Point ghPoint in ghPoints.Cast<GH_Point>())
                {
                    Point3d point = ghPoint.Value;

                    double u, v;
                    if (surface.ClosestPoint(point, out u, out v))
                    {
                        Point3d closestPt = surface.PointAt(u, v);
                        closestPoints.Add(new GH_Point(closestPt));
                    }
                }
                // Add the point to the same path
                closestPtTree.AppendRange(closestPoints, path);
            }
            return closestPtTree;
        }

        #region Not really in use
        // Organize the point tree sequence according to surface uv
        public static GH_Structure<GH_Point> OrganizePtSequence(Surface surface, GH_Structure<GH_Point> pointsTree, GH_Component component)
        {
            //Surface surface = refinedGeometry.GetSurface();
            //GH_Structure < GH_Point > pointsTree = refinedGeometry.GetPointsTree();
            bool needFlip = false;

            // 1. Check if the tree needs to flip
            // Get surface uv direction at (0,0)
            Plane frame;
            surface.FrameAt(0, 0, out frame);
            Vector3d uSurface = frame.XAxis;
            Vector3d vSurface = frame.YAxis;

            // Get the first point of the tree
            GH_Path firstBranchPath = pointsTree.Paths[0];
            IList firstBranch = pointsTree.get_Branch(firstBranchPath);
            Point3d firstPoint = ((GH_Point)firstBranch[0]).Value;
            double uFirst, vFirst;
            surface.ClosestPoint(firstPoint, out uFirst, out vFirst);

            // Get the second point of the tree
            GH_Path secondBranchPath = pointsTree.Paths[1];
            IList secondBranch = pointsTree.get_Branch(secondBranchPath);
            Point3d secondPoint = ((GH_Point)secondBranch[0]).Value;

            // Calculate angle
            Vector3d uNewFirstPt = secondPoint - firstPoint;
            double uuAngle = Vector3d.VectorAngle(uNewFirstPt, uSurface);
            double uvAngle = Vector3d.VectorAngle(uNewFirstPt, vSurface);
            double uuAngleMin = Math.Min(uuAngle, Math.PI - uuAngle);
            double uvAngleMin = Math.Min(uvAngle, Math.PI - uvAngle);
            if (uuAngleMin > uvAngleMin) { needFlip = true; }

            // Flip if needed
            if (needFlip) pointsTree = TreeHelper.FlipMatrix(pointsTree, component);

            // 2. Turn GH_Structure to a 2D array
            List<int> treeSize = TreeHelper.GetTreeLayerLengths(pointsTree, component);
            int branch1Size = treeSize[0];
            int branch2Size = treeSize[1];

            // Initialize 2D array
            (double u, double v, Point3d point)[,] uvPointArray = new (double u, double v, Point3d point)[branch2Size, branch1Size];

            // Store all points and their UV in the array, and get the domain of these points
            double uMin = double.MaxValue;
            double vMin = double.MaxValue;
            int dir1 = 0;
            foreach (GH_Path path in pointsTree.Paths)
            {
                int dir2 = 0;
                foreach (GH_Point ghPt in pointsTree.get_Branch(path))
                {
                    Point3d pt = ghPt.Value; // Turn GH_Point to Point3d
                    double u, v;
                    if (surface.ClosestPoint(pt, out u, out v)) //surface.ClosestPoint can only use GH_Point
                    {
                        uvPointArray[dir1, dir2] = (u, v, pt);
                        if (u < uMin) uMin = u;
                        if (v < vMin) vMin = v;
                    }
                    dir2++;
                }
                dir1++;
            }

            // 3. Resort the sequence of the points to meet surface UV direction
            int dir1Count = uvPointArray.GetLength(0);
            int dir2Count = uvPointArray.GetLength(1);
            if (uFirst == uMin && vFirst == vMin)
            {
                // Check flip
                Console.WriteLine("1");
            }
            else if (uFirst != uMin && vFirst != vMin)
            {
                // Reverse u and v
                for (int i = 0; i < dir1Count / 2; i++)
                {
                    for (int j = 0; j < dir2Count / 2; j++)
                    {
                        var temp = uvPointArray[i, j];
                        uvPointArray[i, j] = uvPointArray[dir1Count - 1 - i, dir2Count - 1 - j];
                        uvPointArray[dir1Count - 1 - i, dir2Count - 1 - j] = temp;
                    }
                }
                Console.WriteLine("2");
            }
            else if (uFirst != uMin && vFirst == vMin)
            {
                // Reverse u
                for (int i = 0; i < dir1Count / 2; i++)
                {
                    for (int j = 0; j < dir2Count; j++)
                    {
                        var temp = uvPointArray[i, j];
                        uvPointArray[i, j] = uvPointArray[dir1Count - 1 - i, j];
                        uvPointArray[dir1Count - 1 - i, j] = temp;
                    }
                }
                Console.WriteLine("3");
            }
            else if (uFirst == uMin && vFirst != vMin)
            {
                // Reverse v
                for (int i = 0; i < dir1Count; i++)
                {
                    for (int j = 0; j < dir2Count / 2; j++)
                    {
                        var temp = uvPointArray[i, j];
                        uvPointArray[i, j] = uvPointArray[i, dir2Count - 1 - j];
                        uvPointArray[i, dir2Count - 1 - j] = temp;
                    }
                }
                Console.WriteLine("4");
            }
            else { Console.WriteLine("None"); }

            // 4. Turn back to GH_Structure
            GH_Structure<GH_Point> pointsTreeOut = new GH_Structure<GH_Point>();

            // Get the new dimensions
            int uCount = uvPointArray.GetLength(0);
            int vCount = uvPointArray.GetLength(1);

            // Add the points to the tree
            for (int i = 0; i < uCount; i++)
            {
                for (int j = 0; j < vCount; j++)
                {
                    Point3d point = uvPointArray[i, j].point;
                    GH_Path path = new GH_Path(i);
                    pointsTreeOut.Append(new GH_Point(point), path);// Turn back to GH_Point when finished
                }
            }

            return pointsTreeOut;
        }
        #endregion
    }


}
