using BarkBeetle.Skeletons;
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
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static GH_IO.VersionNumber;

using BarkBeetle.Utils;

namespace BarkBeetle.GeometriesPackage
{
    internal class GeometryPackageManager
    {
        private GeometryPackage geometryPackage;
        public GeometryPackage GeometryPackage
        {
            get { return geometryPackage; }
        }
        GH_Component component;

        public void SetComponent(GH_Component currentComponent)
        {
            component = currentComponent;
        }

        // Construct the Refined Geometry in the manager
        public void SetGeometryPackage(double stripWidth, Surface surface, GH_Structure<GH_Point> pointsTree, string so )
        {
            //GH_Structure<GH_Point> organizedPtsTree = OrganizePtSequence(surface, pointsTree);
            Surface extendedSurface = ProcessExtendedSurface(stripWidth/2, stripWidth/2, surface);
            GH_Structure<GH_Point> closestPtTree = SurfaceClosestPtTree(extendedSurface, pointsTree);

            //Setup geometry package
            geometryPackage = new GeometryPackage(stripWidth, extendedSurface, closestPtTree, so);

            //setup skeleton crv (because we want to reference the surface so we do it here)
            geometryPackage.Skeleton.SkeletonCurve = ProcessSkeletonCurve();

            // uv curve and vectors
            List<List<GH_Curve>> uvCurves = new List<List<GH_Curve>>();
            GH_Vector[,,] uvVectors = null;
            GetUVCurvesAndPtUVVectors(ref uvCurves, ref uvVectors);
            geometryPackage.UVCurves = uvCurves;
            geometryPackage.Skeleton.UVVectors = uvVectors;
        }

        // Extend the surface outside edges smoothly
        public Surface ProcessExtendedSurface(double uWidth, double vWidth, Surface surface) 
        {
            // Setup for U direction
            List<Curve> extendedUCurves = new List<Curve>();
            Interval uDomain = surface.Domain(0);

            // Extend U direction curves
            int numUDivs = 20;
            for (int i = 0; i <= numUDivs; i++)
            {
                double uParam = uDomain.ParameterAt(i / (double)numUDivs);
                Curve uIsoCurve = surface.IsoCurve(1, uParam);
                // Extend
                Curve extendedUIsoCurve = uIsoCurve.Extend(CurveEnd.Both, vWidth, CurveExtensionStyle.Smooth);
                extendedUCurves.Add(extendedUIsoCurve);
            }

            // Rebuild for u direction
            Brep[] loftedBrepsU = Brep.CreateFromLoft(extendedUCurves, Point3d.Unset, Point3d.Unset, LoftType.Normal, false);
            Surface loftedSurfaceU = loftedBrepsU[0].Faces[0].ToNurbsSurface();

            /////////////////////////////////////////
            // Setup for V direction
            List<Curve> extendedVCurves = new List<Curve>();

            Interval vDomain = loftedSurfaceU.Domain(1);

            // Extend V direction curves
            int numVDivs = 20;
            for (int i = 0; i <= numVDivs; i++)
            {
                double vParam = vDomain.ParameterAt(i / (double)numVDivs);
                Curve vIsoCurve = loftedSurfaceU.IsoCurve(0, vParam);
                // Extend
                Curve extendedVIsoCurve = vIsoCurve.Extend(CurveEnd.Both, uWidth, CurveExtensionStyle.Smooth);
                extendedVCurves.Add(extendedVIsoCurve);
            }

            Brep[] loftedBrepsV = Brep.CreateFromLoft(extendedVCurves, Point3d.Unset, Point3d.Unset, LoftType.Normal, false);
            Surface loftedSurfaceV = loftedBrepsV[0].Faces[0].ToNurbsSurface();
            loftedSurfaceV.Transpose(true);

            return loftedSurfaceV;
        }

        // Pull points on the surface
        public GH_Structure<GH_Point> SurfaceClosestPtTree(Surface surface, GH_Structure<GH_Point> pointsTree)
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

