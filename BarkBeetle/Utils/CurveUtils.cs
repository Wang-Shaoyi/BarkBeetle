using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
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
    }
}
