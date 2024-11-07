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
    internal class StackBetween : ToolpathStack
    {
        Surface topSrf = null;

        public StackBetween(StackPatterns tb,  double h, bool ag, Surface topS, Point3d refPt, double angle) : base(tb,h,ag, refPt, angle) 
        {
            topSrf = topS;
            GenerateToolpathStack(tb, h, ag, refPt, angle);
        }

        public override List<GH_Surface> CreateStackSurfaces()
        {

            Surface baseSurface = null;
            if (Patterns.BottomPattern != null) baseSurface = Patterns.BottomPattern.Skeleton.UVNetwork.ExtendedSurface;
            else baseSurface = Patterns.MainPatterns[0].Skeleton.UVNetwork.ExtendedSurface;

            LayerNum = (int)(BrepUtils.AverageSurfaceDistance(baseSurface, topSrf, 6)/LayerHeight) +1;

            List<GH_Surface> stackSurfaces = BrepUtils.TweenBetweenSurfaces(baseSurface, topSrf, LayerNum);
            
            return stackSurfaces;
        }

        public override List<GH_Curve> CreateStackLayerCurves() 
        {
            /////////// Create pattern curve list/////////
            List<Curve> allPatternCurves = new List<Curve>();
            int repeatCount = LayerNum - (Patterns.TopCount + Patterns.BottomCount);
            int mainCount = Patterns.MainPatterns.Count;

            if (repeatCount > 0 && Patterns.MainPatterns != null)
            {
                List<Curve> main = new List<Curve>();
                foreach (var pattern in Patterns.MainPatterns)
                {
                    main.Add(pattern.CoutinuousCurve);
                }
                for (int i = 0; i < repeatCount; i++)
                {
                    allPatternCurves.Add(main[i % mainCount]);
                }
            }

            if (Patterns.BottomPattern != null && Patterns.BottomCount != 0)
            {
                for (int i = 0; i < Patterns.BottomCount; i++) allPatternCurves.Insert(0, Patterns.BottomPattern.CoutinuousCurve);
            }

            if (Patterns.TopPattern != null && Patterns.TopCount != 0)
            {
                for (int i = 0; i < Patterns.TopCount; i++) allPatternCurves.Add(Patterns.TopPattern.CoutinuousCurve);
            }

            List<GH_Curve> stackCurves = new List<GH_Curve>();
            Surface baseSrf = Surfaces[0].Value.Surfaces[0];
            Interval uDomainBase = baseSrf.Domain(0);
            Interval vDomainBase = baseSrf.Domain(1);

            for (int i = 0; i < LayerNum; i++)
            {
                Curve baseCurve = allPatternCurves[i];
                List<Point3d> points = PointDataUtils.GetExplodedCurveVertices(baseCurve);

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

        public override List<List<GH_Plane>> CreateStackOrientPlanes(double angle, ref List<List<GH_Number>> speedFactor)
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
                Surface nextSurface = null;
                if (i != gH_Curves.Count - 1)
                {
                    nextSurface = gH_Surfaces[i + 1].Value.Surfaces[0];
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
                        double angleInRadians = Rhino.RhinoMath.ToRadians(angle);
                        Vector3d rotationAxis = newPlane.YAxis;
                        Transform rotation = Transform.Rotation(-angleInRadians, rotationAxis, newPlane.Origin);
                        newPlane.Transform(rotation);
                    }
                    planesThis.Add(new GH_Plane(newPlane));

                    // Calculate speed
                    if (i== gH_Curves.Count - 1) doublesThis.Add(new GH_Number(1));
                    else
                    {
                        // Calculate distance between this point and previous layer
                        nextSurface.ClosestPoint(pt, out double u, out double v);
                        Point3d closestPointOnSurface = nextSurface.PointAt(u, v);
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
