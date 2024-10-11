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

        public static Vector3d GetTangentAtPoint(Curve curve, Point3d point)
        {
            double t;

            // 找到点在曲线上的最近点的参数 t
            if (curve.ClosestPoint(point, out t))
            {
                // 检查是否在转折点附近
                double nextDiscontinuity;
                bool isKink = curve.GetNextDiscontinuity(Continuity.C1_locus_continuous, t, curve.Domain.Max, out nextDiscontinuity)
                              && Math.Abs(nextDiscontinuity - t) < 0.001;

                Vector3d tangent;

                if (isKink)
                {
                    // 如果在转折点，沿着下一段曲线的方向获取切向
                    double nextT = t + 0.001;  // 略微移动 t 参数
                    tangent = curve.TangentAt(nextT);
                }
                else
                {
                    // 正常获取切向
                    tangent = curve.TangentAt(t);
                }

                tangent.Unitize();  // 归一化切向量
                return tangent;
            }

            return Vector3d.Unset;  // 如果没有找到最近点，返回未定义的向量
        }
    }
}
