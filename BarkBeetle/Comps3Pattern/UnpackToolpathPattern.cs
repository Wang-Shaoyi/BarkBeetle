using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;


using BarkBeetle.Utils;
using Grasshopper.Kernel.Data;
using BarkBeetle.Pattern;
using BarkBeetle.Skeletons;

namespace BarkBeetle.CompsToolpath
{
    public class UnpackToolpathPattern : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Extract_RefinedGeometry class.
        /// </summary>
        public UnpackToolpathPattern()
          : base("Unpack Toolpath Pattern", "Unpack Pattern",
              "Unpack all geometries in the Toolpath Pattern",
              "BarkBeetle", "3-Pattern")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpath Pattern", "Pattern", "BarkBeetle Toolpath Pattern object", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Skeleton Graph", "Skeleton", "BarkBeetle Skeleton Graph object", GH_ParamAccess.item);
            pManager.AddCurveParameter("Curve", "Curve", "Toolpath curve for a layer", GH_ParamAccess.item);
            pManager.AddCurveParameter("Bundle Curves", "Bundle Curves", "Toolpath curves before connected", GH_ParamAccess.list);
            pManager.AddPointParameter("Points", "Points", "Toolpath corner points", GH_ParamAccess.list);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Initialize
            ToolpathPatternGoo goo = null;

            //Set inputs
            if (!DA.GetData(0, ref goo)) return;
            ToolpathPattern toolpathPattern = goo.Value;

            //Run

            GH_Curve crv = new GH_Curve(toolpathPattern.CoutinuousCurve);

            SkeletonGraph skeletonG = toolpathPattern.Skeleton;
            SkeletonGraphGoo sgGoo = new SkeletonGraphGoo(skeletonG);

            List<Curve> curves = toolpathPattern.BundleCurves;
            List<Point3d> corners = toolpathPattern.CornerPtsList;

            List<GH_Curve> ghCurves = new List<GH_Curve>();

            foreach (Curve curve in curves)
            {
                GH_Curve ghCurve = new GH_Curve(curve); 
                ghCurves.Add(ghCurve);                   
            }

            // Output
            DA.SetData(0, sgGoo);
            DA.SetData(1, crv);
            DA.SetDataList(2, ghCurves);
            DA.SetDataList(3, corners);
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
                return Resources.UnpackToolpathBase;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("704A699D-3722-4607-9BED-E05F8F1EAA64"); }
        }
    }
}