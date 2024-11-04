using Grasshopper.Kernel.Data;
using Grasshopper;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel.Types;

namespace BarkBeetle.Utils
{
    internal class BrepUtils
    {

        public static Surface ProcessExtendedSurface(double uWidth, double vWidth, Surface surface)
        {
            // Setup for U direction
            List<Curve> extendedUCurves = new List<Curve>();
            Interval uDomain = surface.Domain(0);

            // Extend U direction curves
            int numUDivs = 20;
            for (int i = 0; i <= numUDivs; i++)
            {
                double uParam = uDomain.ParameterAt(i / (double)numUDivs);
                Curve uIsoCurve = surface.IsoCurve(1, uParam);

                // Check if the curve is closed; if so, add without extending
                if (uIsoCurve.IsClosed)
                {
                    extendedUCurves.Add(uIsoCurve);
                }
                else
                {
                    // Extend
                    Curve extendedUIsoCurve = uIsoCurve.Extend(CurveEnd.Both, vWidth, CurveExtensionStyle.Smooth);
                    extendedUCurves.Add(extendedUIsoCurve);
                }
            }

            // Attempt to create lofted surface for U direction
            Brep[] loftedBrepsU = null;
            while (extendedUCurves.Count > 1)
            {
                loftedBrepsU = Brep.CreateFromLoft(extendedUCurves, Point3d.Unset, Point3d.Unset, LoftType.Normal, false);
                if (loftedBrepsU.Length != 0) break;
                extendedUCurves.RemoveAt(extendedUCurves.Count - 1); // Remove last curve and retry
            }

            Surface loftedSurfaceU = loftedBrepsU?[0].Faces[0].ToNurbsSurface();
            if (loftedSurfaceU == null) throw new Exception("Failed to create lofted surface in U direction.");

            /////////////////////////////////////////
            // Setup for V direction
            List<Curve> extendedVCurves = new List<Curve>();
            Interval vDomain = loftedSurfaceU.Domain(1);

            // Extend V direction curves
            int numVDivs = 20;
            for (int i = 0; i <= numVDivs; i++)
            {
                double vParam = vDomain.ParameterAt(i / (double)numVDivs);
                Curve vIsoCurve = loftedSurfaceU.IsoCurve(0, vParam);

                // Check if the curve is closed; if so, add without extending
                if (vIsoCurve.IsClosed)
                {
                    extendedVCurves.Add(vIsoCurve);
                }
                else
                {
                    // Extend
                    Curve extendedVIsoCurve = vIsoCurve.Extend(CurveEnd.Both, uWidth, CurveExtensionStyle.Smooth);
                    extendedVCurves.Add(extendedVIsoCurve);
                }
            }

            // Attempt to create lofted surface for V direction
            Brep[] loftedBrepsV = null;
            while (extendedVCurves.Count > 1)
            {
                loftedBrepsV = Brep.CreateFromLoft(extendedVCurves, Point3d.Unset, Point3d.Unset, LoftType.Normal, false);
                if (loftedBrepsV.Length != 0) break;
                extendedVCurves.RemoveAt(extendedVCurves.Count - 1); // Remove last curve and retry
            }

            Surface loftedSurfaceV = loftedBrepsV?[0].Faces[0].ToNurbsSurface();
            if (loftedSurfaceV == null) throw new Exception("Failed to create lofted surface in V direction.");

            loftedSurfaceV.Transpose(true);
            return loftedSurfaceV;
        }

        public static GH_Structure<GH_Surface> StripFromCurves(GH_Structure<GH_Curve> uvCurves, Surface surface,double strip_width,double extend)
        {
            List<List<GH_Curve>> listCurves = TreeHelper.ConvertGHStructureToList(uvCurves);
            List<List<GH_Surface>> brepsAll = new List<List<GH_Surface>>();

            foreach (List<GH_Curve> ghCrvs in listCurves)
            {
                List<GH_Surface> breps = new List<GH_Surface>();

                foreach (GH_Curve ghCrv in ghCrvs)
                {
                    Curve crv = ghCrv.Value;

                    Curve extend_crv = crv.Extend(CurveEnd.Both, extend, CurveExtensionStyle.Smooth);
                    double[] divisionParameters = extend_crv.DivideByCount(20, true);

                    // Lists to store the points, tangent vectors, and normal vectors
                    List<Point3d> divisionPoints = new List<Point3d>();
                    Curve[] loftLines = new Curve[divisionParameters.Count()];

                    int i = 0;
                    foreach (double t in divisionParameters)
                    {
                        // Get the actual point on the curve using the parameter
                        Point3d pt = crv.PointAt(t);
                        divisionPoints.Add(pt);

                        // Compute tangent vector at the point on the curve
                        Vector3d tangent = crv.TangentAt(t);

                        // Get the UV coordinates of the point on the surface and compute the surface normal
                        double u, v;
                        Vector3d normal;
                        surface.ClosestPoint(pt, out u, out v);// Find the point's (u, v) coordinates on the surface
                        normal = surface.NormalAt(u, v);  // Get the surface normal at that (u, v)

                        // Compute the cross product and line
                        Vector3d crossVec = Vector3d.CrossProduct(normal, tangent);

                        Point3d start = pt - crossVec * strip_width / 2;
                        Point3d end = pt + crossVec * strip_width / 2;
                        Line line = new Line(start, end);
                        loftLines[i] = line.ToNurbsCurve();
                        i++;
                    }

                    Brep loftBrep = Brep.CreateFromLoft(loftLines, Point3d.Unset, Point3d.Unset, LoftType.Normal, false)[0];
                    breps.Add(new GH_Surface(loftBrep));
                }
                brepsAll.Add(breps);
            }

            GH_Structure<GH_Surface>  strips = TreeHelper.ConvertToGHStructure(brepsAll);
            return strips;
        }


