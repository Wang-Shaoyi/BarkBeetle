using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using BarkBeetle.Utils;

namespace BarkBeetle.Comps7Utils
{
    public class UnrollStraightStripConsistentWidth : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public UnrollStraightStripConsistentWidth()
          : base("Unroll Straight Strip (consistent width)", "Unroll Straight Strip",
              "Unroll and label straight (geodesic) strips",
              "BarkBeetle", "1-Network")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "C", "Input curves", GH_ParamAccess.list);
            pManager.AddNumberParameter("Tolerance", "T", "Intersection tolerance", GH_ParamAccess.item, 0.1);
            pManager.AddNumberParameter("Width", "W", "Strip width", GH_ParamAccess.item);
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
            var curves = new List<Curve>();
            double tolerance = 0;
            double width = 0;
            double distance = 0;
            double holeRadius = 0;
            double fontSize = 0;

            if (!DA.GetDataList(0, curves)) return;
            if (!DA.GetData(1, ref tolerance)) return;
            if (!DA.GetData(2, ref width)) return;
            if (!DA.GetData(3, ref distance)) return;
            if (!DA.GetData(4, ref holeRadius)) return;
            if (!DA.GetData(5, ref fontSize)) return;

            // Initialize
            var rectangles = new List<GH_Curve>();
            var points = new List<GH_Point>();
            var holes = new List<GH_Circle>();
            var indicesTextOnCurve = new List<GH_Curve>();
            var indicesTextOnPlane = new List<GH_Curve>();

            Unroll.CreateRectangles1(curves, width, distance, fontSize, rectangles, indicesTextOnCurve, indicesTextOnPlane);
            Unroll.ProcessIntersections1(
                curves,
                tolerance,
                width,
                distance,
                holeRadius,
                fontSize,
                points,
                holes,
                indicesTextOnCurve,
                indicesTextOnPlane);

            // Output
            DA.SetDataList(0, rectangles);
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
                return Resources.UnrollStrip;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("94ABD720-18C1-4E9B-84EB-3AD70ED618A0"); }
        }
    }
}