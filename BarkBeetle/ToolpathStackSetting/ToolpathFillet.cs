using BarkBeetle.CompsToolpath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;
using System.Security.Cryptography;
using Grasshopper.Kernel.Types;
using BarkBeetle.Utils;

namespace BarkBeetle.ToolpathStackSetting
{
    internal class ToolpathFillet
    {
        public static GH_Curve FilletContinuousToolpathStackByLayers(ToolpathStack toolpathStack, double r,double layerBetweenFactor, ref ToolpathStack newToolpathStack)
        {
            // Initialize
            List<GH_Curve> filletCurves = new List<GH_Curve>();
            
            // Get data
            List<GH_Curve> originalCurves = toolpathStack.LayerCurves;
            List<GH_Surface> gH_Surfaces = toolpathStack.Surfaces;
            int layerCount = gH_Surfaces.Count;


            // 检查 toolpathStack 是哪个子类
            if (toolpathStack is StackOffset)
            {
                // 如果是 StackVertical 类型，则创建一个新的 StackVertical 实例
                newToolpathStack = new StackOffset(
                    toolpathStack.Patterns,
                    toolpathStack.LayerHeight,
                    toolpathStack.AngleGlobal,
                    toolpathStack.LayerHeight * layerCount,
                    toolpathStack.PlaneRefPt,
                    toolpathStack.RotateAngle);
            }
            else if (toolpathStack is StackBetween)
            {
                // 如果是 StackBetween 类型，则创建一个新的 StackBetween 实例
                newToolpathStack = new StackBetween(
                    toolpathStack.Patterns,
                    toolpathStack.LayerHeight,
                    toolpathStack.AngleGlobal,
                    toolpathStack.Surfaces[layerCount - 1].Value.Surfaces[0],
                    toolpathStack.PlaneRefPt,
                    toolpathStack.RotateAngle);
            }

            // Go through each layer
            for (int i = 0; i < layerCount; i++)
            {
                
                // Get the fillet curve
                Curve crv = originalCurves[i].Value;
                Surface srf = gH_Surfaces[i].Value.Surfaces[0];
                Curve filletCrv = FilletSingleLayerToolpathOnSurface(crv, r, srf);
                // Trim every layer with f * r
                layerBetweenFactor += 0.0001;
                Curve trimCurrent = null;
                if (i == 0) trimCurrent = filletCrv.Trim(CurveEnd.End, layerBetweenFactor * r);
                else if (i == layerCount - 1) trimCurrent = filletCrv.Trim(CurveEnd.Start, layerBetweenFactor * r);
                else trimCurrent = filletCrv.Trim(CurveEnd.Both, layerBetweenFactor * r);
                filletCurves.Add(new GH_Curve(trimCurrent));
            }

            newToolpathStack.LayerCurves = filletCurves;

            newToolpathStack.FinalCurve = new GH_Curve(JoinAndFilletBetweenLayers(newToolpathStack.LayerCurves, r, layerBetweenFactor));

            GH_Curve finalCurv = newToolpathStack.FinalCurve;

            List<List<GH_Number>> speedFactors = new List<List<GH_Number>>();

            newToolpathStack.OrientPlanes = newToolpathStack.CreateStackOrientPlanes(newToolpathStack.RotateAngle,ref speedFactors);
            newToolpathStack.SpeedFactors = speedFactors;

            return newToolpathStack.FinalCurve;
        }


        public static Curve JoinAndFilletBetweenLayers(List<GH_Curve> curves, double r, double f)
        {

            //3. Draw arch
            PolyCurve polyCurve = new PolyCurve();

            for (int i = 0; i < curves.Count; i++)
            {
                // Append Curve
                Curve currentCurve = curves[i].Value;
                
                polyCurve.Append(currentCurve);

                // Append arch
                if (i < curves.Count - 1)
                {
                    Curve nextCurve = curves[i + 1].Value;
                    Curve blend1 = Curve.CreateBlendCurve(currentCurve, nextCurve, BlendContinuity.Position);

                    polyCurve.Append(blend1);
                }
            }
            return polyCurve;
        }

        public static Curve FilletSingleLayerToolpathOnSurface(Curve curve, double r, Surface surface)
        {
            // 1. Get discontinuouty
            List<double> discontinuities = new List<double>();

            double tStart = curve.Domain.Min;
            double tEnd = curve.Domain.Max;

            double t;
            while (curve.GetNextDiscontinuity(Continuity.C1_continuous, tStart, tEnd, out t))
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


        #region This is very very very very slow!!!!
        //public static Curve FilletPolyCurve(Curve curve, double r)
        //{
        //    if (curve is PolyCurve polyCurve)
        //    {
        //        for (int i = 0; i < polyCurve.SegmentCount; i++)
        //        {
        //            Curve currentCurve = polyCurve.SegmentCurve(i);
        //            double curveLength = currentCurve.GetLength();

        //            Point3d currentStartPt = currentCurve.PointAtLength(r);
        //            Point3d currentEndPt = currentCurve.PointAtLength(curveLength - r);

        //            double startT, endT;
        //            currentCurve.ClosestPoint(currentStartPt, out startT);
        //            currentCurve.ClosestPoint(currentEndPt, out endT);

        //            if (curveLength > 2 * r)
        //            {
        //                // construct the trimmed curve
        //                Curve trimmedCurve = currentCurve.Trim(startT, endT);
        //                if (trimmedCurve != null)
        //                {
        //                    polyCurve.Append(trimmedCurve);
        //                }
        //            }

        //            // if not the final curve, draw arc
        //            if (i < polyCurve.SegmentCount - 1)
        //            {
        //                Curve nextCurve = polyCurve.SegmentCurve(i+1);

        //                Point3d endPt = currentCurve.PointAtEnd;
        //                Point3d startPt = nextCurve.PointAtStart;

        //                // get arc points and vectors
        //                Vector3d currentEndVec = currentCurve.TangentAt(curveLength - r);
        //                Point3d nextStartPt = nextCurve.PointAtLength(r);

        //                // Draw connection arc
        //                Arc connectingArc = new Arc(currentEndPt, currentEndVec, nextStartPt);
        //                polyCurve.Append(new ArcCurve(connectingArc));
        //            }
        //        }
        //        return polyCurve;
        //    }
        //    return curve;
        //}
        #endregion
    }
}
