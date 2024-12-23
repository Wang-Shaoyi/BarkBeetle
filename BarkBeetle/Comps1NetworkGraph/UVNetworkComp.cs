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
    public class UVNetworkComp : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the SkeletonFromSAndPT class.
        /// </summary>
        public UVNetworkComp()
          : base("BarkBeetle Network", "Network",
              "Create BarkBeetle Network",
              "BarkBeetle", "1-Network")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points Tree", "Points Tree", "Input a point tree (m by n)", GH_ParamAccess.tree);
            pManager.AddSurfaceParameter("Base Surface", "Base Surface", "Optional, base surface to organize the skeleton", GH_ParamAccess.item);
            pManager.AddMeshParameter("Mesh", "Mesh", "Optional, base mesh to organize the skeleton", GH_ParamAccess.item);
            pManager.AddNumberParameter("Strip width", "Width", "Input the strip width", GH_ParamAccess.item, 1);
            pManager.AddIntegerParameter("Reference Option", "Option", "Which position is the network refering to. 0-points, 1-surface, 2-mesh", GH_ParamAccess.item, 0);

            Params.Input[1].Optional = true;
            Params.Input[2].Optional = true;
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
            GH_Structure<GH_Point> pointsTree = new GH_Structure<GH_Point>();
            double stripWidth = 0;
            
            int optionInt = 0;

            //Set inputs
            if (!DA.GetDataTree(0, out pointsTree)) return;
            DA.GetData(1, ref surface);
            DA.GetData(2, ref mesh);
            if (!DA.GetData(3, ref stripWidth)) return;
            if (!DA.GetData(4, ref optionInt)) return;


            #region Error message.
            if (pointsTree == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No points");
                return;
            }
            if (pointsTree.PathCount == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The point tree has no branches.");
                return;
            }
            if (!TreeHelper.CheckTreeFormat2D(pointsTree))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid tree format: The tree is not in a proper 2D format.");
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
            UVNetworkFromPointTree network = new UVNetworkFromPointTree(surface, mesh, pointsTree, stripWidth, option);

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
                return Resources.UVNetworkOnSrf;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("5E1F1B09-1A9F-4D21-802D-289F3E9E2E94"); }
        }
    }
}