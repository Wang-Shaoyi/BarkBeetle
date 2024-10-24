using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using BarkBeetle.GeometriesPackage;
using BarkBeetle.Utils;
using Grasshopper.Kernel.Data;
using BarkBeetle.ToolpathBaseSetting;
using BarkBeetle.ToolpathStackSetting;

namespace BarkBeetle.CompsToolpath
{
    public class UnpackToolpathStack : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Extract_RefinedGeometry class.
        /// </summary>
        public UnpackToolpathStack()
          : base("Unpack Toolpath Stack", "Unpack Stack",
              "Unpack all geometries in the Toolpath Stack",
              "BarkBeetle", "Toolpath")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpath Stack", "TS", "BarkBeetle ToolpathStack object", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpath Base", "TB", "BarkBeetle ToolpathBase object", GH_ParamAccess.item);
            pManager.AddCurveParameter("Toolpath Curve", "C", "Continuous toolpath curve", GH_ParamAccess.item);
            pManager.AddCurveParameter("Toolpath Curves", "Crvs", "Toolpath curve from layers", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Toolpath Frames", "TS", "Toolpath frames", GH_ParamAccess.tree);
            pManager.AddSurfaceParameter("Surface Series", "S", "Each layer has one reference surface", GH_ParamAccess.list);
            pManager.AddNumberParameter("Speed Factors", "Speed", "Speed factors for each toolpath frame, 0.5 = median, 1 = max, 0 = min", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Initialize
            ToolpathStackGoo goo = null;

            //Set inputs
            if (!DA.GetData(0, ref goo)) return;
            ToolpathStack toolpathStack = goo.Value;


            if (toolpathStack == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "no ToolpathStack");
                return;
            }

            //Run

            ToolpathBase toolpathBase = toolpathStack._ToolpathBase;
            ToolpathBaseGoo tbGoo = new ToolpathBaseGoo(toolpathBase);

            GH_Curve gH_Curve = toolpathStack.FinalCurve;

            List<GH_Curve> gH_Curves = toolpathStack.LayerCurves;

            List<List<GH_Plane>> frames = toolpathStack.OrientPlanes;
            GH_Structure<GH_Plane> frameTree = TreeHelper.ConvertToGHStructure(frames);

            List<GH_Surface> gH_Surfaces = toolpathStack.Surfaces;

            List<List<GH_Number>> speedFactor = toolpathStack.SpeedFactors;
            GH_Structure<GH_Number> speedFactorTree = TreeHelper.ConvertToGHStructure(speedFactor);

            // Output
            DA.SetData(0, tbGoo);
            DA.SetData(1, gH_Curve);
            DA.SetDataList(2, gH_Curves);
            DA.SetDataTree(3, frameTree);
            DA.SetDataList(4, gH_Surfaces);
            DA.SetDataTree(5, speedFactorTree);
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
                return Resources.UnpackToolpathStack;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("150B92CA-57D8-4283-ABED-9FBC492F7C47"); }
        }
    }
}