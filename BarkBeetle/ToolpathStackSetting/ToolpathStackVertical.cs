using BarkBeetle.ToolpathBaseSetting;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;

using BarkBeetle.Utils;
using System.Security.Cryptography;

namespace BarkBeetle.ToolpathStackSetting
{
    internal class ToolpathStackVertical:ToolpathStack
    {
        public override string ToolpathStackName { get; set; } = "Vertical";

        double totalHeight = 0;

        public ToolpathStackVertical(ToolpathBase tb, Plane plane, double h, bool ag, double totalH) : base(tb,plane,h,ag) 
        {
            totalHeight = totalH;
            PerformCustomLogic(tb, plane, h, ag);
        }

        public override List<GH_Surface> CreateStackSurfaces()
        {
            LayerNum = (int)(totalHeight / LayerHeight);

            Surface baseSurface = _ToolpathBase.SkeletonPackage.ExtendedSurface;

            List<GH_Surface> stackSurfaces = new List<GH_Surface>();
            
            for (int i = 0; i < LayerNum; i++)
            {
                Surface dupSurface = baseSurface.Duplicate() as Surface;
                Transform translation = Transform.Translation(new Vector3d(0, 0, i * LayerHeight));
                dupSurface.Transform(translation);
                stackSurfaces.Add(new GH_Surface(dupSurface));
            }
            return stackSurfaces;
        }
        public override List<GH_Curve> CreateStackLayerCurves() 
        {
            Curve baseCurve = _ToolpathBase.Curve;
            List<GH_Curve> stackCurves = new List<GH_Curve>();
            
            for (int i = 0; i < LayerNum; i++)
            {
                Curve dupCurve = baseCurve.Duplicate() as Curve;
                Transform translation = Transform.Translation(new Vector3d(0, 0, i * LayerHeight));
                dupCurve.Transform(translation);
                stackCurves.Add(new GH_Curve(dupCurve));
            }
            return stackCurves;
        }

        public override List<List<GH_Plane>> CreateStackOrientPlanes(ref List<List<GH_Number>> speedFactor)
        {
            List<GH_Curve> gH_Curves = LayerCurves;
            List<GH_Surface> gH_Surfaces = Surfaces;


            Point3d refOrigin = GlobalReferencePlane.Origin;
            Vector3d refX = GlobalReferencePlane.XAxis;
            Vector3d refY = GlobalReferencePlane.YAxis;

            List<List<GH_Plane>> planesStructure = new List<List<GH_Plane>>();
            

            for (int i = 0;i < gH_Curves.Count; i++)
            {
                List<GH_Plane> planesThis = new List<GH_Plane>();
                List<GH_Number> doublesThis = new List<GH_Number>();
                List<Point3d> toolpathExplodedPts = PointDataUtils.GetExplodedCurveVertices(gH_Curves[i].Value);
                Surface surface = gH_Surfaces[i].Value.Surfaces[0];

                foreach (Point3d pt in toolpathExplodedPts)
                {
                    Plane newPlane = new Plane();
                    if (AngleGlobal)
                    {
                        newPlane = new Plane(pt, refX, refY);
                    }
                    else
                    {
                        double u, v;
                        Vector3d normal = new Vector3d();
                        if (surface.ClosestPoint(pt, out u, out v))
                        {
                            normal = surface.NormalAt(u, v);
                        }
                        Vector3d newYAxis = Vector3d.CrossProduct(refY, normal);
                        Vector3d newXAxis = Vector3d.CrossProduct(normal, newYAxis);
                        newPlane = new Plane(pt, newXAxis, newYAxis);
                    }
                    planesThis.Add(new GH_Plane(newPlane));
                    doublesThis.Add(new GH_Number(0.5));
                }

                speedFactor.Add(doublesThis);
                planesStructure.Add(planesThis);
            }


            return planesStructure;

        }
    }
}
