using BarkBeetle.CompsToolpath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;
using System.Security.Cryptography;

namespace BarkBeetle.ToolpathSetting
{
    internal class ToolpathUtils
    {
        public static Curve FilletToolpathBaseOnSurface(Curve curve, double r, Surface surface)
        {
            // 1. Get discontinuouty
            List<double> discontinuities = new List<double>();

            double tStart = curve.Domain.Min;
            double tEnd = curve.Domain.Max;

            double t;
            while (curve.GetNextDiscontinuity(Continuity.C1_locus_continuous, tStart, tEnd, out t))
            {
                discontinuities.Add(t);
                tStart = t;
            }

            if (discontinuities.Count > 2)
            {
                discontinuities.RemoveAt(discontinuities.Count - 1);
            }

            // 2 Get tangent on plusEpsilonList
            double epsilon = 1e-6;
            List<Vector3d> plusEpsilonTangents = new List<Vector3d>();
            List<Vector3d> minusEpsilonTangents = new List<Vector3d>();
            foreach (double x in discontinuities)
            {
                Vector3d tangentPlus = curve.TangentAt(x + epsilon);
                Vector3d tangentMinus = curve.TangentAt(x - epsilon);
                tangentPlus.Unitize();
                tangentMinus.Unitize();
                plusEpsilonTangents.Add(tangentPlus);
                minusEpsilonTangents.Add(tangentMinus);
            }

            // 3 move points along plusEpsilonTangents and minusEpsilonTangents, and pull to curve
            List<Point3d> pointsMovedByPlus = new List<Point3d>();
            List<Point3d> pointsMovedByMinus = new List<Point3d>();
            for (int i = 0; i < discontinuities.Count; i++)
            {
                Point3d pointOnCurve = curve.PointAt(discontinuities[i]);
                Point3d movedPoint = pointOnCurve + plusEpsilonTangents[i] * r;
                double closestT;
                if (curve.ClosestPoint(movedPoint, out closestT))
                {
                    Point3d closestPoint = curve.PointAt(closestT);
                    pointsMovedByPlus.Add(closestPoint);
                }

                Point3d pointOnCurve2 = curve.PointAt(discontinuities[i]);
                Point3d movedPoint2 = pointOnCurve2 + minusEpsilonTangents[i] * -r;

                double closestT2;
                if (curve.ClosestPoint(movedPoint2, out closestT2))
                {
                    Point3d closestPoint = curve.PointAt(closestT2);
                    pointsMovedByMinus.Add(closestPoint);
                }
            }

            // 6 Create fillet parts and move points to surface
            List<Point3d> pointsMovedByPlusSrf = new List<Point3d>();
            List<Point3d> pointsMovedByMinusSrf = new List<Point3d>();
            List<NurbsCurve> nurbsCurves = new List<NurbsCurve>();

            for (int i = 0; i < discontinuities.Count; i++)
            {
                Point3d ptPlus = pointsMovedByPlus[i];
                Point3d ptOnCurve = curve.PointAt(discontinuities[i]);
                Point3d ptMinus = pointsMovedByMinus[i];

                double uPlus, vPlus, uMinus, vMinus;

                if (surface.ClosestPoint(ptPlus, out uPlus, out vPlus))
                {
                    Point3d ptPlusSrf = surface.PointAt(uPlus, vPlus);
                    pointsMovedByPlusSrf.Add(ptPlusSrf);
                }

                if (surface.ClosestPoint(ptMinus, out uMinus, out vMinus))
                {
                    Point3d ptMinusSrf = surface.PointAt(uMinus, vMinus);
                    pointsMovedByMinusSrf.Add(ptMinusSrf);
                }

                Point3d[] controlPoints = new Point3d[3] { pointsMovedByPlusSrf[i], ptOnCurve, pointsMovedByMinusSrf[i] };

                NurbsCurve nurbsCurve = NurbsCurve.Create(false, 2, controlPoints);
                nurbsCurves.Add(nurbsCurve);
            }

            //7 redraw iso curves
            double uStart, vStart;
            if (surface.ClosestPoint(curve.PointAtStart, out uStart, out vStart))
            {
                Point3d startPtOnSrf = surface.PointAt(uStart, vStart);
                pointsMovedByPlusSrf.Insert(0, startPtOnSrf);
            }

            double uEnd, vEnd;
            if (surface.ClosestPoint(curve.PointAtEnd, out uEnd, out vEnd))
            {
                Point3d endPtOnSrf = surface.PointAt(uEnd, vEnd);
                pointsMovedByMinusSrf.Add(endPtOnSrf); 
            }

            List<Curve> isoCurves = new List<Curve>();

            for (int i = 0; i < pointsMovedByPlusSrf.Count; i++)
            {
                Point3d ptPlusSrf = pointsMovedByPlusSrf[i];
                Point3d ptMinusSrf = pointsMovedByMinusSrf[i];

                Curve isoCurve = surface.InterpolatedCurveOnSurface(new List<Point3d> { ptPlusSrf, ptMinusSrf }, 0.01);
                isoCurves.Add(isoCurve);
            }

            List<Curve> allCurves = new List<Curve>(isoCurves);
            allCurves.AddRange(nurbsCurves);

            Curve[] joinedCurves = Curve.JoinCurves(allCurves);

            return joinedCurves[0];
        }
    }
}
