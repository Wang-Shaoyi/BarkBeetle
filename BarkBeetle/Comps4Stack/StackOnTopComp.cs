using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using BarkBeetle.Skeletons;
using BarkBeetle.Pattern;
using BarkBeetle.ToolpathStackSetting;
using BarkBeetle.Utils;
using Grasshopper.Kernel.Data;

namespace BarkBeetle.Comps4Stack
{
    public class StackOnTopComp : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ToolpathStackVertical class.
        /// </summary>
        public StackOnTopComp()
          : base("Stack On Top", "Stack On Top",
              "Create a new stack from a pattern on the top of a given stack. In this case the are separated toolpaths that align with each other.",
              "BarkBeetle", "4-Stack")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpath Stack", "TS", "BarkBeetle ToolpathStack object", GH_ParamAccess.item);
            pManager.AddGenericParameter("Toolpath Pattern", "TP", "BarkBeetle Toolpath Pattern object", GH_ParamAccess.item);
            pManager.AddNumberParameter("Layer Height", "h", "Height of a single layer", GH_ParamAccess.item);
            pManager.AddNumberParameter("Total Height", "H", "Total Height", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpath Stack", "TS", "BarkBeetle ToolpathStack object", GH_ParamAccess.item);
            pManager.AddCurveParameter("Toolpath Curve", "C", "Continuous toolpath curve", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Toolpath Planes", "Planes", "Toolpath planes", GH_ParamAccess.tree);
            pManager.AddGenericParameter("Mapped Toolpath Pattern", "Mapped Pattern", "BarkBeetle Toolpath Pattern object", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            // Initialize
            ToolpathStackGoo stackGoo = null;
            ToolpathPatternGoo patternGoo = null;

            //Set inputs
            if (!DA.GetData(0, ref stackGoo)) return;
            ToolpathStack toolpathStack = stackGoo.Value;
            if (!DA.GetData(1, ref patternGoo)) return;
            ToolpathPattern toolpathPattern = patternGoo.Value;

            // Initialize
            double layerH = 0;
            double totalH = 0;

            //Set inputs
            if (!DA.GetData(2, ref layerH)) return;
            if (!DA.GetData(3, ref totalH)) return;


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
            ToolpathStack newStack = StackOnTop.CreatStackOnTop(toolpathStack, toolpathPattern, layerH, totalH, out ToolpathPattern newPattern);
            ToolpathStackGoo newStackGoo = new ToolpathStackGoo(newStack);
            ToolpathPatternGoo newPatternGoo = new ToolpathPatternGoo(newPattern);

            GH_Curve gH_Curve = newStack.FinalCurve;
            List<List<GH_Plane>> frames = newStack.OrientPlanes;
            GH_Structure<GH_Plane> frameTree = TreeHelper.ConvertToGHStructure(frames);

            // Finally assign the spiral to the output parameter.
            DA.SetData(0, newStackGoo);
            DA.SetData(1, gH_Curve);
            DA.SetDataTree(2, frameTree);
            DA.SetData(3, newPatternGoo);

            var param = Params.Output[2] as IGH_PreviewObject;
            if (param != null)
            {
                param.Hidden = true;
            }
        }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Resources.edge_beam;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("E35F7686-13ED-4EA3-BDA5-05084C1F3E9D"); }
        }
    }
}