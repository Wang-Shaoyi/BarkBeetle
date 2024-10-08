using BarkBeetle.Utils;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarkBeetle.RefinedGeometry
{
    internal class RefinedGeometryManager
    {
        public RefinedGeometry refinedGeometry;
        GH_Component component;

        public void SetComponent(GH_Component currentComponent)
        {
            component = currentComponent;
        }

        public void SetRefinedGeometry(double stripWidth, Surface surface, GH_Structure<GH_Point> pointsTree)
        {
            refinedGeometry = new RefinedGeometry(stripWidth, surface, pointsTree);
        }

        public void ProcessSkeletonPoints()
        {
            Surface surface = refinedGeometry.GetSurface();
            GH_Structure < GH_Point > pointsTree = refinedGeometry.GetPointsTree();
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
                    Point3d pt = ghPt.Value;
                    double u, v;
                    if (surface.ClosestPoint(pt, out u, out v))
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
            int rowCount = uvPointArray.GetLength(0);
            int colCount = uvPointArray.GetLength(1);

            // Add the points to the tree
            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < colCount; j++)
                {
                    Point3d point = uvPointArray[i, j].point;
                    GH_Path path = new GH_Path(i);
                    pointsTreeOut.Append(new GH_Point(point), path);
                }
            }

            refinedGeometry.Skeleton = pointsTreeOut;
        }

        public void ProcessExtendedSurface() { }
    }
}