        public static double AverageSurfaceDistance(Surface surfaceBottom, Surface surfaceTop, int sampleCount)
        {
            double totalDistance = 0.0;
            Interval uDomain = surfaceTop.Domain(0); 
            Interval vDomain = surfaceTop.Domain(1); 

            // sample points on top surface to get the distance
            for (int i = 0; i < sampleCount; i++)
            {
                double u = uDomain.ParameterAt(i / (double)(sampleCount - 1)); 
                for (int j = 0; j < sampleCount; j++)
                {
                    double v = vDomain.ParameterAt(j / (double)(sampleCount - 1));

                    Point3d pointA = surfaceTop.PointAt(u, v);

                    // Compute closest point on bottom surface
                    surfaceBottom.ClosestPoint(pointA, out double _u, out double _v);
                    Point3d pointB = surfaceBottom.PointAt(_u, _v);
                    totalDistance += pointA.DistanceTo(pointB);
                }
            }

            // Compute average distance
            double averageDistance = totalDistance / (sampleCount * sampleCount);
            return averageDistance;
        }

        public static List<GH_Surface> TweenBetweenSurfaces(Surface surfaceA, Surface surfaceB, int n)
        {
            NurbsSurface nurbsSurfaceA = surfaceA.ToNurbsSurface();
            NurbsSurface nurbsSurfaceB = surfaceB.ToNurbsSurface();

            // initalize a list
            List<GH_Surface> interpolatedSurfaces = new List<GH_Surface>();

            // Add the first surface
            interpolatedSurfaces.Add(new GH_Surface(nurbsSurfaceA));

            // Add the in-between surfaces
            int intermediateCount = n - 2;
            for (int i = 1; i <= intermediateCount; i++)
            {
                double t = i / (double)(n - 1);

                NurbsSurface interpolatedSurface = MorphSurface(nurbsSurfaceA, nurbsSurfaceB, t);

                interpolatedSurfaces.Add(new GH_Surface(interpolatedSurface));
            }

            // Add the last surface
            interpolatedSurfaces.Add(new GH_Surface(nurbsSurfaceB));

            return interpolatedSurfaces;
        }

        private static NurbsSurface MorphSurface( NurbsSurface startSurface, NurbsSurface endSurface, double t)
        {
            NurbsSurface targetSurface = startSurface.Duplicate() as NurbsSurface;

            int uCount = Math.Max(startSurface.Points.CountU, endSurface.Points.CountU);
            int vCount = Math.Max(startSurface.Points.CountV, endSurface.Points.CountV);

            startSurface = startSurface.Rebuild(startSurface.Degree(0), startSurface.Degree(1), uCount, vCount);
            endSurface = endSurface.Rebuild(endSurface.Degree(0), endSurface.Degree(1), uCount, vCount);
            targetSurface = targetSurface.Rebuild(targetSurface.Degree(0), targetSurface.Degree(1), uCount, vCount); 

            // Go through all control points
            for (int u = 0; u < uCount; u++)
            {
                for (int v = 0; v < vCount; v++)
                {
                    // Get the two control points
                    Point3d pointStart = startSurface.Points.GetControlPoint(u, v).Location;
                    Point3d pointEnd = endSurface.Points.GetControlPoint(u, v).Location;

                    // Calculate interpolate points
                    Point3d interpolatedPoint = new Point3d(pointStart);
                    interpolatedPoint.Interpolate(pointStart, pointEnd, t);

                    // update control points on target surface
                    targetSurface.Points.SetControlPoint(u, v, new ControlPoint(interpolatedPoint));
                }
            }
            return targetSurface;
        }

    }

    
}