        // Set Skeleton Curve from Skeleton Points
        /// This can only be called after skeleton(geometry package) is constructed
        public GH_Curve ProcessSkeletonCurve()
        {
            List<GH_Point> skeletonPoints = geometryPackage.Skeleton.SkeletonPoints;
            Surface surface = geometryPackage.ExtendedSurface;

            List<Point3d> pointGroup = new List<Point3d>();
            List<Curve> surfaceCurves = new List<Curve>();

            for (int i = 1; i < skeletonPoints.Count; i++)
            {
                Point3d previousPt = skeletonPoints[i - 1].Value;
                Point3d currentPt = skeletonPoints[i].Value;

                Curve curve = surface.InterpolatedCurveOnSurface(new List<Point3d> { previousPt, currentPt }, 0.01);
                surfaceCurves.Add(curve);
            }

            Curve[] surfaceCurve = Curve.JoinCurves(surfaceCurves, 0.01);
            return new GH_Curve(surfaceCurve[0]);
        }

        // Get the interpolated curves of the organized pts
        /// This can only be called after skeleton(geometry package) is constructed
        public void GetUVCurvesAndPtUVVectors(ref List<List<GH_Curve>> uvCurves, ref GH_Vector[,,] uvVectors)
        {
            Curve skeletonCrv = geometryPackage.Skeleton.SkeletonCurve.Value;
            GH_Structure<GH_Point> organizedPtsTree = geometryPackage.OrganizedPtsTree;
            organizedPtsTree.Simplify(GH_SimplificationMode.CollapseLeadingOverlaps);

            int uCount = organizedPtsTree.PathCount;
            int vCount = organizedPtsTree.Branches.Max(b => b.Count);
            uvVectors = new GH_Vector[uCount, vCount, 2];
            

            for (int i = 0; i < 2; i++)
            {
                List<GH_Curve> interpolatedCurvesDir = new List<GH_Curve>();
                uCount = organizedPtsTree.PathCount;
                vCount = organizedPtsTree.Branches.Max(b => b.Count);

                for (int u = 0; u < uCount; u++)
                {
                    // Get the curves
                    IList ghPoints = organizedPtsTree.get_Branch(organizedPtsTree.Paths[u]);
                    List<Point3d> points = ghPoints.Cast<GH_Point>().Select(p => p.Value).ToList();
                    Curve curve_dir = Curve.CreateInterpolatedCurve(points, 3);
                    interpolatedCurvesDir.Add(new GH_Curve(curve_dir));

                    for (int v = 0; v < points.Count; v++)
                    {
                        double t;
                        if (curve_dir.ClosestPoint(points[v], out t))
                        {
                            // Get the tangent at the closest point on the curve
                            Vector3d tangent = curve_dir.TangentAt(t);
                            tangent.Unitize();  // Normalize the tangent vector

                            if (i == 0)
                            {
                                uvVectors[u, v, 0] = new GH_Vector(tangent);
                            }
                            else
                            {
                                // Get directions
                                double r;
                                skeletonCrv.ClosestPoint(points[v], out r);
                                //Vector3d mainDir = PointDataUtils.GetTangentAtPoint(curve_dir, points[v]);
                                Vector3d mainDir = skeletonCrv.TangentAt(r + 1e-6);
                                Vector3d tangentPre = uvVectors[v, u, 0].Value;

                                double sinPre = PointDataUtils.SinOfTwoVectors(mainDir, tangentPre);
                                double sinThis = PointDataUtils.SinOfTwoVectors(mainDir, tangent);

                                if (sinPre < sinThis) // When the first direction governs
                                {
                                    if (PointDataUtils.CosOfTwoVectors(mainDir, tangentPre) < 0)
                                    {
                                        tangentPre.Reverse();
                                    }
                                    else tangent.Reverse();
                                    uvVectors[v, u, 0] = new GH_Vector(tangentPre);
                                    uvVectors[v, u, 1] = new GH_Vector(tangent);
                                }
                                else  // When the second direction governs
                                {
                                    if (PointDataUtils.CosOfTwoVectors(mainDir, tangent) < 0)
                                    {
                                        tangentPre.Reverse();
                                        tangent.Reverse();
                                    }
                                    uvVectors[v, u, 0] = new GH_Vector(tangent);
                                    uvVectors[v, u, 1] = new GH_Vector(tangentPre);
                                }
                            }
                        }
                    }
                }
                uvCurves.Add(interpolatedCurvesDir);
                organizedPtsTree = TreeHelper.FlipMatrixNoComp(organizedPtsTree);
            }
        }


        #region Not in use
        // Organize the point tree sequence according to surface uv
        public GH_Structure<GH_Point> OrganizePtSequence(Surface surface, GH_Structure<GH_Point> pointsTree)
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
