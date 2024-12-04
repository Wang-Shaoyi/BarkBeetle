using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using BarkBeetle.ToolpathStackSetting;
using Grasshopper.Kernel.Types;

namespace BarkBeetle.Comps4Stack
{
    public class FilletToolpathStackComp : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the FilletToolpathBase class.
        /// </summary>
        public FilletToolpathStackComp()
          : base("Fillet toolpath from ToolpathStack", "Fillet Toolpath",
              "Fillets BarkBeetle generated non-planar toolpath",
              "BarkBeetle", "4-Stack")
        {
        }

        private GH_Curve cachedCurve = null;
        private ToolpathStack newToolpathStack = null;
        private bool previousState = false;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("ToolpathStack", "TS", "BarkBeetle ToolpathStack object", GH_ParamAccess.item);
            pManager.AddNumberParameter("Radius", "r", "Radius for fillet", GH_ParamAccess.item, 1.0); //default is here
            pManager.AddBooleanParameter("Trigger", "T", "Run and update this component", GH_ParamAccess.item, false);
            pManager.AddNumberParameter("SeamLength Factor", "f", "Length factor of the seam area (compared to radius)", GH_ParamAccess.item, 10);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "C", "Finished Curve", GH_ParamAccess.item);
            pManager.AddGenericParameter("ToolpathStack", "TS", "BarkBeetle ToolpathStack object", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            ToolpathStackGoo goo = null;
            double f = 10;
            double r = 0.0;
            bool trigger = false;

            if (!DA.GetData(0, ref goo)) return;
            ToolpathStack toolpathStack = goo.Value;

            if (!DA.GetData(1, ref r)) return;
            if (!DA.GetData(2, ref trigger)) return;
            if (!DA.GetData(3, ref f)) return;

            if (r <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "radius must be larger than 0");
                return;
            }


            if (trigger && !previousState)
            {
                cachedCurve = ToolpathFillet.FilletContinuousToolpathStackByLayers(toolpathStack, r, f, ref newToolpathStack);
            }

            previousState = trigger;

            DA.SetData(0, cachedCurve);
            DA.SetData(1, new ToolpathStackGoo(newToolpathStack));
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
                return Resources.FilletToolpath;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("ACA79928-4E69-48F8-9CDC-9D89DE663833"); }
        }
    }
}