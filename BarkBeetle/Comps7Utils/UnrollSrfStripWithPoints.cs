using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using BarkBeetle.Utils;
using System.Linq;

namespace BarkBeetle.Comps7Utils
{
    public class UnrollSrfStripWithPoints : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public UnrollSrfStripWithPoints()
          : base("Unroll Surface Strip with Points", "Unroll Srf Strip with Points",
              "Unroll and label intersecting surface strips",
              "BarkBeetle", "7-Utils")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Surfaces", "S", "Input surfaces", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Curves", "C", "Input curves, should match the input surfaces", GH_ParamAccess.tree);
            pManager.AddPointParameter("Points", "Pts", "Points to move", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Indexes", "i", "indexes of points", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Tolerance", "T", "Intersection tolerance", GH_ParamAccess.item, 0.1);
            pManager.AddNumberParameter("Distance", "D", "Distance between strips", GH_ParamAccess.item);
            pManager.AddNumberParameter("Hole Radius", "HR", "Radius of the holes", GH_ParamAccess.item);
            pManager.AddNumberParameter("Font Size", "FS", "Font size for labels", GH_ParamAccess.item, 1);
            pManager[3].Optional = true;
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
            var surfaces = new GH_Structure<GH_Surface>();
            var curves = new GH_Structure<GH_Curve>();
            var pts = new GH_Structure<GH_Point>();
            var indexes = new GH_Structure<GH_Integer>();
            double tolerance = 0;
            double distance = 0;
            double holeRadius = 0;
            double fontSize = 0;

            List<List<int>> indexList = null;

            if (!DA.GetDataTree(0, out surfaces)) return;
            if (!DA.GetDataTree(1, out curves)) return;
            if (!DA.GetDataTree(2, out pts)) return;
            if (DA.GetDataTree(3, out indexes))
            {
                indexList = TreeHelper.FlattenStructureList(indexes).Select(list => list.Select(num =>num.Value).ToList()).ToList();
            }
            if (!DA.GetData(4, ref tolerance)) return;
            if (!DA.GetData(5, ref distance)) return;
            if (!DA.GetData(6, ref holeRadius)) return;
            if (!DA.GetData(7, ref fontSize)) return;

            List<Surface> surfaceList = TreeHelper.FlattenStructure(surfaces).Select(s => s.Value.Surfaces[0]).ToList();
            List<Curve> curveList = TreeHelper.FlattenStructure(curves).Select(s => s.Value).ToList();
            List<List<Point3d>> ptList = TreeHelper.FlattenStructureList(pts).Select(list => list.Select(pt => pt.Value).ToList()).ToList();

            // Validate inputs
            if (surfaceList.Count != curveList.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Curve and surface lists must have the same length.");
                return;
            }

            // Run
            Unroll.UnrollSurfacesAndLabelingWithPoints(
            curveList, surfaceList, tolerance, ptList, distance, holeRadius, fontSize, out List<GH_Curve> stripBoundaries,
            out List<GH_Point> points, out List<GH_Circle> holes,
            out List<GH_Curve> indicesTextOnCurve, out List<GH_Curve> indicesTextOnPlane, indexList);

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
                return Resources.UnrollWithPts;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("BA46F98E-E656-44D6-98DC-8196C38347BA"); }
        }
    }
}