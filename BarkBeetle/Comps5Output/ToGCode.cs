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
              "Transfrom Toolpath Stack to GCode",
              "BarkBeetle", "5-Output")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpath Stack", "Stack", "BarkBeetle Toolpath Stack object", GH_ParamAccess.item);
            pManager.AddNumberParameter("Min Speed", "Min", "Maximum Speed", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Max Speed", "Max", "Maximum Speed", GH_ParamAccess.item, 1);
            pManager.AddIntegerParameter("Rounding", "Rounding", "Speed Rounding", GH_ParamAccess.item, 2);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Gcode", "Gcode", "Generated gcode", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Toolpath Planes", "Planes", "Toolpath planes", GH_ParamAccess.list);
            pManager.AddNumberParameter("Robot Speed", "Speed", "Robot Speed", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
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

            List<string> gcode = GcodeRelated.ConvertPlanesToGCodeWithSpeed(flattenFrames, actualSpeeds);

            // Output
            DA.SetDataList(0, gcode);
            DA.SetDataList(1, flattenFrames);
            DA.SetDataList(2, actualSpeeds);
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