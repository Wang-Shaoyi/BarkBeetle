using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarkBeetle.Utils
{
    internal class CurveUtils
    {
        // Intersection with tolerance
        public static List<Point3d> CurveIntersect(List<Curve> curvesA,List<Curve> curvesB,double tolerance)
        {
            List<Point3d> intersectionPoints = new List<Point3d>();

            foreach (Curve curveA in curvesA)
            {
                foreach (Curve curveB in curvesB)
                {
                    var intersections = Rhino.Geometry.Intersect.Intersection.CurveCurve(curveA, curveB, tolerance, tolerance);

                    if (intersections != null)
                    {
                        for (int i = 0; i < intersections.Count; i++)
                        {
                            var intersection = intersections[i];
                            intersectionPoints.Add(intersection.PointA);
                        }
                    }
                }
            }

            return intersectionPoints;
        }

        public static List<List<GH_Curve>> GetUVCurvesVecPt(GH_Structure<GH_Point> organizedPtsTree, ref GH_Vector[,,] uvVectors, ref GH_Point[,] organizedPtsArray)
        {
            List<List<GH_Curve>> uvCurves = new List<List<GH_Curve>>();

            organizedPtsTree.Simplify(GH_SimplificationMode.CollapseLeadingOverlaps);

            int uCount = organizedPtsTree.PathCount;
            int vCount = organizedPtsTree.Branches.Max(b => b.Count);

            uvVectors = new GH_Vector[uCount, vCount, 2];
            organizedPtsArray = new GH_Point[uCount, vCount];

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
                                uvVectors[u, v, 1] = new GH_Vector(tangent);
                                organizedPtsArray[u, v] = new GH_Point(points[v]);
                            }
                            else uvVectors[v, u, 0] = new GH_Vector(tangent);
                        }
                    }
                }
                uvCurves.Add(interpolatedCurvesDir);
                organizedPtsTree = TreeHelper.FlipMatrixNoComp(organizedPtsTree);
            }
            return uvCurves;
        }


        public static bool IsConvexPointOnCurve(Curve curve, Point3d vertex)
        {
            if (!curve.IsClosed)
                throw new ArgumentException("Curve should be closed");

            CurveOrientation orientation = curve.ClosedCurveOrientation(Plane.WorldXY);
            bool counterClockwise = orientation == CurveOrientation.CounterClockwise;

            int direction = DetermineCurveDirection(curve, vertex);

            return (counterClockwise && direction == 1) || (!counterClockwise && direction != 1);
        }

        // This is based on discontinuity
        public static List<Point3d> GetDiscontinuityPoints(Curve curve, out List<Curve> segments)
        {


            List<Point3d> discontinuityPoints = new List<Point3d>();

            double t = curve.Domain.Min;
            List<double> splitParams = new List<double>();
            Vector3d tangentBefore, tangentAfter;

            while (curve.GetNextDiscontinuity(Continuity.G1_continuous, t, curve.Domain.Max, out double discontinuityT))
            {
                t = discontinuityT;

                // 计算前后切线
                tangentBefore = curve.TangentAt(t - Rhino.RhinoMath.ZeroTolerance);
                tangentAfter = curve.TangentAt(t + Rhino.RhinoMath.ZeroTolerance);

                double angle = Vector3d.VectorAngle(tangentBefore, tangentAfter) * (180.0 / System.Math.PI); // 角度转换为度

                // 如果角度大于给定的degree，则认为是不连续点
                if (angle > 30)
                {
                    Point3d discontinuityPoint = curve.PointAt(t);
                    splitParams.Add(t);
                    discontinuityPoints.Add(curve.PointAt(t));
                }
            }

            if (!discontinuityPoints.Contains(curve.PointAtStart))
            {
                discontinuityPoints.Insert(0, curve.PointAtStart);
                splitParams.Insert(0, curve.Domain.Min);
            }

            segments = new List<Curve>(curve.Split(splitParams));

            return discontinuityPoints;
        }

        public static bool IsPointADiscontinuity(Curve curve, Point3d point, double degree)
        {
            List<Point3d> discontinuities = new List<Point3d>();
            double t;
            double searchStart = curve.Domain.Min;
            Vector3d tangentBefore, tangentAfter;

            // Get all discontinuity points
            while (curve.GetNextDiscontinuity(Continuity.G1_locus_continuous, searchStart, curve.Domain.Max, out t))
            {
                // 计算前后切线
                tangentBefore = curve.TangentAt(t - Rhino.RhinoMath.ZeroTolerance);
                tangentAfter = curve.TangentAt(t + Rhino.RhinoMath.ZeroTolerance);

                // 计算两切线向量之间的角度
                double angle = Vector3d.VectorAngle(tangentBefore, tangentAfter) * (180.0 / System.Math.PI); // 角度转换为度

                // 如果角度大于给定的degree，则认为是不连续点
                if (angle > degree)
                {
                    Point3d discontinuityPoint = curve.PointAt(t);
                    discontinuities.Add(discontinuityPoint);
                }

                searchStart = t + Rhino.RhinoMath.ZeroTolerance;
            }

            // 检查点是否在不连续点列表中
            foreach (Point3d dp in discontinuities)
            {
                if (point.DistanceTo(dp) < Rhino.RhinoMath.ZeroTolerance)
                {
                    return true; // 点是不连续点
                }
            }

            return false; // 点不是不连续点
        }

        // This is based on explosion
        public static List<Point3d> GetExplodedCurveVertices(Curve curve, double d)
        {

            List<Point3d> vertexList = new List<Point3d>();
            Curve[] segments = curve.DuplicateSegments();
            //if (segments != null)
            //{
            //    foreach (Curve segment in segments)
            //    {
            //        vertexList.Add(segment.PointAtStart);
            //    }
            //    vertexList.Add(segments[segments.Length - 1].PointAtEnd); // 添加最后一个顶点
            //}
            if (segments != null)
            {
                foreach (Curve segment in segments)
                {
                    if (segment.GetLength() > d * 1.5)
                    {
                        double segmentLength = segment.GetLength();
                        int count = (int)(segmentLength / d);
                        for (int i = 0; i <= count; i++)
                        {
                            double t = segment.Domain.ParameterAt(i * d / segmentLength);
                            vertexList.Add(segment.PointAt(t));
                        }
                    }
                    else
                    {
                        vertexList.Add(segment.PointAtStart);
                    }
                }
                // 确保最后一个点总是包含
                if (!vertexList.Contains(segments[segments.Length - 1].PointAtEnd))
                {
                    vertexList.Add(segments[segments.Length - 1].PointAtEnd);
                }
            }
            else
            {
                vertexList.Add(curve.PointAtStart);
                vertexList.Add(curve.PointAtEnd);
            }

            return vertexList;
        }
        private static void GetVerticesRecursive(Curve curve, List<Point3d> vertices)
        {
            if (curve is PolyCurve polyCurve)
            {
                // 遍历 PolyCurve 的所有段
                for (int i = 0; i < polyCurve.SegmentCount; i++)
                {
                    GetVerticesRecursive(polyCurve.SegmentCurve(i), vertices);
                }
            }
            else
            {
                // 添加起点
                if (vertices.Count == 0 || !vertices[vertices.Count - 1].EpsilonEquals(curve.PointAtStart, RhinoMath.SqrtEpsilon))
                {
                    vertices.Add(curve.PointAtStart);
                }

                // 添加中点
                double halfLength = curve.GetLength() / 2.0;
                if (curve.LengthParameter(halfLength, out double midParameter))
                {
                    vertices.Add(curve.PointAt(midParameter));
                }

                // 添加终点
                vertices.Add(curve.PointAtEnd);
            }
        }

        public static PolyCurve CreatePolyCurveOnSurface(Surface surface, List<Point3d> points)
        {
            if (surface == null || points == null || points.Count < 2)
            {
                throw new ArgumentException("Surface and point list must be valid and contain at least two points.");
            }

            // Step 1: 将点拉回曲面上
            List<Point3d> projectedPoints = new List<Point3d>();
            foreach (Point3d point in points)
            {
                if (surface.ClosestPoint(point, out double u, out double v))
                {
                    projectedPoints.Add(surface.PointAt(u, v));
                }
            }

            // Step 2: 创建 PolyCurve
            PolyCurve polyCurve = new PolyCurve();

            for (int i = 0; i < projectedPoints.Count - 1; i++)
            {
                // 创建两点之间的直线段
                Line segment = new Line(projectedPoints[i], projectedPoints[i + 1]);
                polyCurve.Append(new LineCurve(segment));
            }

            return polyCurve;
        }

        public static Curve RemapPolyCurveOnNewSurface(Surface newSrf, Surface oldSrf, List<Point3d> points)
        {
            newSrf.SetDomain(0, new Interval(oldSrf.Domain(0).T0, oldSrf.Domain(0).T1));
            newSrf.SetDomain(1, new Interval(oldSrf.Domain(1).T0, oldSrf.Domain(1).T1));

            if (newSrf == null || points == null || points.Count < 2)
            {
                throw new ArgumentException("Surface and point list must be valid and contain at least two points.");
            }

            // Pull point to surface
            List<Point3d> projectedPoints = new List<Point3d>();
            foreach (Point3d point in points)
            {
                if (oldSrf.ClosestPoint(point, out double u, out double v))
                {
                    projectedPoints.Add(newSrf.PointAt(u, v));
                }
            }

            // Generate the new curve
            List<Curve> surfaceCurves = new List<Curve>();
            for (int j = 0; j < projectedPoints.Count - 1; j++) // Generate curve by segments
            {
                Curve curve = newSrf.InterpolatedCurveOnSurface(new List<Point3d> { projectedPoints[j], projectedPoints[j + 1] }, 0.01);
                surfaceCurves.Add(curve);
            }

            Curve[] surfaceCurve = Curve.JoinCurves(surfaceCurves, 10); // Join the segments

            return surfaceCurve[0];
        }

        

        public static int DetermineCurveDirection(Curve curve, Point3d point)
        {
            double t;
            // get point parameter
            curve.ClosestPoint(point, out t); 

            // 获取曲线域
            Interval interval = curve.Domain;

            // 计算前后的参数值，确保它们位于曲线的定义域内
            double tBefore = t - (curve.GetLength() * 0.000001) / curve.Domain.Length; // The number here makes a difference!!!
            double tAfter = t + (curve.GetLength() * 0.000001) / curve.Domain.Length;

            // 约束tBefore和tAfter在曲线域内
            if (tBefore < interval.Min)
            {
                tBefore = interval.Max - (interval.Min - tBefore);
            }

            if (tAfter > interval.Max)
            {
                tAfter = interval.Min + (tAfter - interval.Max);
            }

            //tBefore = System.Math.Max(tBefore, interval.Min);
            //tAfter = System.Math.Min(tAfter, interval.Max);

            // 获取前后点的切线向量
            Vector3d tangentBefore = curve.TangentAt(tBefore);
            Vector3d tangentAfter = curve.TangentAt(tAfter);

            // 使用向量叉乘来确定转弯方向
            Vector3d crossProduct = Vector3d.CrossProduct(tangentBefore, tangentAfter);

            if (crossProduct.Z > 0)
            {
                return 1; // counterclockwise
            }
            else if (crossProduct.Z < 0)
            {
                return -1; // clockwise
            }
            else
            {
                return 0;
            }
        }

    }
}
