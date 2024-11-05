using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using BarkBeetle.Utils;

namespace BarkBeetle.CompsUtils
{
    public class CurveIntersection : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CurveIntersection class.
        /// </summary>
        public CurveIntersection()
          : base("Curve Intersection", "Curve Intersect",
              "Curve Intersection with Tolerance",
              "BarkBeetle", "7-Utils")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves A", "A", "First set of curves", GH_ParamAccess.list);
            pManager.AddCurveParameter("Curves B", "B", "Second set of curves", GH_ParamAccess.list);
            pManager.AddNumberParameter("Tolerance", "T", "Intersection tolerance", GH_ParamAccess.item, 0.01);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Intersection Points", "P", "Intersection points between the curves", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Declare variables for input
            List<Curve> curvesA = new List<Curve>();
            List<Curve> curvesB = new List<Curve>();
            double tolerance = 0.01;

            // Retrieve input data
            if (!DA.GetDataList(0, curvesA)) return;
            if (!DA.GetDataList(1, curvesB)) return;
            if (!DA.GetData(2, ref tolerance)) return;

            // Call the static method to find intersection points
            List<GH_Point> intersectionPoints = CurveUtils.CurveIntersect(curvesA, curvesB, tolerance);

            // Output the result
            DA.SetDataList(0, intersectionPoints);
        }

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
            get { return new Guid("7B417943-821A-40B3-9948-FFDD9DFE1526"); }
        }
    }
}