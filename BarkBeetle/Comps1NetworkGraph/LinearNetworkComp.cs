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
using static BarkBeetle.Network.UVNetwork;

namespace BarkBeetle.CompsGeoPack
{
    public class LinearNetworkComp : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the SkeletonFromSAndPT class.
        /// </summary>
        public LinearNetworkComp()
          : base("BarkBeetle Linear Network", "Linear Network",
              "Create BarkBeetle Linear Network",
              "BarkBeetle", "1-Network")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Main Points", "Main Points", "The points on the main curve", GH_ParamAccess.list);
            pManager.AddPointParameter("Branch Points", "Branch Points", "Points of the branch curves", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Branch Points Index", "BranchID", "Index of the main point related to the branches", GH_ParamAccess.list);
            pManager.AddSurfaceParameter("Base Surface", "Base Surface", "Required, base surface to organize the skeleton", GH_ParamAccess.item);
            pManager.AddMeshParameter("Mesh", "Mesh", "Optional, base mesh to organize the skeleton", GH_ParamAccess.item);
            pManager.AddNumberParameter("Strip width", "Width", "Input the strip width", GH_ParamAccess.item, 1);
            pManager.AddIntegerParameter("Reference Option", "Option", "Which position is the network refering to. 0-points, 1-surface, 2-mesh", GH_ParamAccess.item, 0);

            Params.Input[1].Optional = true;
            Params.Input[2].Optional = true;
            Params.Input[3].Optional = false;
            Params.Input[4].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("UVNetwork", "Network", "BarkBeetle UVNetwork object", GH_ParamAccess.item);
            pManager.AddPointParameter("Points", "Points", "Points pulled on the surface", GH_ParamAccess.tree);
            pManager.AddCurveParameter("UVCurves", "UVCurves", "UV curves generated from the point tree", GH_ParamAccess.tree);
            pManager.AddSurfaceParameter("Extended Surface", "Surface", "Extended surface for toolpath generation", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Initialize
            Surface surface = null;
            Mesh mesh = null;
            List<GH_Point> mainPts = new List<GH_Point>();
            List<GH_Point> subPts = new List<GH_Point>();
            List<int> subID = new List<int>();
            double stripWidth = 0;
            
            int optionInt = 0;

            //Set inputs
            if (!DA.GetDataList(0, mainPts)) return;
            DA.GetDataList(1, subPts);
            DA.GetDataList(2, subID);
            DA.GetData(3, ref surface);
            DA.GetData(4, ref mesh);
            if (!DA.GetData(5, ref stripWidth)) return;
            if (!DA.GetData(6, ref optionInt)) return;


            #region Error message.
            if (mainPts == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No points");
                return;
            }
            if (stripWidth <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "strip width must be larger than 0");
                return;
            }
            #endregion

            // Run Function
            //// Get reference option

            NetworkReferenceOption option = ConvertToReferenceOption(optionInt);
            LinearNetwork network = new LinearNetwork(surface, mesh, mainPts, subPts, subID, stripWidth, option);

            UVNetworkGoo networkGoo = new UVNetworkGoo(network);

            GH_Structure<GH_Point> organizedPtsTree = network.OrganizedPtsTree;
            List<List<GH_Curve>> uvCurves = network.UVCurves;
            GH_Structure<GH_Curve> uvCurvesTree = TreeHelper.ConvertToGHStructure(uvCurves);
            GH_Surface gH_Surface = new GH_Surface(network.ExtendedSurface);


            // Finally assign the spiral to the output parameter.
            DA.SetData(0, networkGoo);
            DA.SetDataTree(1, organizedPtsTree);
            DA.SetDataTree(2, uvCurvesTree);
            DA.SetData(3, gH_Surface);

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
                return Resources.LinearNetwork;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("36EF8E9D-D391-42E7-83AD-ADB0CE275E5A"); }
        }
    }
}