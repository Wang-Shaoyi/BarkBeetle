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
    internal class StackBetweenProject : ToolpathStack
    {
        public Surface topSrf = null;

        public StackBetweenProject(StackPatterns tb,  double h, bool ag, Surface topS, GeometryBase refGeo, double angle) : base(tb,h,ag, refGeo, angle) 
        {
            topSrf = topS;
            GenerateToolpathStack(tb, h, ag, refGeo, angle);
        }

        public override List<GH_Surface> CreateStackSurfaces()
        {

            Surface baseSurface = null;
            if (Patterns.BottomPattern != null) baseSurface = Patterns.BottomPattern.BaseSrf;
            else baseSurface = Patterns.MainPatterns[0].BaseSrf;

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

            /////////// Create stack curve list/////////
            List<GH_Curve> stackCurves = new List<GH_Curve>();
            Surface baseSrf = Surfaces[0].Value.Surfaces[0];
            Interval uDomainBase = baseSrf.Domain(0);
            Interval vDomainBase = baseSrf.Domain(1);

            bool flipCrv = false;

            for (int i = 0; i < LayerNum - 1; i++)
            {
                Curve baseCurve = allPatternCurves[i];
                
                List<Point3d> points = CurveUtils.GetExplodedCurveVertices(baseCurve, LayerHeight * 5);

                // Get current surface
                Surface srf = Surfaces[i].Value.Surfaces[0];

                // Project to srf
                Curve[] projectedCurves = Curve.ProjectToBrep(baseCurve, srf.ToBrep(), Vector3d.ZAxis, Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);

                Curve resultCrv = projectedCurves[0];
                if (flipCrv) resultCrv.Reverse();
                flipCrv = !flipCrv;

                stackCurves.Add(new GH_Curve(resultCrv));
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
                List<Point3d> toolpathExplodedPts = CurveUtils.GetExplodedCurveVertices(gH_Curves[i].Value, LayerHeight * 5);
                Surface surface = gH_Surfaces[i].Value.Surfaces[0];
                Surface nextSurface = null;

                nextSurface = gH_Surfaces[i + 1].Value.Surfaces[0];

                foreach (Point3d pt in toolpathExplodedPts)
                {
                    Vector3d xDir = Vector3d.YAxis;
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

                    // Calculate distance between this point and previous layer
                    nextSurface.ClosestPoint(pt, out double u1, out double v1);
                    Point3d closestPointOnSurface = nextSurface.PointAt(u1, v1);
                    double distance = pt.DistanceTo(closestPointOnSurface);

                    doublesThis.Add(new GH_Number(LayerHeight / distance)); // TODO: should be rounded?
                }

                speedFactor.Add(doublesThis);
                planesStructure.Add(planesThis);
            }


            return planesStructure;

        }
    }
}
