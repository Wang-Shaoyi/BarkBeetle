using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using BarkBeetle.ToolpathBaseSetting;
using BarkBeetle.ToolpathStackSetting;
using BarkBeetle.Utils;
using Grasshopper.Kernel.Data;

namespace BarkBeetle.CompsToolpath
{
    public class ToolpathStackVerticalComp : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ToolpathStackVertical class.
        /// </summary>
        public ToolpathStackVerticalComp()
          : base("ToolpathStackVertical", "Vertical Toolpath",
              "Stack toolpath layers vertically",
              "BarkBeetle", "Toolpath")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpath Base", "TB", "BarkBeetle ToolpathBase object", GH_ParamAccess.item);
            pManager.AddNumberParameter("Layer Height", "h", "Height of a single layer", GH_ParamAccess.item);
            pManager.AddNumberParameter("Total Height", "H", "Total Height", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Orient Option", "Orient", "Frame z axis global or local(true: global; false: local)", GH_ParamAccess.item, true);
            pManager.AddPlaneParameter("Reference Plane", "Plane", "Reference plane for frame orientation", GH_ParamAccess.item, Plane.WorldXY);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
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
            ToolpathBaseGoo goo = null;
            double layerH = 0;
            double totalH = 0;
            bool angleGlobal = true;
            Plane refPlane = new Plane();

            //Set inputs
            if (!DA.GetData(0, ref goo)) return;
            ToolpathBase toolpathBase = goo.Value;

            if (!DA.GetData(1, ref layerH)) return;
            if (!DA.GetData(2, ref totalH)) return;
            if (!DA.GetData(3, ref angleGlobal)) return;
            if (!DA.GetData(4, ref refPlane)) return;


            // Error message.
            if (totalH <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Total height must be larger than 0");
                return;
            }
            if (layerH <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Layer height must be larger than 0");
                return;
            }

            // Run Function
            ToolpathStackVertical toolpathStack = new ToolpathStackVertical(toolpathBase,refPlane,layerH,angleGlobal,totalH);
            ToolpathStackGoo stackGoo = new ToolpathStackGoo(toolpathStack);

            GH_Curve gH_Curve = toolpathStack.FinalCurve;
            List<List<GH_Plane>> frames = toolpathStack.OrientPlanes;
            GH_Structure<GH_Plane> frameTree = TreeHelper.ConvertToGHStructure(frames);

            // Finally assign the spiral to the output parameter.
            DA.SetData(0, stackGoo);
            DA.SetData(1, gH_Curve);
            DA.SetDataTree(2, frameTree);

            var param = this.Params.Output[2] as IGH_PreviewObject;
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
                return Resources.VerticalStack;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("325070F8-F91D-4788-861B-A9DBC64B3C2F"); }
        }
    }
}