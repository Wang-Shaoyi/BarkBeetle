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
        public static List<GH_Point> CurveIntersect(List<Curve> curvesA,List<Curve> curvesB,double tolerance)
        {
            List<GH_Point> intersectionPoints = new List<GH_Point>();

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
                            intersectionPoints.Add(new GH_Point(intersection.PointA));
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

        public static Vector3d GetTangentAtPoint(Curve curve, Point3d point, double epsilon = 1e-10)
        {
            Vector3d tangent = Vector3d.Unset;

            double t;
            if (curve.ClosestPoint(point, out t))
            {
                tangent = curve.TangentAt(t + epsilon);
                tangent.Unitize();
            }

            return tangent;
        }

        public static bool IsConvexPointOnCurve(Curve curve, Point3d vertex)
        {
            if (!curve.IsClosed)
                throw new ArgumentException("Curve should be closed");

            // find parameter t on curve
            double t;
            if (!curve.ClosestPoint(vertex, out t))
                throw new ArgumentException("point is not on curve");

            // 获取点附近的两个参数
            double tPrev = t - 0.001;
            double tNext = t + 0.001;

            // 确保参数在曲线域范围内（循环处理）
            double domainStart = curve.Domain.Min;
            double domainEnd = curve.Domain.Max;

            if (tPrev < domainStart) tPrev = domainEnd - (domainStart - tPrev);
            if (tNext > domainEnd) tNext = domainStart + (tNext - domainEnd);

            // 获取前后点的坐标
            Point3d prevPoint = curve.PointAt(tPrev);
            Point3d nextPoint = curve.PointAt(tNext);

            // 计算两条边向量
            Vector3d vecPrev = prevPoint - vertex;
            Vector3d vecNext = nextPoint - vertex;

            // 计算法向量
            Vector3d tangent = curve.TangentAt(t);
            Vector3d normal = Vector3d.CrossProduct(vecNext, vecPrev);
            normal.Unitize();

            // 判断曲线方向和法向量方向的一致性
            bool isConvexPoint = Vector3d.Multiply(normal, tangent) < 0;

            // 判断法线方向
            return isConvexPoint; // >0 为凸点，<0 为凹点
        }

        public static List<Point3d> GetDiscontinuityPoints(Curve curve, out List<Curve> segments)
        {
            List<Point3d> discontinuityPoints = new List<Point3d>();

            double t = curve.Domain.Min;
            List<double> splitParams = new List<double>();

            while (curve.GetNextDiscontinuity(Continuity.G1_continuous, t, curve.Domain.Max, out double discontinuityT))
            {
                t = discontinuityT;
                splitParams.Add(t);
                discontinuityPoints.Add(curve.PointAt(t));
            }

            if (!discontinuityPoints.Contains(curve.PointAtStart))
            {
                discontinuityPoints.Insert(0, curve.PointAtStart);
                splitParams.Insert(0, curve.Domain.Min);
            }

            segments = new List<Curve>(curve.Split(splitParams));

            return discontinuityPoints;
        }

        public static List<Point3d> GetEvenlyDistributedPointsOnCrv(Curve curve, int n)
        {
            if (curve == null || n < 2)
                throw new ArgumentException("Curve can't be empty");

            // 使用 DivideByCount 方法获取点的参数
            Point3d[] points;
            curve.DivideByCount(n - 1, true, out points);


            return new List<Point3d>(points);
        }
    }
}
