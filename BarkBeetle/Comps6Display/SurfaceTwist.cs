using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using System.Drawing;

using BarkBeetle.Utils;

namespace BarkBeetle.Comps6Display
{
    public class SurfaceTwist : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the SurfaceCurvature class.
        /// </summary>
        public SurfaceTwist()
          : base("Surface Twist Angle per Length", "Surface Twist",
              "Display surface (stirp) twist angle per length",
              "BarkBeetle", "6-Display & Analysis")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Surfaces", "Surfaces", "List of surfaces to analyze twisting", GH_ParamAccess.list);
            pManager.AddCurveParameter("Center Curves", "Curves", "Center curves of the surfaces. Must match surfaces to analyze", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Density", "Density", "Mesh density for curvature analysis", GH_ParamAccess.item, 1);

            pManager.AddIntegerParameter("Output Unit", "Unit", "Unit for output (0: rad/m, 1: rad/cm, 2: rad/mm)", GH_ParamAccess.item, 0);
            var unitParam = pManager[3] as Param_Integer;
            if (unitParam != null)
            {
                unitParam.AddNamedValue("rad/meter", 0);
                unitParam.AddNamedValue("rad/centimeter", 1);
                unitParam.AddNamedValue("rad/millimeter", 2);
            }
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Meshes", "Meshes", "Curvature-colored meshes", GH_ParamAccess.list);
            pManager.AddColourParameter("Legend Colors", "Colors", "Colors for legend visualization", GH_ParamAccess.list);
            pManager.AddNumberParameter("Legend Tags", "Tags", "Tags for legend visualization", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Input variables
            List<Surface> surfaces = new List<Surface>();
            List<Curve> curves = new List<Curve>();
            int density = 0;
            int outputUnit = 0;

            // Get inputs
            if (!DA.GetDataList(0, surfaces)) return;
            if (!DA.GetDataList(1, curves)) return;
            if (!DA.GetData(2, ref density)) return;
            if (!DA.GetData(3, ref outputUnit)) return;

            // Output variables
            List<Mesh> meshes;
            List<Color> legendColors;
            List<double> legendTags;

            // Call the curvature display function
            EvaluationDisplay display = new EvaluationDisplay();
            display.DisplayTwist(surfaces, curves, density, outputUnit, out meshes, out legendColors, out legendTags);

            // Set outputs
            DA.SetDataList(0, meshes);
            DA.SetDataList(1, legendColors);
            DA.SetDataList(2, legendTags);
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
                return Resources.twisting;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("40C0027E-FE00-41BB-9B16-6F49A6D6B735"); }
        }
    }
}