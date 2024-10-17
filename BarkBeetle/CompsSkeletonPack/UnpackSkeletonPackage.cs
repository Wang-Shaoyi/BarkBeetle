using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using BarkBeetle.GeometriesPackage;
using BarkBeetle.Utils;
using Grasshopper.Kernel.Data;
using BarkBeetle.ToolpathBaseSetting;

namespace BarkBeetle.CompsGeoPack
{
    public class UnpackSkeletonPackage : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Extract_RefinedGeometry class.
        /// </summary>
        public UnpackSkeletonPackage()
          : base("Unpack Skeleton Package", "Unpack Skeleton",
              "Unpack all geometries in the Skeleton Package",
              "BarkBeetle", "Skeleton")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Skeleton Package", "SP", "BarkBeetle SkeletonPackage Object", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Organized point tree", "PT", "Organized point tree", GH_ParamAccess.tree);
            pManager.AddSurfaceParameter("Extended surface", "S", "Extended surface", GH_ParamAccess.item);
            pManager.AddPointParameter("Skeleton points", "P", "Skeleton points", GH_ParamAccess.list);
            pManager.AddCurveParameter("Skeleton curve", "C", "Skeleton curves", GH_ParamAccess.item);
            pManager.AddCurveParameter("UV curves", "uv C", "uv iso curves", GH_ParamAccess.tree);
            pManager.AddVectorParameter("UV vectors", "uv", "Points uv vectors", GH_ParamAccess.tree);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Initialize
            SkeletonPackageGoo goo = null;

            //Set inputs
            if (!DA.GetData(0, ref goo)) return;
            SkeletonPackage geoPack = goo.Value;

            //Run
            GH_Structure<GH_Point> organizedPtTree = geoPack.OrganizedPtsTree;
            GH_Surface extendedSrf = new GH_Surface(geoPack.ExtendedSurface);
            List<GH_Point> skeletonPts = geoPack.Skeleton.SkeletonPoints;
            GH_Curve skeletonCrv = geoPack.Skeleton.SkeletonCurve;
            GH_Structure<GH_Curve> uvCurves = TreeHelper.ConvertToGHStructure(geoPack.UVCurves);
            GH_Structure<GH_Vector> uvVectors = TreeHelper.Convert3DArrayToGHStructure(geoPack.Skeleton.UVVectors);

            // Output
            DA.SetDataTree(0, organizedPtTree);
            DA.SetData(1, extendedSrf);
            DA.SetDataList(2, skeletonPts);
            DA.SetData(3, skeletonCrv);
            DA.SetDataTree(4, uvCurves);
            DA.SetDataTree(5, uvVectors);
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
                return Resources.UnpackSkeletonPackage;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("036C8B79-A6E2-41C2-925C-E56FE1A268DE"); }
        }
    }
}