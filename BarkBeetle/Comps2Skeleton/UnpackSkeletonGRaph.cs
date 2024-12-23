using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;


using BarkBeetle.Utils;
using Grasshopper.Kernel.Data;
using BarkBeetle.Pattern;
using BarkBeetle.Skeletons;
using BarkBeetle.Network;

namespace BarkBeetle.Comps2Skeleton
{
    public class UnpackSkeletonGraph : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Extract_RefinedGeometry class.
        /// </summary>
        public UnpackSkeletonGraph()
          : base("Unpack Skeleton Graph", "Unpack Skeleton",
              "Unpack all geometries in the Skeleton Graph",
              "BarkBeetle", "2-Skeleton")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Skeleton Graph", "Skeleton", "BarkBeetle Skeleton Graph object", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("UVNetwork", "Network", "BarkBeetle UVNetwork object", GH_ParamAccess.item);
            pManager.AddPointParameter("Skeleton Points", "Points", "Re-sorted the sequence of points", GH_ParamAccess.list);
            pManager.AddCurveParameter("Main Curve", "Curve", "Skeleton main curve", GH_ParamAccess.item);
            pManager.AddCurveParameter("Branch Curves", "Branch Curves", "Skeleton branch curves", GH_ParamAccess.list);
            pManager.AddVectorParameter("Vectors", "Vectors", "Vectors for skeleton points. Each point has two perpendicular vectors", GH_ParamAccess.list);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Initialize
            SkeletonGraphGoo goo = null;

            //Set inputs
            if (!DA.GetData(0, ref goo)) return;
            SkeletonGraph skeletonGraph = goo.Value;

            //Run
            UVNetworkGoo uvNetworkGoo = new UVNetworkGoo(skeletonGraph.UVNetwork);
            List<GH_Point> points = skeletonGraph.SkeletonPtList;
            GH_Curve curve = skeletonGraph.SkeletonMainCurve;
            List<GH_Curve> curves = skeletonGraph.SkeletonBranchCurves;
            List<GH_Vector> vectors = skeletonGraph.SkeletonVectors;

            // Finally assign the spiral to the output parameter.
            DA.SetData(0, uvNetworkGoo);
            DA.SetDataList(1, points);
            DA.SetData(2, curve);
            DA.SetDataList(3, curves);
            DA.SetDataList(4, vectors);
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
                return Resources.UnpackSkeletonPackage;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("77F1C066-C675-47B6-A44D-DDB6821B908C"); }
        }
    }
}