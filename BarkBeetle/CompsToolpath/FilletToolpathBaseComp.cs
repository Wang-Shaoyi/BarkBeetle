using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;

using BarkBeetle.ToolpathSetting;

namespace BarkBeetle.CompsToolpath
{
    public class FilletToolpathBaseComp : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the FilletToolpathBase class.
        /// </summary>
        public FilletToolpathBaseComp()
          : base("Fillet toolpathBase on surface", "Fillet",
              "Fillets BarkBeetle generated non-planar toolpath",
              "BarkBeetle", "Toolpath")
        {
        }

        private Curve cachedCurve = null;
        private bool previousState = false;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "C", "Toolpath curve", GH_ParamAccess.item);
            pManager.AddNumberParameter("Radius", "r", "Radius for fillet", GH_ParamAccess.item, 1.0); //default is here
            pManager.AddSurfaceParameter("Surface","S", "Surface that the toolpath was built upon", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Trigger", "T", "Run and update this component", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "C", "Finished Curve", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve toolpath = null;
            double r = 0.0;
            Surface surface = null;
            bool trigger = false;

            if (!DA.GetData(0, ref toolpath)) return;
            if (!DA.GetData(1, ref r)) return;
            if (!DA.GetData(2, ref surface)) return;
            if (!DA.GetData(3, ref trigger)) return;

            if (r <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "radius must be larger than 0");
                return;
            }

            if (trigger && !previousState)
            {
                cachedCurve = ToolpathUtils.FilletToolpathBaseOnSurface(toolpath, r, surface);
            }

            previousState = trigger;

            DA.SetData(0, cachedCurve);
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
                return null;
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