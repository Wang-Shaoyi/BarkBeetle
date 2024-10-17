﻿using BarkBeetle.GeometriesPackage;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BarkBeetle.ToolpathBaseSetting;
using Grasshopper.Kernel.Types;

namespace BarkBeetle.ToolpathStackSetting
{
    internal abstract class ToolpathStack
    {
        /// <summary>
        /// /////////////////////////////////
        /// </summary>
        //Properties

        public virtual string ToolpathStackName { get; set; } = "stack";
        private GH_Curve finalCurve { get; set; } //The final continuous curve
        public GH_Curve FinalCurve
        {
            get { return finalCurve; }
            set { finalCurve = value; }
        }

        private List<GH_Curve> layerCurves { get; set; } //One curve per layer
        public List<GH_Curve> LayerCurves
        {
            get { return layerCurves; }
            set { layerCurves = value; }
        }

        private List<GH_Surface> surfaces { get; set; } //One surface per layer
        public List<GH_Surface> Surfaces
        {
            get { return surfaces; }
        }

        private ToolpathBase toolpathBase { get; set; }
        public ToolpathBase _ToolpathBase
        {
            get { return toolpathBase; }
        }

        private Plane globalReferencePlane { get; set; }
        public Plane GlobalReferencePlane
        {
            get { return globalReferencePlane; }
        }

        private List<List<GH_Plane>> orientPlanes { get; set; } // final planes for orientation
        public List<List<GH_Plane>> OrientPlanes
        {
            get { return orientPlanes; }
            set { orientPlanes = value; }
        }

        private List<List<GH_Number>> speedFactors { get; set; } // speedfactor at each point. 0.5 = median, 1 = max, 0 = min
        public List<List<GH_Number>> SpeedFactors
        {
            get { return speedFactors; }
            set {  speedFactors = value; }
        }

        private double layerHeight { get; set; }
        public double LayerHeight
        {
            get { return layerHeight; }
        }

        private int layerNum { get; set; }  //Will calculate in each subclass
        public int LayerNum
        {
            get { return layerNum; }
            set { layerNum = value; }
        }

        private bool angleGlobal { get; set; } // true: global z axis; false: local z axis
        public bool AngleGlobal
        {
            get { return angleGlobal; }
        }


        public ToolpathStack(ToolpathBase tb, Plane plane, double h, bool ag)
        {
            
        }

        public void PerformCustomLogic(ToolpathBase tb, Plane plane, double h, bool ag)
        {
            toolpathBase = tb;
            globalReferencePlane = plane;
            layerHeight = h;
            angleGlobal = ag;

            surfaces = CreateStackSurfaces();

            layerCurves = CreateStackLayerCurves();
            finalCurve = CreateStackFinalCurve();

            List<List<GH_Number>> sf = new List<List<GH_Number>>();
            orientPlanes = CreateStackOrientPlanes(ref sf); // Will calculate speed factor here
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
        public abstract List<List<GH_Plane>> CreateStackOrientPlanes(ref List<List<GH_Number>> speedFactor);


        //public GH_Curve CreateStackFinalCurve()
        //{
        //    List<GH_Curve> finalCrvs = new List<GH_Curve>();
        //    PolyCurve polyCurve = new PolyCurve();

        //    for (int i = 0; i < LayerCurves.Count; i++)
        //    {
        //        polyCurve.Append(LayerCurves[i].Value);

        //        // If not the final curve
        //        if (i < LayerCurves.Count - 1)
        //        {
        //            Curve currentCurve = LayerCurves[i].Value;
        //            Curve nextCurve = LayerCurves[i + 1].Value;

        //            Point3d endPt = currentCurve.PointAtEnd;
        //            Point3d startPt = nextCurve.PointAtStart;

        //            Line connectingLine = new Line(endPt, startPt);

        //            polyCurve.Append(new LineCurve(connectingLine));
        //        }
        //    }
        //    return new GH_Curve(polyCurve);
        //}
    }
}
