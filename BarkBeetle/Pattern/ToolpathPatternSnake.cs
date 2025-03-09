
using BarkBeetle.Network;
using BarkBeetle.Skeletons;
using BarkBeetle.Utils;
using Eto.Forms;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BarkBeetle.Pattern
{
    internal class ToolpathPatternSnake : ToolpathPattern
    {
        private double spacing;
        private int middleCrvOption;
        private List<double> depthList = new List<double>();
        private Point3d[,,] inAndOutCornerPts { get; set; }

        public ToolpathPatternSnake(SkeletonGraph sG, Point3d seam, double pw, double s, int middleCrvOption) : base(sG, seam, pw)
        {
            double stripWidth = sG.UVNetwork.StripWidth;
            if (stripWidth < pw * 6) return;
            spacing = s;
            this.middleCrvOption = middleCrvOption;
            ConstructToolpathPattern();
            
        }

        public override void ConstructToolpathPattern()
        {
            // Create the three reference curves
            inAndOutCornerPts = ReplicatePtsToCorners();
            CornerPtsList = new List<Point3d>();
            List<Curve> closedCrvs = CreatClosedReferenceCurves(inAndOutCornerPts);

            Curve insideCrv = closedCrvs[0];
            Curve outsideCrv = closedCrvs[1];
            Curve boundaryCrv = closedCrvs[2];
            if (middleCrvOption == 2)
            {
                insideCrv = closedCrvs[1];
                outsideCrv = closedCrvs[2];
                boundaryCrv = closedCrvs[3];
            }

            Curve snakeCrv = CreateSnakeCurve(insideCrv, outsideCrv, spacing);

            if (middleCrvOption == 1) 
            {
                BundleCurves = new List<Curve> { Skeleton.SkeletonMainCurve.Value, snakeCrv, boundaryCrv };
                SeamPt = Skeleton.SkeletonMainCurve.Value.PointAtEnd;
            }
            else if (middleCrvOption == 2)
            {
                BundleCurves = new List<Curve> { closedCrvs[0], snakeCrv, boundaryCrv };
            }
            else BundleCurves = new List<Curve> { snakeCrv, boundaryCrv };

            CoutinuousCurve = ToolpathContinuousStraight(BundleCurves);
        }

        private Point3d[,,] ReplicatePtsToCorners()
        {
            // Set up all needed properties
            BBPoint[,] bbPointArray = Skeleton.BBPointArray;

            int ptCount = Skeleton.SkeletonPtList.Count;

            // calculate number of circles
            double stripWidth = Skeleton.UVNetwork.StripWidth;
            double depthNum = (int)(stripWidth / (PathWidth * 2));

            int bundleCrvCount = 3;
            // set up middle curve option
            if (middleCrvOption == 1)
            {
                depthList = new List<double> { 0.5, depthNum - 2, depthNum - 1 };
            }
            else if (middleCrvOption == 2)
            {
                depthList = new List<double> { 0, 1, depthNum - 2, depthNum - 1 };
                bundleCrvCount = 4;
            }
            else depthList = new List<double> { 0, depthNum - 2, depthNum - 1 }; // no middle curve

            // Set up an empty array
            Point3d[,,] ptArray3D = new Point3d[ptCount, bundleCrvCount, 6];
            BBPoint curBBPoint = bbPointArray[0, 0];

            // Go though all points in gh_Points
            for (int i = 0; i < ptCount; i++)
            {
                Point3d pt = curBBPoint.CurrentPt3d;

                Vector3d vecMain = curBBPoint.VectorU;
                Vector3d vecSub = curBBPoint.VectorV;

                double sin = PointDataUtils.SinOfTwoVectors(vecMain, vecSub);

                vecMain = vecMain / sin;
                vecSub = vecSub / sin;

                for (int j = 0; j < bundleCrvCount; j++)
                {
                    Vector3d moveMain = vecMain * PathWidth * (depthList[j] + 0.5);
                    Vector3d moveSub = vecSub * PathWidth * (depthList[j] + 0.5);
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
                    for (int j = 0; j < bundleCrvCount; j++)
                    {
                        Vector3d moveTowards = neighTowardsVec * PathWidth * (2 * depthNum - depthList[j] - 0.5);
                        Vector3d moveSide = neighSideVec * PathWidth * (depthList[j] + 0.5);
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

        private List<Curve> CreatClosedReferenceCurves(Point3d[,,] ptArray3D)
        {
            Surface surface = BaseSrf;
            BBPoint[,] bbPointArray = Skeleton.BBPointArray;
            BBPoint curBBPoint = bbPointArray[0, 0];

            int vectorMainDirection = PointDataUtils.DetermineVectorDirection(curBBPoint.VectorU, curBBPoint.VectorV);

            // Create an empty list to save all the tool paths
            List<Curve> toolpathList = new List<Curve>();

            int count = Skeleton.SkeletonPtList.Count;

            int bundleCrvCount = 3;
            // set up middle curve option
            if (middleCrvOption == 2)
            {
                bundleCrvCount = 4;
            }

            // For each toolpath circle
            for (int k = 0; k < bundleCrvCount; k++)
            {
                // Create an empty list to sort the points
                List<Point3d> toolpathPointList = new List<Point3d>();
                curBBPoint = bbPointArray[0, 0];

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

                    else curBBPoint = bbPointArray[0, 0];

                    if (curBBPoint.IsNextIndexAssigned())
                    {
                        curBBPoint = BBPoint.FindByIndex(curBBPoint.NextIndex, bbPointArray);
                    }
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

        private Curve CreateSnakeCurve(Curve insideCrv, Curve outsideCrv, double density)
        {
            Surface surface = BaseSrf;
            List<Point3d> insideDiscontinuousPts = CurveUtils.GetDiscontinuityPoints(insideCrv, out List<Curve> insideSegments);
            List<Point3d> outsideDiscontinuousPts = CurveUtils.GetDiscontinuityPoints(outsideCrv, out List<Curve> outsideSegments);

            int count = insideDiscontinuousPts.Count;

            List<Point3d> ptsAll = new List<Point3d>();

            for (int i = 0; i < count; i++)
            {
                int previousIndex = (i == 0) ? count - 1 : i - 1;
                int nextIndex = (i == count - 1) ? 0 : i + 1;

                // Get points
                Point3d insideStart = insideDiscontinuousPts[i];
                Point3d insideEnd = insideDiscontinuousPts[nextIndex];
                Point3d insidePre = insideDiscontinuousPts[previousIndex];
                Point3d outsideStart = outsideDiscontinuousPts[i];
                Point3d outsideEnd = outsideDiscontinuousPts[nextIndex];
                Point3d outsidePre = outsideDiscontinuousPts[previousIndex];

                Curve outsideSeg = outsideSegments[i];
                Curve preOutsideSeg = outsideSegments[previousIndex];
                Curve insideSeg = insideSegments[i];
                Curve preInsideSeg = insideSegments[previousIndex];

                bool flipSequence = true;
                List<Point3d> outsideControlPts = new List<Point3d>();
                List<Point3d> insideControlPts = new List<Point3d>();

                int n = 0;

                if (CurveUtils.IsConvexPointOnCurve(insideCrv, insideStart))
                {
                    // Condition 1: both convex
                    if (CurveUtils.IsConvexPointOnCurve(insideCrv, insideEnd))
                    {
                        // Draw the control curves
                        outsideSeg.ClosestPoint(insideEnd, out double t1);
                        Curve outsideControlCrv = outsideSeg.Trim(new Interval(outsideSeg.Domain.Min, t1));

                        preOutsideSeg.ClosestPoint(insideStart, out double t2);
                        Point3d ptNew = preOutsideSeg.PointAt(t2);

                        ptNew = BrepUtils.GetClosestPointOnSurface(surface, ptNew);

                        Curve insideOtherSeg = surface.InterpolatedCurveOnSurface(new List<Point3d> { ptNew, insideStart }, 0.01);
                        Curve insideControlCrv = Curve.JoinCurves(new List<Curve> { insideOtherSeg,insideSeg }, 0.01)[0];

                        // odd segments in between
                        int maxCount = (int) Math.Round(Math.Max(outsideControlCrv.GetLength(), insideControlCrv.GetLength()) / (PathWidth + spacing));
                        if (maxCount % 2 == 0) maxCount -= 1;
                        n = Math.Max(maxCount,1);

                        outsideControlCrv.DivideByCount(n, true, out Point3d[] outsidePoints);
                        outsideControlPts =  new List<Point3d>(outsidePoints);

                        insideControlCrv.DivideByCount(n, true, out Point3d[] insidePoints);
                        insideControlPts = new List<Point3d>(insidePoints);

                        flipSequence = true;
                    }
                    // Condition 2: start convex, end not
                    else
                    {
                        // Draw the control curves
                        Curve outsideControlCrv = outsideSeg;

                        insideSeg.ClosestPoint(outsideEnd, out double t1);
                        Curve insideMidSeg = insideSeg.Trim(new Interval(insideSeg.Domain.Min, t1));

                        preOutsideSeg.ClosestPoint(insideStart, out double t2);
                        Point3d ptNew = preOutsideSeg.PointAt(t2);
                        ptNew = BrepUtils.GetClosestPointOnSurface(surface, ptNew);

                        Curve insideOtherSeg = surface.InterpolatedCurveOnSurface(new List<Point3d> { ptNew, insideStart }, 0.01);
                        Curve insideControlCrv = Curve.JoinCurves(new List<Curve> { insideOtherSeg, insideMidSeg }, 0.01)[0];

                        // even segments in between
                        int maxCount = (int)Math.Round(Math.Max(outsideControlCrv.GetLength(), insideControlCrv.GetLength()) / (PathWidth + spacing));
                        if (maxCount % 2 == 1) maxCount -= 1;
                        n = Math.Max(maxCount, 2);

                        outsideControlCrv.DivideByCount(n, true, out Point3d[] outsidePoints);
                        outsideControlPts = new List<Point3d>(outsidePoints);

                        insideControlCrv.DivideByCount(n, true, out Point3d[] insidePoints);
                        insideControlPts = new List<Point3d>(insidePoints);

                        flipSequence = true;
                    }

                }
                else
                {
                    // Condition 3: end convex, start not
                    if (CurveUtils.IsConvexPointOnCurve(insideCrv, insideEnd))
                    {
                        // Draw the control curves
                        Curve insideControlCrv = insideSeg;

                        outsideSeg.ClosestPoint(insideEnd, out double t1);
                        Curve outsideMidSeg = outsideSeg.Trim(new Interval(outsideSeg.Domain.Min, t1));

                        preInsideSeg.ClosestPoint(outsideStart, out double t2);
                        Point3d ptNew = preInsideSeg.PointAt(t2);
                        ptNew = BrepUtils.GetClosestPointOnSurface(surface, ptNew);

                        Curve outsideOtherSeg = surface.InterpolatedCurveOnSurface(new List<Point3d> { ptNew, outsideStart }, 0.01);
                        Curve outsideControlCrv = Curve.JoinCurves(new List<Curve> { outsideOtherSeg, outsideMidSeg }, 0.01)[0];

                        // even segments in between
                        int maxCount = (int)Math.Round(Math.Max(outsideControlCrv.GetLength(), insideControlCrv.GetLength()) / (PathWidth + spacing));
                        if (maxCount % 2 == 1) maxCount -= 1;
                        n = Math.Max(maxCount, 2);

                        outsideControlCrv.DivideByCount(n, true, out Point3d[] outsidePoints);
                        outsideControlPts = new List<Point3d>(outsidePoints);

                        insideControlCrv.DivideByCount(n, true, out Point3d[] insidePoints);
                        insideControlPts = new List<Point3d>(insidePoints);

                        flipSequence = false;
                    }
                    // Condition 4: both not convex
                    else
                    {
                        // Draw the control curves
                        insideSeg.ClosestPoint(outsideEnd, out double t1);
                        Curve insideControlCrv = insideSeg.Trim(new Interval(insideSeg.Domain.Min, t1));

                        preInsideSeg.ClosestPoint(outsideStart, out double t2);
                        Point3d ptNew = preInsideSeg.PointAt(t2);

                        ptNew = BrepUtils.GetClosestPointOnSurface(surface, ptNew);

                        Curve outsideOtherSeg = surface.InterpolatedCurveOnSurface(new List<Point3d> { ptNew, outsideStart }, 0.01);
                        Curve outsideControlCrv = Curve.JoinCurves(new List<Curve> { outsideOtherSeg, outsideSeg }, 0.01)[0];

                        // odd segments in between
                        int maxCount = (int)Math.Round(Math.Max(outsideControlCrv.GetLength(), insideControlCrv.GetLength()) / (PathWidth + spacing));
                        if (maxCount % 2 == 0) maxCount -= 1;
                        n = Math.Max(maxCount, 1);

                        outsideControlCrv.DivideByCount(n, true, out Point3d[] outsidePoints);
                        outsideControlPts = new List<Point3d>(outsidePoints);

                        insideControlCrv.DivideByCount(n, true, out Point3d[] insidePoints);
                        insideControlPts = new List<Point3d>(insidePoints);

                        flipSequence = false;
                    }
                }

                for (int j = 0; j < n; j++)
                {
                    if (flipSequence)
                    {
                        ptsAll.Add(BrepUtils.GetClosestPointOnSurface(surface, insideControlPts[j]));
                        ptsAll.Add(BrepUtils.GetClosestPointOnSurface(surface, outsideControlPts[j]));
                    }
                    else
                    {
                        ptsAll.Add(BrepUtils.GetClosestPointOnSurface(surface, outsideControlPts[j]));
                        ptsAll.Add(BrepUtils.GetClosestPointOnSurface(surface, insideControlPts[j]));
                    }
                    flipSequence = !flipSequence;
                }
            }

            List<Curve> snakeCurves = new List<Curve>();
            CornerPtsList.AddRange(ptsAll);

            ptsAll.Add(ptsAll[0]);

            Polyline polyline = new Polyline(ptsAll);

            Curve[] snakeCurve = Curve.JoinCurves(snakeCurves, 10); // Join the segments

            return polyline.ToNurbsCurve();
        }

        public override ToolpathPattern DeepCopy()
        {
            // New instance
            var copy = new ToolpathPatternSnake(this.Skeleton, this.SeamPt, this.PathWidth, this.spacing, this.middleCrvOption);

            return copy;
        }



    }
        
}
