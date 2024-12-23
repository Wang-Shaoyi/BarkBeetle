using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using BarkBeetle.Utils;
using BarkBeetle.Skeletons;

namespace BarkBeetle.CompsUtils
{
    public class OrganizePtTreeFromSrf : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the OrganizePtTreeFromSrf class.
        /// </summary>
        public OrganizePtTreeFromSrf()
          : base("Organize point tree from surface", "Organize Point Tree",
              "Organize the sequence of a point tree according to the uv sequence of a surface",
              "BarkBeetle", "7-Utils")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Surface", "Surface", "Base surface to organize the skeleton", GH_ParamAccess.item);
            pManager.AddPointParameter("Point Tree", "Point Tree", "Input a point tree (m by n)", GH_ParamAccess.tree);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Organized Point Tree", "Organized Point Tree", "Re-sorted the sequence of points", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Initialize
            Surface surface = null;
            GH_Structure<GH_Point> pointsTree = new GH_Structure<GH_Point>();

            //Set inputs
            if (!DA.GetData(0, ref surface)) return;
            if (!DA.GetDataTree(1, out pointsTree)) return;

            // Error message.
            if (surface == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No surface");
                return;
            }
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

            GH_Structure<GH_Point> organizedPT = PointDataUtils.OrganizePtSequence(surface, pointsTree,this);

            DA.SetDataTree(0, organizedPT);
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
                return Resources.OrganizePointTreeformSurface;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("F40328ED-93C1-4BF4-A0EA-8C5FF0EAD2E6"); }
        }
    }
}