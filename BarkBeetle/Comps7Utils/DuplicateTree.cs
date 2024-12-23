using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using BarkBeetle.Utils;

namespace BarkBeetle.Comps7Utils
{
    public class DuplicateTree : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the DuplicateTree class.
        /// </summary>
        public DuplicateTree()
          : base("Duplicate Tree", "Duplicate Tree",
              "Copy a tree multiple times",
              "BarkBeetle", "7-Utils")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Input Tree", "Tree", "Input tree to duplicate", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Count", "Count", "Number of times to duplicate the tree", GH_ParamAccess.item);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Duplicated Tree", "Duplicated Tree", "Duplicated tree with two-layer structure", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<IGH_Goo> inputTree = null;
            int count = 0;

            if (!DA.GetDataTree(0, out inputTree)) return;
            if (!DA.GetData(1, ref count)) return;

            if (count < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Count must be greater than 0.");
                return;
            }

            GH_Structure<IGH_Goo> duplicatedTree = TreeHelper.DuplicateTree(inputTree, count);

            DA.SetDataTree(0, duplicatedTree);
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
                return Resources.copyTree;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("ACF2B48A-0CF1-4495-BA1D-296D0EA8B085"); }
        }
    }
}