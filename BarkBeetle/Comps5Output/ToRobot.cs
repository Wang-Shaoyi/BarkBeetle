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
    public class ToRobot : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Extract_RefinedGeometry class.
        /// </summary>
        public ToRobot()
          : base("To Robot Targets", "To Robot Targets",
              "[Note: This component is still under develop, and the result is not accurate.]Transfrom Toolpath Stack to Kuka movement (e.g., LINear Movement)",
              "BarkBeetle", "5-Output")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpath Stack", "TS", "BarkBeetle ToolpathStack object", GH_ParamAccess.item);
            pManager.AddNumberParameter("Min Speed", "Min", "Maximum Speed", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Max Speed", "Max", "Maximum Speed", GH_ParamAccess.item, 1);
            pManager.AddIntegerParameter("Rounding", "Rounding", "Speed Rounding", GH_ParamAccess.item, 2);
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
            double maxSpeed = 1;
            double minSpeed = 0;
            int rounding = 2;

            //Set inputs
            if (!DA.GetData(0, ref goo)) return;
            ToolpathStack toolpathStack = goo.Value;
            if (!DA.GetData(1, ref minSpeed)) return;
            if (!DA.GetData(2, ref maxSpeed)) return;
            if (!DA.GetData(3, ref rounding)) return;

            if (toolpathStack == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "no ToolpathStack");
                return;
            }

            //Run
            List<List<GH_Plane>> frames = toolpathStack.OrientPlanes;
            List<GH_Plane> flattenFrames = TreeHelper.FlattenList(frames);

            List<List<GH_Number>> speedFactor = toolpathStack.SpeedFactors;
            List<double> flattenSpeedFactor = TreeHelper.FlattenList(speedFactor).Select(x => x.Value).ToList();


            double minFactor = flattenSpeedFactor.Min();
            double maxFactor = flattenSpeedFactor.Max();

            List<double> actualSpeeds = flattenSpeedFactor.Select(factor => Math.Round
            (minSpeed + (factor - minFactor) * (maxSpeed - minSpeed) / (maxFactor - minFactor), rounding)).ToList();


            // Output
            DA.SetDataList(0, flattenFrames);
            DA.SetDataList(1, actualSpeeds);
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
                return Resources.ToRobot;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("85E0BEBB-86F1-490E-8A00-788EC00CE6FE"); }
        }
    }
}