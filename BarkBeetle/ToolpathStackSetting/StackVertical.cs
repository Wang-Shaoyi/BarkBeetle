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
    internal class StackVertical:ToolpathStack
    {

        double totalHeight = 0;

        public StackVertical(StackPatterns sp,  double h, bool ag, double totalH, Point3d refPt, double angle) : base(sp, h, ag, refPt, angle) 
        {
            totalHeight = totalH;
            GenerateToolpathStack(sp, h, ag, refPt, angle);
        }

        public override List<GH_Surface> CreateStackSurfaces()
        {
            LayerNum = (int)(totalHeight / LayerHeight);
            Surface baseSurface = null;

            if (Patterns.BottomPattern != null) baseSurface = Patterns.BottomPattern.Skeleton.UVNetwork.ExtendedSurface;
            else baseSurface = Patterns.MainPatterns[0].Skeleton.UVNetwork.ExtendedSurface;

            List<GH_Surface> stackSurfaces = new List<GH_Surface>();

            for (int i = 0; i < LayerNum; i++)
            {
                Surface dupSurface = baseSurface.Duplicate() as Surface;
                double offsetDistance = i * LayerHeight;

                // Check normal direction
                Vector3d normal = dupSurface.NormalAt(0.5, 0.5); 
                if (normal.Z < 0)
                {
                    offsetDistance = -offsetDistance; // if normal is negative, flip offset direction
                }
                
                Surface offsetSurface = dupSurface.Offset(offsetDistance, 0.01);
                stackSurfaces.Add(new GH_Surface(offsetSurface));
            }

            return stackSurfaces;
        }

        public override List<GH_Curve> CreateStackLayerCurves() 
        {
            /////////// Create pattern curve list/////////
            List<Curve> allPatternCurves = new List<Curve>();
            int repeatCount = LayerNum - (Patterns.TopCount + Patterns.BottomCount);

            if (repeatCount > 0 && Patterns.MainPatterns.Count > 0)
            {
                List<Curve> main = new List<Curve>();
                foreach (var pattern in Patterns.MainPatterns)
                {
                    main.Add(pattern.CoutinuousCurve);
                }
                for (int i = 0; i < repeatCount; i++)
                {
                    allPatternCurves.Add(main[i % Patterns.MainPatterns.Count]);
                }
            }

            if (Patterns.BottomPattern != null && Patterns.BottomCount != 0)
            {
                for (int i = 0; i < Patterns.BottomCount; i++) allPatternCurves.Insert(0,Patterns.BottomPattern.CoutinuousCurve);
            }

            if (Patterns.TopPattern != null && Patterns.TopCount != 0)
            {
                for (int i = 0; i < Patterns.TopCount; i++) allPatternCurves.Add(Patterns.TopPattern.CoutinuousCurve);
            }

            /////////// Create pattern curve list/////////

            List<GH_Curve> stackCurves = new List<GH_Curve>();
            
            for (int i = 0; i < LayerNum; i++)
            {
                Curve baseCurve = allPatternCurves[i];
                List<Point3d> points = PointDataUtils.GetExplodedCurveVertices(baseCurve);
                // Get current surface
                Surface srf = Surfaces[i].Value.Surfaces[0];

                // Pull points on surface
                List<Point3d> pointsOnSurface = new List<Point3d>();
                foreach(Point3d pt in points)
                {
                    double u, v;
                    srf.ClosestPoint(pt, out u, out v);
                    Point3d pt3dOnSurf = srf.PointAt(u, v);
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

                foreach (Point3d pt in toolpathExplodedPts)
                {
                    Vector3d xDir =  pt -  PlaneRefPt;
                    xDir.Z = 0; // project onto xy plane

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
                    doublesThis.Add(new GH_Number(1));
                }

                speedFactor.Add(doublesThis);
                planesStructure.Add(planesThis);
            }


            return planesStructure;

        }
    }
}
