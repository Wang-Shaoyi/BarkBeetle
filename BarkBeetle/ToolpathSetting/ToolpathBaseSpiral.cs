using BarkBeetle.GeometriesPackage;
using BarkBeetle.Toolpath;
using BarkBeetle.Utils;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarkBeetle.ToolpathSetting
{
    internal class ToolpathBaseSpiral : ToolpathBase
    {
        public override string ToolpathType { get; set; } = "Spiral";
        public ToolpathBaseSpiral(GeometryPackage gP, Point3d seam, double pw) : base(gP, seam, pw) { }

        public override void ConstructToolpathBase()
        {
            CornerPts = ReplicatePtsToCorners();
            CornerPts = SpiralToolpathPtsProcess(CornerPts);
            List<Curve> spiralCurves = CreatSpiralToolpath(CornerPts);
            Curve = ToolpathContinuousStraight(spiralCurves);
            Curves = spiralCurves;
        }

        private Point3d[,,] ReplicatePtsToCorners()
        {
            // Set up all needed properties
            List<GH_Point> gH_Points = geometryPackage.Skeleton.SkeletonPoints;
            List<(int, int, int)> skeletonStructure = geometryPackage.Skeleton.SkeletonStructure;
            GH_Vector[,,] uvVectors = geometryPackage.Skeleton.UVVectors;

            // calculate number of circles
            double stripWidth = geometryPackage.StripWidth;
            int depthNum = (int)(stripWidth / (pathWidth * 2));

            // Set up an empty array
            Point3d[,,] ptArray3D = new Point3d[gH_Points.Count, depthNum, 4];

            // Go though all points in gh_Points
            for (int i = 0; i < gH_Points.Count; i++)
            {
                Point3d pt = gH_Points[i].Value;
                int u = skeletonStructure[i].Item1;
                int v = skeletonStructure[i].Item2;

                Vector3d mainVec = uvVectors[u, v, 0].Value;
                Vector3d subVec = uvVectors[u, v, 1].Value;

                double sin = PointDataUtils.SinOfTwoVectors(mainVec, subVec);
                mainVec = mainVec / sin;
                subVec = subVec / sin;

                // left-top
                for (int j = 1; j <= depthNum; j++)
                { ptArray3D[i, j - 1, 0] = new Point3d(pt + mainVec * pathWidth * (j - 0.5) + subVec * pathWidth * (j - 0.5)); }
                // left-bottom
                for (int j = 1; j <= depthNum; j++)
                { ptArray3D[i, j - 1, 1] = new Point3d(pt - mainVec * pathWidth * (j - 0.5) + subVec * pathWidth * (j - 0.5)); }
                // rght-top
                for (int j = 1; j <= depthNum; j++)
                { ptArray3D[i, j - 1, 2] = new Point3d(pt - mainVec * pathWidth * (j - 0.5) - subVec * pathWidth * (j - 0.5)); }
                // right-bottom
                for (int j = 1; j <= depthNum; j++)
                { ptArray3D[i, j - 1, 3] = new Point3d(pt + mainVec * pathWidth * (j - 0.5) - subVec * pathWidth * (j - 0.5)); }
            }

            return ptArray3D;
        }

        private Point3d[,,] SpiralToolpathPtsProcess(Point3d[,,] ptArray3D)
        {
            //Reminder: Point3d[,,] ptArray3D = new Point3d[gH_Points.Count, depthNum, 4];

            GH_Point[,] organizedPtsArray = geometryPackage.Skeleton.OrganizedPtsArray;
            GH_Vector[,,] uvVectors = geometryPackage.Skeleton.UVVectors; //[u,v,main/sub]
            List<(int, int, int)> skeletonStructure = geometryPackage.Skeleton.SkeletonStructure;

            int dim1 = ptArray3D.GetLength(0);
            int depthNum = ptArray3D.GetLength(1);
            Point3d[,,] newPointsArray = new Point3d[dim1, depthNum, 6];

            int[] du = { 0, -1, 0, 1 };
            int[] dv = { 1, 0, -1, 0 };
            int direction = 0;

            for (int i = 0; i < dim1; i++)
            {
                int u = skeletonStructure[i].Item1;
                int v = skeletonStructure[i].Item2;
                int turn = skeletonStructure[i].Item3;

                Point3d currentPt = organizedPtsArray[u, v].Value;

                Vector3d mainVec = uvVectors[u, v, 0].Value;
                Vector3d subVec = uvVectors[u, v, 1].Value;

                Vector3d neighTowardsVec;
                Vector3d neighSideVec;

                // find neighbor point
                if (turn == 0)
                {
                    Point3d neighbour = organizedPtsArray[u + du[direction], v + dv[direction]].Value;
                    Vector3d neighBetweenVec = neighbour - currentPt;
                    //find related vector
                    Vector3d neighMainVec = uvVectors[u + du[direction], v + dv[direction], 0].Value;
                    Vector3d neighSubVec = uvVectors[u + du[direction], v + dv[direction], 1].Value;

                    double sinMain = PointDataUtils.SinOfTwoVectors(neighMainVec, subVec);
                    double sinSub = PointDataUtils.SinOfTwoVectors(neighSubVec, subVec);

                    // Select the neighTowardsVec (the one that has smaller sin with sub
                    if (sinSub < sinMain)
                    {
                        neighTowardsVec = neighSubVec;
                        neighSideVec = neighMainVec;
                    }
                    else
                    {
                        neighTowardsVec = neighMainVec;
                        neighSideVec = neighSubVec;
                    }

                    if (Vector3d.VectorAngle(neighTowardsVec, subVec) < Math.PI / 2) neighTowardsVec.Reverse();
                    if (Vector3d.VectorAngle(neighSideVec, mainVec) > Math.PI / 2) neighSideVec.Reverse();

                    double sin = PointDataUtils.SinOfTwoVectors(neighTowardsVec, neighSideVec);
                    neighTowardsVec = neighTowardsVec / sin;
                    neighSideVec = neighSideVec / sin;

                    for (int j = 0; j < depthNum; j++)
                    {
                        // Keep previous points
                        newPointsArray[i, j, 0] = ptArray3D[i, j, 0];
                        newPointsArray[i, j, 1] = ptArray3D[i, j, 1];
                        newPointsArray[i, j, 2] = ptArray3D[i, j, 2];
                        newPointsArray[i, j, 3] = ptArray3D[i, j, 3];

                        // Move new points
                        Vector3d moveTowards = neighTowardsVec * pathWidth * (2 * depthNum - j - 0.5);
                        Vector3d moveSide = neighSideVec * pathWidth * (j + 0.5);
                        Point3d point4 = neighbour + moveTowards + moveSide;
                        Point3d point5 = neighbour + moveTowards - moveSide;

                        newPointsArray[i, j, 4] = point4;
                        newPointsArray[i, j, 5] = point5;
                    }
                }
                else
                {
                    direction = (direction + 1) % 4;
                    for (int j = 0; j < depthNum; j++)
                    {
                        // Keep previous points
                        newPointsArray[i, j, 0] = ptArray3D[i, j, 0];
                        newPointsArray[i, j, 1] = ptArray3D[i, j, 1];
                        newPointsArray[i, j, 2] = ptArray3D[i, j, 2];
                        newPointsArray[i, j, 3] = ptArray3D[i, j, 3];
                    }
                }
            }
            return newPointsArray;
        }

        private List<Curve> CreatSpiralToolpath(Point3d[,,] ptArray3D)
        {
            //Reminder: Point3d[,,] ptArray3D = new Point3d[gH_Points.Count, depthNum, 6];
            Surface surface = geometryPackage.ExtendedSurface;
            List<(int, int, int)> skeletonStructure = geometryPackage.Skeleton.SkeletonStructure;
            GH_Point[,] organizedPtsArray = geometryPackage.Skeleton.OrganizedPtsArray;

            // Create an empty list to save all the tool paths
            List<Curve> toolpathList = new List<Curve>();

            int count = ptArray3D.GetLength(0);
            int countLastLine = Math.Abs(organizedPtsArray.GetLength(0) - organizedPtsArray.GetLength(1));
            int depthNum = ptArray3D.GetLength(1);

            // For each toolpath circle
            for (int k = 0; k < depthNum; k++)
            {
                // Create an empty list to sort the points
                List<Point3d> toolpathPointList = new List<Point3d>();

                // For each point
                for (int i = 0; i < count; i++)
                {
                    if (i > count - 1 - countLastLine)
                    {
                        toolpathPointList.Insert(0, ptArray3D[i, k, 2]);
                        toolpathPointList.Insert(0, ptArray3D[i, k, 3]);
                        toolpathPointList.Add(ptArray3D[i, k, 1]);
                        toolpathPointList.Add(ptArray3D[i, k, 0]);
                    }
                    else
                    {
                        if (skeletonStructure[i].Item3 == 0)
                        {
                            // Put the points in order
                            toolpathPointList.Insert(0, ptArray3D[i, k, 2]);
                            toolpathPointList.Insert(0, ptArray3D[i, k, 3]);
                            toolpathPointList.Add(ptArray3D[i, k, 1]);
                            toolpathPointList.Add(ptArray3D[i, k, 5]);
                            toolpathPointList.Add(ptArray3D[i, k, 4]);
                            toolpathPointList.Add(ptArray3D[i, k, 0]);
                        }
                        if (skeletonStructure[i].Item3 == 1)
                        {
                            // Put the points in order
                            toolpathPointList.Insert(0, ptArray3D[i, k, 1]);
                            toolpathPointList.Insert(0, ptArray3D[i, k, 2]);
                            toolpathPointList.Insert(0, ptArray3D[i, k, 3]);
                            toolpathPointList.Add(ptArray3D[i, k, 0]);
                        }
                    }
                }

                List<Point3d> points3DOnSrf = new List<Point3d>();
                foreach (Point3d pt3d in toolpathPointList)
                {
                    double u, v;
                    surface.ClosestPoint(pt3d, out u, out v);
                    Point3d pt3dOnSurf = surface.PointAt(u, v);
                    points3DOnSrf.Add(pt3dOnSurf);
                }

                List<Curve> surfaceCurves = new List<Curve>();
                points3DOnSrf.Add(points3DOnSrf[0]); // Close curve

                for (int i = 0; i < points3DOnSrf.Count - 1; i++) // Generate curve by segments (will there be a better method?)
                {
                    Curve curve = surface.InterpolatedCurveOnSurface(new List<Point3d> { points3DOnSrf[i], points3DOnSrf[i + 1] }, 0.01);
                    surfaceCurves.Add(curve);
                }

                Curve[] surfaceCurve = Curve.JoinCurves(surfaceCurves, 0.01); // Join the segments
                toolpathList.Add(surfaceCurve[0]);
            }
            return toolpathList;
        }

        private Curve ToolpathContinuousIncline(List<Curve> spiralCurves)
        {
            Point3d cutPt = seamPt;
            List<Curve> curvesToConnect = new List<Curve>();
            bool firstCrv = true;

            foreach (Curve curve in spiralCurves)
            {
                // Find closest point
                double t;
                if (!curve.ClosestPoint(cutPt, out t))
                    return null;

                // Change curve starting point
                if (curve.IsClosed) curve.ChangeClosedCurveSeam(t);

                double curveLength = curve.GetLength();

                if (pathWidth >= curveLength) return null;

                // Get trim parameter
                double endTrimParameter;
                curve.LengthParameter(curveLength - pathWidth, out endTrimParameter);

                Curve finalCurve = curve.Trim(curve.Domain.Min, endTrimParameter);
                curvesToConnect.Add(finalCurve);

                //Add connect segement
                if (!firstCrv)
                {
                    Curve lineCurve = new LineCurve(new Line(cutPt, finalCurve.PointAtStart));
                    curvesToConnect.Add(lineCurve);
                }
                firstCrv = false;

                // Change Cut Pt
                cutPt = finalCurve.PointAtEnd;
            }

            Curve[] surfaceCurve = Curve.JoinCurves(curvesToConnect, 0.01); // Join the segments
            return surfaceCurve[0];
        }

        private Curve ToolpathContinuousStraight(List<Curve> spiralCurves)
        {
            Point3d cutPt = seamPt;
            List<Curve> curvesToConnect = new List<Curve>();
            bool firstCrv = true;
            bool change = true;
            Point3d lastStart = seamPt;

            foreach (Curve curve in spiralCurves)
            {
                // Find closest point
                double t;
                if (!curve.ClosestPoint(cutPt, out t))
                    return null;

                // Change curve starting point
                if (curve.IsClosed) curve.ChangeClosedCurveSeam(t);

                double curveLength = curve.GetLength();

                if (pathWidth >= curveLength) return null;

                Curve finalCurve = curve;
                double startTrimParameter;
                curve.LengthParameter(pathWidth, out startTrimParameter);
                finalCurve = curve.Trim(startTrimParameter, curve.Domain.Max);
                curvesToConnect.Add(finalCurve);

                Curve lineCurve = curve;
                //Add connect segement
                if (!firstCrv)
                {
                    if (change)
                    {
                        lineCurve = new LineCurve(new Line(cutPt, finalCurve.PointAtEnd));
                        change = !change;
                    }
                    else
                    {
                        lineCurve = new LineCurve(new Line(lastStart, finalCurve.PointAtStart));
                        change = !change;
                    }
                    curvesToConnect.Add(lineCurve);
                }
                firstCrv = false;

                // Change Cut Pt
                cutPt = finalCurve.PointAtEnd;
                lastStart = finalCurve.PointAtStart;
            }

            Curve[] surfaceCurve = Curve.JoinCurves(curvesToConnect, 0.01); // Join the segments
            return surfaceCurve[0];
        }
    }
}
