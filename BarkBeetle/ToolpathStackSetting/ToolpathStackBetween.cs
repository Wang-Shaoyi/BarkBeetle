using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;

using BarkBeetle.Utils;
using BarkBeetle.Pattern;
using System.Security.Cryptography;

namespace BarkBeetle.ToolpathStackSetting
{
    internal class ToolpathStackBetween : ToolpathStack
    {
        public override string ToolpathStackName { get; set; } = "Vertical";

        Surface topSrf = null;

        public ToolpathStackBetween(ToolpathPattern tb,  double h, bool ag, Surface topS, Point3d refPt) : base(tb,h,ag, refPt) 
        {
            topSrf = topS;
            PerformCustomLogic(tb, h, ag, refPt);
        }

        public override List<GH_Surface> CreateStackSurfaces()
        {
            
            Surface baseSurface = Pattern.Skeleton.UVNetwork.ExtendedSurface;
            LayerNum = (int)(BrepUtils.AverageSurfaceDistance(baseSurface, topSrf, 6)/LayerHeight) +1;

            List<GH_Surface> stackSurfaces = BrepUtils.TweenBetweenSurfaces(baseSurface, topSrf, LayerNum);
            
            return stackSurfaces;
        }

        public override List<GH_Curve> CreateStackLayerCurves() 
        {
            Curve baseCurve = Pattern.CoutinuousCurve;
            List<Point3d> points = PointDataUtils.GetExplodedCurveVertices(baseCurve);

            List<GH_Curve> stackCurves = new List<GH_Curve>();
            Surface baseSrf = Surfaces[0].Value.Surfaces[0];
            Interval uDomainBase = baseSrf.Domain(0);
            Interval vDomainBase = baseSrf.Domain(1);

            for (int i = 0; i < LayerNum; i++)
            {
                // Get current surface
                Surface srf = Surfaces[i].Value.Surfaces[0];

                Interval uDomainSrf = srf.Domain(0);
                Interval vDomainSrf = srf.Domain(1);

                // Pull points on surface
                List<Point3d> pointsOnSurface = new List<Point3d>();
                foreach(Point3d pt in points)
                {
                    double uBase, vBase;
                    baseSrf.ClosestPoint(pt, out uBase, out vBase);

                    // 计算相对位置
                    double uNormalized = uDomainBase.NormalizedParameterAt(uBase);
                    double vNormalized = vDomainBase.NormalizedParameterAt(vBase);

                    // 将相对位置映射到第二个曲面的 UV 范围
                    double uSrf = uDomainSrf.ParameterAt(uNormalized);
                    double vSrf = vDomainSrf.ParameterAt(vNormalized);

                    Point3d pt3dOnSurf = srf.PointAt(uSrf, vSrf);
                    pointsOnSurface.Add(pt3dOnSurf);
                }

                // Generate the new curve
                List<Curve> surfaceCurves = new List<Curve>();
                for (int j = 0; j < pointsOnSurface.Count - 1; j++) // Generate curve by segments
                {
                    Curve curve = srf.InterpolatedCurveOnSurface(new List<Point3d> { pointsOnSurface[j], pointsOnSurface[j + 1] }, 0.01);
                    surfaceCurves.Add(curve);
                }

                Curve[] surfaceCurve = Curve.JoinCurves(surfaceCurves, 0.01); // Join the segments
                stackCurves.Add(new GH_Curve(surfaceCurve[0]));
            }
            return stackCurves;
        }

        public override List<List<GH_Plane>> CreateStackOrientPlanes( ref List<List<GH_Number>> speedFactor)
        {
            List<GH_Curve> gH_Curves = LayerCurves;
            List<GH_Surface> gH_Surfaces = Surfaces;

            List<List<GH_Plane>> planesStructure = new List<List<GH_Plane>>();
            

            for (int i = 0;i < gH_Curves.Count; i++)
            {
                List<GH_Plane> planesThis = new List<GH_Plane>();
                List<GH_Number> doublesThis = new List<GH_Number>();
                List<Point3d> toolpathExplodedPts = PointDataUtils.GetExplodedCurveVertices(gH_Curves[i].Value);
                Surface surface = gH_Surfaces[i].Value.Surfaces[0];
                Surface preSurface = null;
                if (i != 0)
                {
                    preSurface = gH_Surfaces[i - 1].Value.Surfaces[0];
                }

                foreach (Point3d pt in toolpathExplodedPts)
                {
                    Vector3d xDir =  pt -  PlaneRefPt;
                    xDir.Z = 0; // project the vector on the global xy plane

                    Plane newPlane = new Plane();
                    if (AngleGlobal)
                    {
                        Vector3d zDir = new Vector3d(0, 0, 1);
                        Vector3d yDir = Vector3d.CrossProduct(zDir, xDir);
                        newPlane = new Plane(pt, xDir, yDir);
                    }
                    else
                    {
                        double u, v;
                        Vector3d normal = new Vector3d();
                        if (surface.ClosestPoint(pt, out u, out v))
                        {
                            normal = surface.NormalAt(u, v);
                        }
                        Vector3d newYAxis = Vector3d.CrossProduct(xDir, -normal);
                        Vector3d newXAxis = Vector3d.CrossProduct(-normal, newYAxis);
                        newPlane = new Plane(pt, newXAxis ,newYAxis);

                        //////////////////////
                        // Rotate the plane around Y axis
                        double angleInRadians = Rhino.RhinoMath.ToRadians(5); //TODO: make this an input of the component
                        Vector3d rotationAxis = newPlane.YAxis;
                        Transform rotation = Transform.Rotation(-angleInRadians, rotationAxis, newPlane.Origin);
                        newPlane.Transform(rotation);
                    }
                    planesThis.Add(new GH_Plane(newPlane));

                    // Calculate speed
                    if (i==0) doublesThis.Add(new GH_Number(1));
                    else
                    {
                        // Calculate distance between this point and previous layer
                        preSurface.ClosestPoint(pt, out double u, out double v);
                        Point3d closestPointOnSurface = surface.PointAt(u, v);
                        double distance = pt.DistanceTo(closestPointOnSurface);

                        doublesThis.Add(new GH_Number(distance/LayerHeight)); // TODO: should be rounded?
                    }
                }

                speedFactor.Add(doublesThis);
                planesStructure.Add(planesThis);
            }


            return planesStructure;

        }
    }
}
