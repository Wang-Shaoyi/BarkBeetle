using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using System.Drawing;

using BarkBeetle.Utils;

namespace BarkBeetle.Comps6Display
{
    public class SurfaceCurvature : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the SurfaceCurvature class.
        /// </summary>
        public SurfaceCurvature()
          : base("Surface Curvature", "Surface Curvature",
              "Display surface curvature",
              "BarkBeetle", "6-Display")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Surfaces", "S", "List of surfaces to analyze curvature", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Density", "D", "Mesh density for curvature analysis", GH_ParamAccess.item, 1);

            pManager.AddIntegerParameter("Type", "T", "Curvature type (0: Mean, 1: Gaussian, 2: Min, 3: Max)", GH_ParamAccess.item, 0);
            var typeParam = pManager[2] as Param_Integer; 
            if (typeParam != null)
            {
                typeParam.AddNamedValue("Mean", 0);
                typeParam.AddNamedValue("Gaussian", 1);
                typeParam.AddNamedValue("Min", 2);
                typeParam.AddNamedValue("Max", 3);
            }

            pManager.AddIntegerParameter("Output Unit", "U", "Unit for output (0: 1/m, 1/cm, 2: 1/mm)", GH_ParamAccess.item, 0);
            var unitParam = pManager[3] as Param_Integer;
            if (unitParam != null)
            {
                unitParam.AddNamedValue("1/meter", 0);
                unitParam.AddNamedValue("1/centimeter", 1);
                unitParam.AddNamedValue("1/millimeter", 2);
            }
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Meshes", "M", "Curvature-colored meshes", GH_ParamAccess.list);
            pManager.AddColourParameter("Legend Colors", "C", "Colors for legend visualization", GH_ParamAccess.list);
            pManager.AddNumberParameter("Legend Tags", "T", "Tags for legend visualization", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Input variables
            List<Surface> surfaces = new List<Surface>();
            int density = 0;
            int type = 0;
            int outputUnit = 0;

            // Get inputs
            if (!DA.GetDataList(0, surfaces)) return;
            if (!DA.GetData(1, ref density)) return;
            if (!DA.GetData(2, ref type)) return;
            if (!DA.GetData(3, ref outputUnit)) return;

            // Output variables
            List<Mesh> meshes;
            List<Color> legendColors;
            List<double> legendTags;

            // Call the curvature display function
            CurvatureDisplay display = new CurvatureDisplay();
            display.DisplayCurvature(surfaces, density, type, outputUnit, out meshes, out legendColors, out legendTags);

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
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("B1047563-8B89-4C60-AFC6-9EED87D28097"); }
        }
    }
}