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
    }
}
