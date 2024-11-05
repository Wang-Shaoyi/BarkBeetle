
using BarkBeetle.Network;
using BarkBeetle.Skeletons;
using BarkBeetle.Utils;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BarkBeetle.Pattern
{
    internal class ToolpathPatterhSpiral : ToolpathPattern
    {
        public ToolpathPatterhSpiral(SkeletonGraph sG, Point3d seam, double pw) : base(sG, seam, pw) { }

        public override void ConstructToolpathPattern()
        {
            CornerPts = ReplicatePtsToCorners();
            BundleCurves = CreatSpiralToolpath(CornerPts);
            CoutinuousCurve = ToolpathContinuousStraight(BundleCurves);
        }

        private Point3d[,,] ReplicatePtsToCorners()
        {
            // Set up all needed properties
            BBPoint[,] bbPointArray = Skeleton.BBPointArray;

            int uCnt = bbPointArray.GetLength(0);
            int vCnt = bbPointArray.GetLength(1);
            
            // calculate number of circles
            double stripWidth = Skeleton.UVNetwork.StripWidth;
            int depthNum = (int)(stripWidth / (PathWidth * 2));

            // Set up an empty array
            Point3d[,,] ptArray3D = new Point3d[uCnt * vCnt, depthNum, 6];
            BBPoint curBBPoint = bbPointArray[0, 0];

            // Go though all points in gh_Points
            for (int i = 0; i < uCnt * vCnt; i++)
            {
                Point3d pt = curBBPoint.CurrentPt3d;

                Vector3d vecMain = curBBPoint.VectorU;
                Vector3d vecSub = curBBPoint.VectorV;

                double sin = PointDataUtils.SinOfTwoVectors(vecMain, vecSub);

                vecMain = vecMain / sin;
                vecSub = vecSub / sin;

                for (int j = 0; j < depthNum; j++)
                {
                    Vector3d moveMain = vecMain * PathWidth * (j + 0.5);
                    Vector3d moveSub = vecSub * PathWidth * (j + 0.5);
                    ptArray3D[i, j, 0] = new Point3d(pt + moveMain + moveSub);// left-top
                    ptArray3D[i, j, 1] = new Point3d(pt - moveMain + moveSub);// left-bottom
                    ptArray3D[i, j, 2] = new Point3d(pt - moveMain - moveSub);// rght-top
                    ptArray3D[i, j, 3] = new Point3d(pt + moveMain - moveSub);// right-bottom
                }

                // Two points on branch
                if (curBBPoint.IsBranchIndexAssigned())
                {
                    BBPoint branchBBPt = BBPoint.FindByIndex(curBBPoint.BranchIndex, bbPointArray);
                    Point3d ptBranch = branchBBPt.CurrentPt3d;

                    Vector3d vecBranch1 = branchBBPt.VectorU;
                    Vector3d vecBranch2 = branchBBPt.VectorV;

                    Vector3d neighTowardsVec;
                    Vector3d neighSideVec;

                    double sin1 = PointDataUtils.SinOfTwoVectors(vecBranch1, vecSub);
                    double sin2 = PointDataUtils.SinOfTwoVectors(vecBranch2, vecSub);

                    // Select the neighTowardsVec (the one that has smaller sin with sub
                    if (sin1 < sin2)
                    {
                        neighTowardsVec = vecBranch1;
                        neighSideVec = vecBranch2;
                    }
                    else
                    {
                        neighTowardsVec = vecBranch2;
                        neighSideVec = vecBranch1;
                    }

                    if (Vector3d.VectorAngle(neighTowardsVec, vecSub) < Math.PI / 2) neighTowardsVec.Reverse();
                    if (Vector3d.VectorAngle(neighSideVec, vecMain) > Math.PI / 2) neighSideVec.Reverse();

                    double sinBranch = PointDataUtils.SinOfTwoVectors(neighTowardsVec, neighSideVec);
                    neighTowardsVec = neighTowardsVec / sinBranch;
                    neighSideVec = neighSideVec / sinBranch;

                    // Move new points
                    for (int j = 0; j < depthNum; j++)
                    {
                        Vector3d moveTowards = neighTowardsVec * PathWidth * (2 * depthNum - j - 0.5);
                        Vector3d moveSide = neighSideVec * PathWidth * (j + 0.5);
                        ptArray3D[i, j, 4] = ptBranch + moveTowards + moveSide;
                        ptArray3D[i, j, 5] = ptBranch + moveTowards - moveSide;
                    }
                }

                if (curBBPoint.IsNextIndexAssigned())
                {
                    curBBPoint = BBPoint.FindByIndex(curBBPoint.NextIndex, bbPointArray);
                }
                else break;
            }
            return ptArray3D;
        }

        private List<Curve> CreatSpiralToolpath(Point3d[,,] ptArray3D)
        {
            Surface surface = Skeleton.UVNetwork.ExtendedSurface;
            BBPoint[,] bbPointArray = Skeleton.BBPointArray;
            BBPoint curBBPoint = bbPointArray[0, 0];

            int vectorMainDirection = PointDataUtils.DetermineVectorDirection(curBBPoint.VectorU, curBBPoint.VectorV);

            // Create an empty list to save all the tool paths
            List<Curve> toolpathList = new List<Curve>();

            int cnt1 = bbPointArray.GetLength(0);
            int cnt2 = bbPointArray.GetLength(1);

            int count = ptArray3D.GetLength(0);

            int countLastLine = PointDataUtils.FindSpiralLastLineCount(cnt1, cnt2);
            int depthNum = ptArray3D.GetLength(1);

            // For each toolpath circle
            for (int k = 0; k < depthNum; k++)
            {
                
                // Create an empty list to sort the points
                List<Point3d> toolpathPointList = new List<Point3d>();

                ////////////Add points with sequence///////////////
                // For each point
                for (int i = 0; i < count; i++)
                {
                    bool flipSeqence = (vectorMainDirection != PointDataUtils.DetermineVectorDirection(curBBPoint.VectorU, curBBPoint.VectorV));

                    if (curBBPoint.TurningType == 0)
                    {
                        if (flipSeqence)
                        {
                            // Put the points in order
                            toolpathPointList.Add(ptArray3D[i, k, 2]);
                            toolpathPointList.Add(ptArray3D[i, k, 3]);
                            toolpathPointList.Insert(0, ptArray3D[i, k, 1]);
                            if (curBBPoint.IsBranchIndexAssigned())
                            {
                                toolpathPointList.Insert(0, ptArray3D[i, k, 5]);
                                toolpathPointList.Insert(0, ptArray3D[i, k, 4]);
                            }
                            toolpathPointList.Insert(0, ptArray3D[i, k, 0]);
                        }
                        else
                        {
                            // Put the points in order
                            toolpathPointList.Insert(0, ptArray3D[i, k, 2]);
                            toolpathPointList.Insert(0, ptArray3D[i, k, 3]);
                            toolpathPointList.Add(ptArray3D[i, k, 1]);
                            if (curBBPoint.IsBranchIndexAssigned())
                            {
                                toolpathPointList.Add(ptArray3D[i, k, 5]);
                                toolpathPointList.Add(ptArray3D[i, k, 4]);
                            }
                            toolpathPointList.Add(ptArray3D[i, k, 0]);
                        }
                    }
                    else if (curBBPoint.TurningType == 1)
                    {
                        if (flipSeqence)
                        {
                            // Put the points in order
                            toolpathPointList.Add(ptArray3D[i, k, 3]);
                            toolpathPointList.Insert(0, ptArray3D[i, k, 2]);
                            toolpathPointList.Insert(0, ptArray3D[i, k, 1]);
                            if (curBBPoint.IsBranchIndexAssigned())
                            {
                                toolpathPointList.Insert(0, ptArray3D[i, k, 5]);
                                toolpathPointList.Insert(0, ptArray3D[i, k, 4]);
                            }
                            toolpathPointList.Insert(0, ptArray3D[i, k, 0]);
                        }
                        else
                        {
                            // Put the points in order
                            toolpathPointList.Insert(0, ptArray3D[i, k, 1]);
                            toolpathPointList.Insert(0, ptArray3D[i, k, 2]);
                            toolpathPointList.Insert(0, ptArray3D[i, k, 3]);
                            toolpathPointList.Add(ptArray3D[i, k, 0]);
                        }
                    }
                    else if (curBBPoint.TurningType == -1)
                    {
                        if (flipSeqence)
                        {
                            toolpathPointList.Insert(0, ptArray3D[i, k, 0]);
                            toolpathPointList.Add(ptArray3D[i, k, 1]);
                            toolpathPointList.Add(ptArray3D[i, k, 2]);
                            toolpathPointList.Add(ptArray3D[i, k, 3]);
                        }
                        else
                        {
                            // Put the points in order
                            toolpathPointList.Insert(0, ptArray3D[i, k, 3]);
                            toolpathPointList.Add(ptArray3D[i, k, 2]);
                            toolpathPointList.Add(ptArray3D[i, k, 1]);
                            if (curBBPoint.IsBranchIndexAssigned())
                            {
                                toolpathPointList.Add(ptArray3D[i, k, 5]);
                                toolpathPointList.Add(ptArray3D[i, k, 4]);
                            }
                            toolpathPointList.Add(ptArray3D[i, k, 0]);
                        }
                    }

                    if (curBBPoint.IsNextIndexAssigned())
                    {
                        curBBPoint = BBPoint.FindByIndex(curBBPoint.NextIndex, bbPointArray);
                    }
                    else curBBPoint = bbPointArray[0, 0];
                }

                ///////////////////////Draw Curve///////////////////
                // Pull all points on surface
                List<Point3d> points3DOnSrf = new List<Point3d>();
                foreach (Point3d pt3d in toolpathPointList)
                {
                    double u, v;
                    surface.ClosestPoint(pt3d, out u, out v);
                    Point3d pt3dOnSurf = surface.PointAt(u, v);
                    points3DOnSrf.Add(pt3dOnSurf);
                }

                // Initialize for curve drawing
                List<Curve> surfaceCurves = new List<Curve>();
                points3DOnSrf.Add(points3DOnSrf[0]); // Close curve

                // Draw curve
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
            Point3d cutPt = SeamPt;
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

                if (PathWidth >= curveLength) return null;

                // Get trim parameter
                double endTrimParameter;
                curve.LengthParameter(curveLength - PathWidth, out endTrimParameter);

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
            Point3d cutPt = SeamPt;
            List<Curve> curvesToConnect = new List<Curve>();
            bool firstCrv = true;
            bool change = true;
            Point3d lastStart = SeamPt;

            foreach (Curve curve in spiralCurves)
            {
                // Find closest point
                double t;
                if (!curve.ClosestPoint(cutPt, out t))
                    return null;

                // Change curve starting point
                if (curve.IsClosed) curve.ChangeClosedCurveSeam(t);

                double curveLength = curve.GetLength();

                if (PathWidth >= curveLength) return null;

                Curve finalCurve = curve;
                double startTrimParameter;
                curve.LengthParameter(PathWidth, out startTrimParameter);
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
