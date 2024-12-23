using BarkBeetle.Utils;
using Grasshopper.Kernel.Types;
using Microsoft.SqlServer.Server;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BarkBeetle.Pattern
{


    internal class AvoidObstacles
    {
        private Curve skeletonCrv;
        public List<Curve> BlockBoundaries;
        public List<Curve> TrimCurves;
        public List<Point3d> IntersectionPts;
        private Surface surface;
        private double pathWidth;


        public AvoidObstacles(ToolpathPattern pattern, List<Point3d> pts, Curve block, Plane blockPlane, out ToolpathPattern OutputPattern)
        {
            skeletonCrv = pattern.Skeleton.SkeletonMainCurve.Value;
            surface = pattern.BaseSrf;

            BlockBoundaries = new List<Curve>();
            TrimCurves = new List<Curve>();
            IntersectionPts = new List<Point3d>();

            pathWidth = pattern.PathWidth;
            Curve continuousCrv = pattern.BundleCurves[0].DuplicateCurve();


            ToolpathPattern newPattern = pattern.DeepCopy();

            ////////////////////////////////
            if (block == null || !block.IsClosed)
                throw new ArgumentException("Block curve should be closed");

            Point3d center = AreaMassProperties.Compute(block).Centroid;

            foreach (Point3d pt in pts)
            {
                // Draw a rectangle as boundary
                skeletonCrv.ClosestPoint(pt, out double t);
                Point3d closestPt = skeletonCrv.PointAt(t);
                Curve cutCurve = ReorientAndOffsetOnSurface(closestPt, skeletonCrv, surface, block, blockPlane, pathWidth, out Curve blockOnSrf, out Plane targetPlane);
                BlockBoundaries.Add(blockOnSrf);
                TrimCurves.Add(cutCurve);


                // Find intersections
                List<Point3d> intersectionPts = CurveUtils.CurveIntersect(new List<Curve> { cutCurve }, new List<Curve> { continuousCrv }, 0.5);
                IntersectionPts.AddRange(intersectionPts);

                // Get the center point for ordering
                Point3d centerPoint = pt;

                // Order the points around the center points
                List<Point3d> sortedIntersectionPts = intersectionPts
                    .OrderBy(p => Math.Atan2(p.Y - centerPoint.Y, p.X - centerPoint.X)) 
                    .ToList();

                // Reorder the intersections by distance
                //List<Point3d> reorderedPts = ReorderPointsByDistanceToCurve(intersectionPts, skeletonCrv);
                Curve newCrv = TrimCurveByPoints(intersectionPts, continuousCrv,cutCurve, surface);
                continuousCrv = newCrv;
            }

            newPattern.BundleCurves[0] = continuousCrv;
            newPattern.CoutinuousCurve = newPattern.ToolpathContinuousStraight(newPattern.BundleCurves);
            OutputPattern = newPattern;
        }

        public static Curve ReorientAndOffsetOnSurface(Point3d point, Curve curve, Surface surface, Curve block, Plane blockPlane, double pathWidth, out Curve blockOnSrf, out Plane targetPlane)
        {
            // Draw the vectors
            surface.ClosestPoint(point, out double u, out double v);
            curve.ClosestPoint(point, out double t);

            Vector3d normal = surface.NormalAt(u, v);
            normal.Unitize();

            Vector3d tangentU = curve.TangentAt(t);
            tangentU.Unitize();

            Vector3d tangentV = Vector3d.CrossProduct(normal, tangentU);
            tangentV.Unitize();

            targetPlane = new Plane(point,tangentU,tangentV);

            blockOnSrf = OrientGeometry(block, blockPlane, targetPlane);

            Curve offsetCrv = OffsetCurveOutward(blockOnSrf, targetPlane, pathWidth/2);

            offsetCrv = Curve.ProjectToBrep(offsetCrv, surface.ToBrep(), -normal, 0.1)[0];

            return offsetCrv;
        }


        public Curve TrimCurveByPoints(List<Point3d> points, Curve curve,Curve cutCurve, Surface surface)
        {
            // If point count not even, return the input curve
            if (points.Count == 0)
            {
                return curve;
            }

            ///////////////////////////////////////////
            // Get all parameters to trim
            List<double> trimToolpathParams = new List<double>();
            for (int i = 0; i < points.Count; i ++)
            {
                curve.ClosestPoint(points[i], out double t);
                trimToolpathParams.Add(t);
            }

            // Split the toolpath curve
            Curve[] splitToolpathCurves = curve.Split(trimToolpathParams);
            if (splitToolpathCurves == null || splitToolpathCurves.Length == 0)
            {
                throw new InvalidOperationException("Curve split failed");
            }

            List<Curve> filteredToolpathCurves = new List<Curve>();
            foreach (var crv in splitToolpathCurves)
            {
                if (crv != null)
                {
                    Point3d midPoint = crv.PointAtNormalizedLength(0.5);
                    var containment = cutCurve.Contains(midPoint, Plane.WorldXY, 0.001);
                    if (containment != PointContainment.Inside) 
                    {
                        filteredToolpathCurves.Add(crv);
                    }
                }
            }

            if (filteredToolpathCurves.Count == 0) return curve;

            /////////////////////////////////////////////
            //// Get all parameters to trim
            //List<double> trimBlockParams = new List<double>();
            //for (int i = 0; i < points.Count; i++)
            //{
            //    cutCurve.ClosestPoint(points[i], out double t);
            //    trimBlockParams.Add(t);
            //}

            //// Split the block cutcurve
            //Curve[] splitBlockCurves = cutCurve.Split(trimBlockParams);
            //if (splitBlockCurves == null || splitBlockCurves.Length == 0)
            //{
            //    throw new InvalidOperationException("Curve split failed");
            //}

            //List<Curve> filteredBlockCurves = new List<Curve>();
            //foreach (var crv in splitBlockCurves)
            //{
            //    if (crv != null)
            //    {
            //        Point3d midPoint = crv.PointAtNormalizedLength(0.5);
            //        var containment = curve.Contains(midPoint, Plane.WorldXY, 0.001);
            //        if (containment != PointContainment.Inside)
            //        {
            //            filteredBlockCurves.Add(crv);
            //        }
            //    }
            //}


            //////////////////////////////////////////////////////////
            // Connect segments
            List<Curve> filteredBlockCurves = new List<Curve>();
            for (int i = 0; i < filteredToolpathCurves.Count; i++)
            {
                Point3d start = filteredToolpathCurves[i].PointAtEnd;
                Point3d end = filteredToolpathCurves[(i + 1) % filteredToolpathCurves.Count].PointAtStart;
                filteredBlockCurves.Add(new Line(start, end).ToNurbsCurve());
            }

            ///////////////////////////////////////////////////////////
            // Join curves
            List<Curve> combinedCurves = new List<Curve>();
            combinedCurves.AddRange(filteredToolpathCurves);
            combinedCurves.AddRange(filteredBlockCurves);
            Curve[] joinedCurves = Curve.JoinCurves(combinedCurves);

            return joinedCurves[0];
        }

        public static Curve OffsetCurveOutward(Curve curve, Plane plane, double offsetDistance)
        {
            if (curve == null || !curve.IsClosed)
                throw new ArgumentException("Curve must be closed");


            // 尝试偏移曲线
            Curve[] offsetCurves = curve.Offset(plane, offsetDistance, 0.01, CurveOffsetCornerStyle.Sharp);

            if (offsetCurves == null || offsetCurves.Length == 0)
            {
                throw new InvalidOperationException("偏移失败，请检查曲线或偏移距离！");
            }

            // 选择偏移后面积变大的曲线
            foreach (Curve offsetCurve in offsetCurves)
            {
                double originalArea = AreaMassProperties.Compute(curve).Area;
                double offsetArea = AreaMassProperties.Compute(offsetCurve).Area;

                if (offsetArea > originalArea)
                {
                    return offsetCurve; // 偏移后面积变大，返回该曲线
                }
            }

            // 如果所有偏移都失败，则抛出异常
            throw new InvalidOperationException("未找到面积变大的偏移曲线！");
        }

        public static Curve OrientGeometry(Curve geometry, Plane sourcePlane, Plane targetPlane)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry), "几何体不能为空！");

            // 创建变换矩阵
            Transform transform = Transform.PlaneToPlane(sourcePlane, targetPlane);

            // 创建几何体的副本以进行变换
            Curve transformedGeometry = geometry.DuplicateCurve();
            if (!transformedGeometry.Transform(transform))
            {
                throw new InvalidOperationException("几何体变换失败！");
            }

            return transformedGeometry;
        }
    }
}
