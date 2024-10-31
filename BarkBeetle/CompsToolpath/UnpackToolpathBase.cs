using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using BarkBeetle.GeometriesPackage;
using BarkBeetle.Utils;
using Grasshopper.Kernel.Data;
using BarkBeetle.ToolpathBaseSetting;

namespace BarkBeetle.CompsToolpath
{
    public class UnpackToolpathBase : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Extract_RefinedGeometry class.
        /// </summary>
        public UnpackToolpathBase()
          : base("Unpack Toolpath Base", "Unpack Base",
              "Unpack all geometries in the Toolpath Base",
              "BarkBeetle", "Toolpath")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpath Base", "TB", "BarkBeetle ToolpathBase object", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Skeleton Package", "SP", "BarkBeetle SkeletonPackage object", GH_ParamAccess.item);
            pManager.AddCurveParameter("Toolpath Base Curve", "C", "Toolpath curve for the first layer", GH_ParamAccess.item);
            pManager.AddCurveParameter("Unconnected Curves", "Crvs", "Toolpath curves before connected", GH_ParamAccess.list);
            pManager.AddPointParameter("Toolpath Points", "P", "Toolpath corner points", GH_ParamAccess.tree);

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
            ToolpathPattern toolpathBase = goo.Value;

            //Run

            GH_Curve crv = new GH_Curve(toolpathBase.Curve);

            SkeletonPackage skeletonPackage = toolpathBase.SkeletonPackage;
            SkeletonPackageGoo spGoo = new SkeletonPackageGoo(skeletonPackage);

            List<Curve> curves = toolpathBase.Curves;
            GH_Structure<GH_Point> corners = TreeHelper.ConvertToGHStructure(toolpathBase.CornerPts);

            List<GH_Curve> ghCurves = new List<GH_Curve>();

            foreach (Curve curve in curves)
            {
                GH_Curve ghCurve = new GH_Curve(curve); 
                ghCurves.Add(ghCurve);                   
            }

            // Output
            DA.SetData(0, spGoo);
            DA.SetData(1, crv);
            DA.SetDataList(2, ghCurves);
            DA.SetDataTree(3, corners);
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