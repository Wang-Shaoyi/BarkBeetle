using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
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
                                uvVectors[u, v, 0] = new GH_Vector(tangent);
                                organizedPtsArray[u, v] = new GH_Point(points[v]);
                            }
                            else uvVectors[v, u, 1] = new GH_Vector(tangent);
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
    }
}
