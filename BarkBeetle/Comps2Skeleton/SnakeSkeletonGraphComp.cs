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

namespace BarkBeetle.CompsGeoPack
{
    public class SnakeSkeletonGraphComp : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the SkeletonFromSAndPT class.
        /// </summary>
        public SnakeSkeletonGraphComp()
          : base("Snake skeleton graph", "Snake skeleton",
              "Skeleton is a data tree re-sorted by a certain sequence",
              "BarkBeetle", "2-Skeleton")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("UVNetwork", "Network", "BarkBeetle UVNetwork object", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Skeleton Graph", "Skeleton", "BarkBeetle SkeletonGraph object", GH_ParamAccess.item);
            pManager.AddPointParameter("Skeleton Points", "Pts", "Re-sorted the sequence of points", GH_ParamAccess.list);
            pManager.AddCurveParameter("Main Curve", "C", "Skeleton main curve", GH_ParamAccess.item);
            pManager.AddCurveParameter("Branch Curves", "BC", "Skeleton branch curve", GH_ParamAccess.list);
            pManager.AddVectorParameter("Vectors", "V", "Vectors", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Initialize
            UVNetworkGoo goo = null;

            //Set inputs
            if (!DA.GetData(0, ref goo)) return;
            UVNetwork network = goo.Value;

            // Run Function
            SkeletonGraphSnake snake = new SkeletonGraphSnake(network);
            SkeletonGraphGoo skeletonGoo = new SkeletonGraphGoo(snake);

            List<GH_Point> points = snake.SkeletonPtList;
            GH_Curve curve = snake.SkeletonMainCurve;
            List<GH_Curve> curves = snake.SkeletonBranchCurves;
            List<GH_Vector> vectors = snake.SkeletonVectors;

            // Finally assign the spiral to the output parameter.
            DA.SetData(0, skeletonGoo);
            DA.SetDataList(1, points);
            DA.SetData(2, curve);
            DA.SetDataList(3, curves);
            DA.SetDataList(4, vectors);

            var param = Params.Output[3] as IGH_PreviewObject;
            if (param != null)
            {
                param.Hidden = true;
            }
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Resources.SpiralSkeleton;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("F0A34392-00CB-477D-998B-C8E49051F3F9"); }
        }
    }
}