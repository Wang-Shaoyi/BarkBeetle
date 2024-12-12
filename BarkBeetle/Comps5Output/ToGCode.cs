using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using BarkBeetle.Utils;
using Grasshopper.Kernel.Data;
using BarkBeetle.ToolpathStackSetting;

namespace BarkBeetle.CompsToolpathOutput
{
    public class ToGCode : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Extract_RefinedGeometry class.
        /// </summary>
        public ToGCode()
          : base("To GCode", "To GCode",
              "Transfrom Toolpath Stack to Kuka movement (e.g., LINear Movement)",
              "BarkBeetle", "5-Output")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpath Stack", "TS", "BarkBeetle ToolpathStack object", GH_ParamAccess.item);
            pManager.AddNumberParameter("Vel Max", "Vel", "Maximum velocity", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Toolpath Planes", "Planes", "Toolpath planes", GH_ParamAccess.list);
            pManager.AddNumberParameter("Speed Factors", "Speed", "Speed factors for each toolpath frame, 0.5 = median, 1 = max, 0 = min", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Initialize
            ToolpathStackGoo goo = null;
            double maxSpeed = 0;

            //Set inputs
            if (!DA.GetData(0, ref goo)) return;
            ToolpathStack toolpathStack = goo.Value;
            if (!DA.GetData(1, ref maxSpeed)) return;

            if (toolpathStack == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "no ToolpathStack");
                return;
            }

            //Run
            List<List<GH_Plane>> frames = toolpathStack.OrientPlanes;
            List<GH_Plane> flattenFrames = TreeHelper.FlattenList(frames);

            List<List<GH_Number>> speedFactor = toolpathStack.SpeedFactors;
            List<GH_Number> flattenSpeed = TreeHelper.FlattenList(speedFactor);
            for (int i = 0; i < flattenFrames.Count; i++)
            {
                flattenSpeed[i] = new GH_Number(flattenSpeed[i].Value * maxSpeed);
            }

            // Output
            DA.SetDataList(0, flattenFrames);
            DA.SetDataList(1, flattenSpeed);
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
                return Resources.ToGcode;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("BB006AC6-9C60-472A-A7EC-B71CFD04B051"); }
        }
    }
}