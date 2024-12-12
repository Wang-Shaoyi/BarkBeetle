using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using BarkBeetle.Utils;

namespace BarkBeetle.Comps7Utils
{
    public class UnrollSrfWithPoints : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public UnrollSrfWithPoints()
          : base("Unroll Intersect Surface Strip", "Unroll Srf Strip",
              "Unroll and label intersecting surface strips",
              "BarkBeetle", "7-Utils")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Surfaces", "S", "Input surfaces", GH_ParamAccess.list);
            pManager.AddCurveParameter("Curves", "C", "Input curves, should match the input surfaces", GH_ParamAccess.list);
            pManager.AddNumberParameter("Tolerance", "T", "Intersection tolerance", GH_ParamAccess.item, 0.1);
            pManager.AddNumberParameter("Distance", "D", "Distance between strips", GH_ParamAccess.item);
            pManager.AddNumberParameter("Hole Radius", "HR", "Radius of the holes", GH_ParamAccess.item);
            pManager.AddNumberParameter("Font Size", "FS", "Font size for labels", GH_ParamAccess.item, 1);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Strips", "S", "Generated strips", GH_ParamAccess.list);
            pManager.AddPointParameter("Points", "P", "Intersection points", GH_ParamAccess.tree);
            pManager.AddCircleParameter("Holes", "H", "Intersection holes", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Labels", "L", "Labels on strips", GH_ParamAccess.list);
            pManager.AddCurveParameter("Original Labels", "OL", "Labels on the original curves", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Inputs
            var surfaces = new List<Surface>();
            var curves = new List<Curve>();
            double tolerance = 0;
            double distance = 0;
            double holeRadius = 0;
            double fontSize = 0;

            if (!DA.GetDataList(0, surfaces)) return;
            if (!DA.GetDataList(1, curves)) return;
            if (!DA.GetData(2, ref tolerance)) return;
            if (!DA.GetData(3, ref distance)) return;
            if (!DA.GetData(4, ref holeRadius)) return;
            if (!DA.GetData(5, ref fontSize)) return;

            // Validate inputs
            if (surfaces.Count != curves.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Curve and surface lists must have the same length.");
                return;
            }

            // Run
            Unroll.UnrollIntersectSurfacesAndLabeling(
            curves, surfaces, tolerance, distance, holeRadius, fontSize, out List<GH_Curve> stripBoundaries,
            out List<GH_Point> points, out List<GH_Circle> holes,
            out List<GH_Curve> indicesTextOnCurve, out List<GH_Curve> indicesTextOnPlane);

            // Output
            DA.SetDataList(0, stripBoundaries);
            DA.SetDataList(1, points);
            DA.SetDataList(2, holes);
            DA.SetDataList(3, indicesTextOnPlane);
            DA.SetDataList(4, indicesTextOnCurve);
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
                return Resources.unrollIntersectSurface;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("69FDE932-C5A0-4E75-AB75-A52B6B08F502"); }
        }
    }
}