using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using BarkBeetle.ToolpathStackSetting;
using BarkBeetle.Utils;

namespace BarkBeetle.CompsVisualization
{
    public class ToolpathVisualization : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ToolpathVisualization class.
        /// </summary>
        public ToolpathVisualization()
          : base("Toolpath Mesh", "Toolpath Mesh",
              "Create toolpath mesh from ToolpathStack",
              "BarkBeetle", "6-Display & Analysis")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpath Stack", "Stack", "BarkBeetle Stack object", GH_ParamAccess.item);
            pManager.AddNumberParameter("Parameter", "Parameter", "Part of the toolpath to display", GH_ParamAccess.item, 1);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Toolpath Mesh", "Mesh", "Toolpath Mesh", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Initialize
            ToolpathStackGoo goo = null;
            double p = 0.0;

            //Set inputs
            if (!DA.GetData(0, ref goo)) return;
            ToolpathStack toolpathStack = goo.Value;

            if (!DA.GetData(1, ref p)) return;

            if (p > 1 || p <0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "parameter should be between 0 and 1");
                return;
            }

            if (toolpathStack == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "no ToolpathStack");
                return;
            }

            Mesh mesh = MeshUtils.MeshFromToolpathStack(toolpathStack, p);

            DA.SetData(0, mesh);

        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Resources.ToolpathToMesh;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("D8B011FC-1BFF-412A-97DC-CF614F5352DA"); }
        }
    }
}