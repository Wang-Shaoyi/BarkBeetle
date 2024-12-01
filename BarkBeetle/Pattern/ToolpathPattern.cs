using Grasshopper.Kernel.Geometry;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BarkBeetle.Utils;
using BarkBeetle.Skeletons;
using Rhino.Collections;

namespace BarkBeetle.Pattern
{
    internal abstract class ToolpathPattern
    {
        // 0 Skeleton
        private SkeletonGraph skeleton { get; set; }
        public SkeletonGraph Skeleton
        {
            get { return skeleton; }
        }

        // 1 Pattern bundle curves
        private List<Curve> bundleCurves { get; set; }
        public List<Curve> BundleCurves
        {
            get { return bundleCurves; }
            set { bundleCurves = value; }
        }

        // 2 Pattern continuous curves
        private Curve coutinuousCurve { get; set; }
        public Curve CoutinuousCurve
        {
            get { return coutinuousCurve; }
            set { coutinuousCurve = value; }
        }

        // 3 Corner pt list
        private List<Point3d> cornerPtsList { get; set; }
        public List<Point3d> CornerPtsList
        {
            get { return cornerPtsList; }
            set { cornerPtsList = value; }
        }

        // 4 Seam points
        private Point3d seamPt { get; set; }
        public Point3d SeamPt
        {
            get { return seamPt; }
        }

        // 5 Path width
        private double pathWidth { get; set; }
        public double PathWidth
        {
            get { return pathWidth; }
        }

        public ToolpathPattern(SkeletonGraph sG, Point3d seam, double pw) 
        {
            pathWidth = pw;
            seamPt = seam;
            skeleton = sG;
        }

        public abstract void ConstructToolpathPattern();

        public Curve ToolpathContinuousIncline(List<Curve> spiralCurves)
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

        public Curve ToolpathContinuousStraight(List<Curve> spiralCurves)
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
