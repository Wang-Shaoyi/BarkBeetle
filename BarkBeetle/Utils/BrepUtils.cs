using Grasshopper.Kernel.Data;
using Grasshopper;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel.Types;
using BarkBeetle.GeometriesPackage;

namespace BarkBeetle.Utils
{
    internal class BrepUtils
    {
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

        public static GH_Structure<GH_Surface> StripFromSkeleton(SkeletonPackage skeletonPacakge, double extend)
        {
            List<List<GH_Curve>> listCurves = skeletonPacakge.UVCurves;
            Surface surface = skeletonPacakge.ExtendedSurface;
            double strip_width = skeletonPacakge.StripWidth;
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

            GH_Structure<GH_Surface> strips = TreeHelper.ConvertToGHStructure(brepsAll);
            return strips;
        }
    }

    
}

