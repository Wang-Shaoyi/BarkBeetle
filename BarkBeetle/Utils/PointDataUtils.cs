using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarkBeetle.Utils
{
    internal class PointDataUtils
    {
        public static double SinOfTwoVectors(Vector3d v1, Vector3d v2)
        {
            double angleRadians = Vector3d.VectorAngle(v1, v2);
            double sinValue = Math.Sin(angleRadians);
            return sinValue;
        }

        public static double CosOfTwoVectors(Vector3d v1, Vector3d v2)
        {
            double angleRadians = Vector3d.VectorAngle(v1, v2);
            double sinValue = Math.Cos(angleRadians);
            return sinValue;
        }

        public static List<Point3d> GetDiscontinuityPoints(Curve curve)
        {
            List<Point3d> discontinuityPoints = new List<Point3d>();

            double t0 = curve.Domain.Min;
            double t1 = curve.Domain.Max;

            double t; // 用于存储不连续点的参数值

            discontinuityPoints.Add(curve.PointAt(t0));

            // 循环查找不连续点
            while (curve.GetNextDiscontinuity(Continuity.C1_continuous, t0, t1, out t))
            {
                // 获取不连续点的坐标
                Point3d discontinuityPoint = curve.PointAt(t);
                discontinuityPoints.Add(discontinuityPoint);

                // 更新 t0 为当前找到的不连续点参数，继续查找
                t0 = t;
            }
            discontinuityPoints.Add(curve.PointAt(t1));

            return discontinuityPoints;
        }

        public static List<Point3d> GetExplodedCurveVertices(Curve curve)
        {
            List<Point3d> vertices = new List<Point3d>();

            // 检查曲线是否是 PolyCurve 类型
            if (curve is PolyCurve polyCurve)
            {
                // Explode PolyCurve 并获取每段的起点和终点
                for (int i = 0; i < polyCurve.SegmentCount; i++)
                {
                    Curve segment = polyCurve.SegmentCurve(i);
                    vertices.Add(segment.PointAtStart);
                    vertices.Add(segment.PointAtEnd);
                }
            }
            else
            {
                // 如果不是 PolyCurve，处理为单独的曲线
                vertices.Add(curve.PointAtStart);
                vertices.Add(curve.PointAtEnd);
            }

            return vertices;
        }
    }


}
