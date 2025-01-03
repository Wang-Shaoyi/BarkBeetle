﻿using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using BarkBeetle.Utils;
using BarkBeetle.Network;
using BarkBeetle.Skeletons;

namespace BarkBeetle.Comps1NetworkGraph
{
    public class BrepStrip : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the StripVisualization class.
        /// </summary>
        public BrepStrip()
          : base("Brep Strip", "Brep Strip",
              "Create Brep Strip from Network OR Curves. (One set of input is enough)",
              "BarkBeetle", "1-Network")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("UVNetwork", "Network", "BarkBeetle UVNetwork object", GH_ParamAccess.item);
            pManager.AddNumberParameter("Strip Extension", "Extension", "How long strips extends on both sides", GH_ParamAccess.item);
            pManager.AddCurveParameter("Curves", "Curves", "Strip center curves", GH_ParamAccess.tree);
            pManager.AddSurfaceParameter("Ref Surface", "Surface", "Reference surface to generate strips on", GH_ParamAccess.item);
            pManager.AddNumberParameter("Strip Width", "Width", "Strip width", GH_ParamAccess.item);

            pManager[0].Optional = true;  // UVNetwork
            pManager[1].Optional = false;  // Strip Extension
            pManager[2].Optional = true;  // Curves
            pManager[3].Optional = true;  // Surface
            pManager[4].Optional = true;  // Strip Width
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Strip", "S", "Output strips", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Initialize
            UVNetworkGoo goo = null;
            double extension = 0;
            GH_Structure<GH_Curve> curves = null;
            GH_Surface surface = null;
            double stripWidth = 0;

            GH_Structure<GH_Surface> strips = null;

            if (!DA.GetData(1, ref extension)) return;
            //Set inputs
            if (!DA.GetData(0, ref goo))
            {
                if (!DA.GetDataTree(2, out curves)) return;
                if (!DA.GetData(3, ref surface)) return;
                if (!DA.GetData(4, ref stripWidth)) return;
                strips = BrepUtils.StripFromCurves(curves, surface.Value.Surfaces[0], stripWidth, extension);
                DA.SetDataTree(0, strips);
                return;
            }

            UVNetwork network = goo.Value;
            GH_Structure<GH_Curve> uvCurvesTree = TreeHelper.ConvertToGHStructure(network.UVCurves);
            strips = BrepUtils.StripFromCurves(uvCurvesTree, network.ExtendedSurface, network.StripWidth, extension);

            DA.SetDataTree(0, strips);
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
                return Resources.VisualizeStrip;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("D3F95A8E-C229-496C-AA0F-DBCA8E5BB68D"); }
        }
    }
}