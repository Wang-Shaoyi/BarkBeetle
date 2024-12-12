using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using BarkBeetle.Pattern;
using BarkBeetle.Skeletons;
using Grasshopper.Kernel.Types;

namespace BarkBeetle.Comps3Pattern
{
    public class PatternAvoidObstacles : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the PatternAvoidObstables class.
        /// </summary>
        public PatternAvoidObstacles()
          : base("Pattern Avoid Obstacles", " Avoid Obstacles",
              "Can avoid obstacles such as shear blocks. Only works for snake infill pattern and spiral infill pattern",
              "BarkBeetle", "3-Pattern")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpath Pattern", "TP", "BarkBeetle Toolpath Pattern object, only works for snake infill pattern and spiral infill pattern.", GH_ParamAccess.item);
            pManager.AddPointParameter("Location Points", "Pt", "Locations of the obstacles", GH_ParamAccess.list);
            pManager.AddCurveParameter("Obstacle Curve", "Obs", "Obstacle Curve",GH_ParamAccess.item);
            pManager.AddPlaneParameter("Reference Plane", "Pl", "Plane of the obstable curve", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Output Toolpath Pattern", "Output", "BarkBeetle Toolpath Pattern object.", GH_ParamAccess.item);
            pManager.AddCurveParameter("Curve", "C", "Toolpath curve for a layer", GH_ParamAccess.item);
            pManager.AddCurveParameter("Block Curves", "BC", "Boundaries of blocking obstacles", GH_ParamAccess.list);
            pManager.AddCurveParameter("Trim Curves", "TC", "Curves trimming the toolpath", GH_ParamAccess.list);
            pManager.AddPointParameter("Intersection Pts", "Pts", "Intersection points", GH_ParamAccess.list);

            if (pManager[3] is IGH_PreviewObject trimCurvesParam)
            {
                trimCurvesParam.Hidden = true;
            }
            if (pManager[4] is IGH_PreviewObject intersectionPtsParam)
            {
                intersectionPtsParam.Hidden = true;
            }
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Initialize
            ToolpathPatternGoo goo = null;
            List<Point3d> points = new List<Point3d>();
            Curve block = null;
            Plane blockPlane = Plane.WorldXY;

            //Set inputs
            if (!DA.GetData(0, ref goo)) return;
            ToolpathPattern toolpathPattern = goo.Value;
            if (!DA.GetDataList(1, points)) return;
            if (!DA.GetData(2, ref block)) return;
            if (!DA.GetData(3, ref blockPlane)) return;

            AvoidObstacles avoidObstacles = new AvoidObstacles(toolpathPattern, points, block, blockPlane, out toolpathPattern);

            ToolpathPatternGoo patternGoo = new ToolpathPatternGoo(toolpathPattern);

            GH_Curve crv = new GH_Curve(toolpathPattern.CoutinuousCurve);

            List<Curve> blocks = avoidObstacles.BlockBoundaries;
            List<Curve> trims = avoidObstacles.TrimCurves;
            List<Point3d> pts = avoidObstacles.IntersectionPts;

            // Finally assign the spiral to the output parameter.
            DA.SetData(0, patternGoo);
            DA.SetData(1, crv);
            DA.SetDataList(2, blocks);
            DA.SetDataList(3, trims);
            DA.SetDataList(4, pts);
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
                return Resources.obstacle;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("9A5C0F0A-EC1F-439F-BC29-A606C13A396F"); }
        }
    }
}