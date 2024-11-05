
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel.Types;
using BarkBeetle.Pattern;
using System.Runtime.InteropServices;

namespace BarkBeetle.ToolpathStackSetting
{
    internal abstract class ToolpathStack
    {
        /// <summary>
        /// /////////////////////////////////
        /// </summary>
        //Properties

        // 0 Pattern
        private StackPatterns patterns { get; set; }
        public StackPatterns Patterns
        {
            get { return patterns; }
        }

        // 1 Final continuous curve
        private GH_Curve finalCurve { get; set; } //The final continuous curve
        public GH_Curve FinalCurve
        {
            get { return finalCurve; }
            set { finalCurve = value; }
        }

        // 2 Each layer curve
        private List<GH_Curve> layerCurves { get; set; } //One curve per layer
        public List<GH_Curve> LayerCurves
        {
            get { return layerCurves; }
            set { layerCurves = value; }
        }

        // 3 Surfaces that every curve was generated from 
        private List<GH_Surface> surfaces { get; set; } //One surface per layer
        public List<GH_Surface> Surfaces
        {
            get { return surfaces; }
        }

        // 4 Plane (Frame) Reference orientation
        private Point3d planeRefPt { get; set; }
        public Point3d PlaneRefPt
        {
            get { return planeRefPt; }
            set { planeRefPt = value; }
        }

        // 5 Planes on the toolpath
        private List<List<GH_Plane>> orientPlanes { get; set; } // final planes for orientation
        public List<List<GH_Plane>> OrientPlanes
        {
            get { return orientPlanes; }
            set { orientPlanes = value; }
        }

        // 6 speedfactor at each point, caculated from layer height
        private List<List<GH_Number>> speedFactors { get; set; } // speedfactor at each point.
        public List<List<GH_Number>> SpeedFactors
        {
            get { return speedFactors; }
            set {  speedFactors = value; }
        }

        // 7 layer height
        private double layerHeight { get; set; }
        public double LayerHeight
        {
            get { return layerHeight; }
        }

        // 8 total number of layers
        private int layerNum { get; set; }  //Will calculate in each subclass
        public int LayerNum
        {
            get { return layerNum; }
            set { layerNum = value; }
        }

        // 9 Whether z axis is the global axis or is z surface on the surface
        private bool angleGlobal { get; set; } // true: global z axis; false: local z axis
        public bool AngleGlobal
        {
            get { return angleGlobal; }
        }

        // 9 Whether z axis is the global axis or is z surface on the surface
        private double rotateAngle { get; set; } // true: global z axis; false: local z axis
        public double RotateAngle
        {
            get { return rotateAngle; }
        }

        public ToolpathStack(StackPatterns sp,  double h, bool ag, Point3d refPt, double angle) { }

        public void GenerateToolpathStack(StackPatterns sp,  double h, bool ag, Point3d refPt, double angle)
        {
            patterns = sp;
            layerHeight = h;
            angleGlobal = ag;
            planeRefPt = refPt;
            rotateAngle = angle;

            surfaces = CreateStackSurfaces();

            layerCurves = CreateStackLayerCurves();
            finalCurve = CreateStackFinalCurve();

            List<List<GH_Number>> sf = new List<List<GH_Number>>();
            orientPlanes = CreateStackOrientPlanes(angle, ref sf); // Will calculate speed factor here
            speedFactors = sf;
        }

        public abstract List<GH_Surface> CreateStackSurfaces();
        public abstract List<GH_Curve> CreateStackLayerCurves();
        public GH_Curve CreateStackFinalCurve()
        {
            PolyCurve polyCurve = new PolyCurve();

            for (int i = 0; i < LayerCurves.Count; i++)
            {
                Curve currentCurve = LayerCurves[i].Value;

                polyCurve.Append(currentCurve);
                if (i < LayerCurves.Count - 1)
                {
                    Curve nextCurve = LayerCurves[i + 1].Value;
                    Curve blend = Curve.CreateBlendCurve(currentCurve, nextCurve, BlendContinuity.Position);
                    polyCurve.Append(blend);
                }

            }

            return new GH_Curve(polyCurve);
        }
        public abstract List<List<GH_Plane>> CreateStackOrientPlanes(double angle, ref List<List<GH_Number>> speedFactor);


    }
}
