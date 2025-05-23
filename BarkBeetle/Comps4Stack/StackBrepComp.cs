﻿using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using BarkBeetle.ToolpathStackSetting;
using BarkBeetle.Utils;
using BarkBeetle.Pattern;
using Grasshopper.Kernel.Data;
using System.Collections;
using System.Runtime.InteropServices;

namespace BarkBeetle.Comps4Stack
{
    /*
    public class StackBrepComp : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ToolpathStackVertical class.
        /// </summary>
        public StackBrepComp()
          : base("Toolpath Stack in Brep", "Stack Brep Toolpath",
              "[Note: This component is still under develop, and the result is not accurate.]Stack toolpath layers in a Brep, a close brep is required",
              "BarkBeetle", "4-Stack")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Stack Patterns", "Patterns", "BarkBeetle Stack Patterns object", GH_ParamAccess.item);
            pManager.AddNumberParameter("Layer Height", "h", "Height of a single layer", GH_ParamAccess.item);
            pManager.AddBrepParameter("Brep", "B", "Brep to generate toolpath stack", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Orient Option", "Orient", "Frame z axis global or local(true: global; false: local)", GH_ParamAccess.item, false);
            pManager.AddPointParameter("Reference Point", "Pt", "Reference point for frame orientation", GH_ParamAccess.item, Point3d.Origin);
            pManager.AddNumberParameter("Plane Rotate Angle", "Angle", "Rotation towards the reference point", GH_ParamAccess.item, 0.0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpath Stack", "TS", "BarkBeetle ToolpathStack object", GH_ParamAccess.item);
            pManager.AddCurveParameter("Toolpath Curve", "C", "Continuous toolpath curve", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Toolpath Frames", "TS", "Toolpath frames", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Initialize
            StackPatternsGoo goo = null;
            double layerH = 0;
            Brep brep = null;
            bool angleGlobal = true;
            Point3d refPt = new Point3d();
            double angle = 0.0;

            //Set inputs
            if (!DA.GetData(0, ref goo)) return;
            StackPatterns stackPatterns = goo.Value;

            if (!DA.GetData(1, ref layerH)) return;
            if (!DA.GetData(2, ref brep)) return;
            if (!DA.GetData(3, ref angleGlobal)) return;
            if (!DA.GetData(4, ref refPt)) return;
            if (!DA.GetData(5, ref angle)) return;


            // Error message.

            // Run Function
            StackBrep toolpathStack = new StackBrep(stackPatterns, layerH, angleGlobal, brep, refPt, angle);
            ToolpathStackGoo stackGoo = new ToolpathStackGoo(toolpathStack);

            GH_Curve gH_Curve = toolpathStack.FinalCurve;
            List<List<GH_Plane>> frames = toolpathStack.OrientPlanes;
            GH_Structure<GH_Plane> frameTree = TreeHelper.ConvertToGHStructure(frames);

            // Finally assign the spiral to the output parameter.
            DA.SetData(0, stackGoo);
            DA.SetData(1, gH_Curve);
            DA.SetDataTree(2, frameTree);

            var param = Params.Output[2] as IGH_PreviewObject;
            if (param != null)
            {
                param.Hidden = true;
            }
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Resources.SurfaceBetweenStack;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("C6B71CB4-8537-459E-90F3-EE32CFB68CE1"); }
        }
    }
    */
}