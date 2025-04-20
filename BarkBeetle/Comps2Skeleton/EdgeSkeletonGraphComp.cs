using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using BarkBeetle.Utils;
using Rhino.Display;
using System.ComponentModel;
using System.Collections;

using BarkBeetle.Network;
using BarkBeetle.Skeletons;
using System.Runtime.InteropServices;

namespace BarkBeetle.CompsGeoPack
{
    public class EdgeSkeletonGraphComp : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the SkeletonFromSAndPT class.
        /// </summary>
        public EdgeSkeletonGraphComp()
          : base("Edge Skeleton Graph", "Edge Skeleton",
              "Edge points re-sorted by surrounding sequence without intersection.",
              "BarkBeetle", "2-Skeleton")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("UVNetwork", "Network", "BarkBeetle UVNetwork object", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Edge Option", "Edge Option", "0: all edges, 1: edge 1, 2: edge 2, 3: edge 3, 4: edge 4", GH_ParamAccess.item, 0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Skeleton Graph", "Skeleton", "BarkBeetle SkeletonGraph object", GH_ParamAccess.item);
            pManager.AddPointParameter("Skeleton Points", "Points", "Re-sorted the sequence of points", GH_ParamAccess.list);
            pManager.AddCurveParameter("Primary Curve", "Curve", "Skeleton main curve", GH_ParamAccess.item);
            pManager.AddVectorParameter("Vectors", "Vectors", "Vectors for skeleton points. Each point has two perpendicular vectors", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Initialize
            UVNetworkGoo goo = null;
            int edgeOption = 0;

            //Set inputs
            if (!DA.GetData(0, ref goo)) return;
            if (!DA.GetData(1, ref edgeOption)) return;
            UVNetwork network = goo.Value;

            // Run Function
            SkeletonGraphEdge spiral = new SkeletonGraphEdge(network, edgeOption);
            SkeletonGraphGoo skeletonGoo = new SkeletonGraphGoo(spiral);

            List<GH_Point> points = spiral.SkeletonPtList;
            GH_Curve curve = spiral.SkeletonMainCurve;
            List<GH_Vector> vectors = spiral.SkeletonVectors;

            // Finally assign the spiral to the output parameter.
            DA.SetData(0, skeletonGoo);
            DA.SetDataList(1, points);
            DA.SetData(2, curve);
            DA.SetDataList(3, vectors);

            var param = Params.Output[3] as IGH_PreviewObject;
            if (param != null)
            {
                param.Hidden = true;
            }
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
                return Resources.edgeSkeleton;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("B3772607-4375-401F-AFB5-9993013CA89B"); }
        }
    }
}