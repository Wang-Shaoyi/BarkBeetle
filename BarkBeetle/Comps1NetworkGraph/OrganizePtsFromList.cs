using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace BarkBeetle.Comps1NetworkGraph
{
    public class OrganizePtsFromList : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the OrganizePtsFromList class.
        /// </summary>
        public OrganizePtsFromList()
          : base("Organize PtTree From List", "PTG",
              "Generates a point tree structure from a point list and a sequence list.",
              "BarkBeetle", "1-Network")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "Pts", "The list of points.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Sequence", "Seq", "The sequence to reorder points.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("PointsPerBranch", "N", "Number of points per branch.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("PointTree", "Tree", "The resulting GH_Structure<GH_Point>.", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Input variables
            List<Point3d> points = new List<Point3d>();
            List<int> sequence = new List<int>();
            int n = 0;

            // Get inputs
            if (!DA.GetDataList(0, points)) return;
            if (!DA.GetDataList(1, sequence)) return;
            if (!DA.GetData(2, ref n)) return;

            // Validate inputs
            if (points.Count != sequence.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Points and sequence lists must have the same length.");
                return;
            }
            if (n <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Points per branch must be greater than 0.");
                return;
            }

            // Step 1: Reorder points based on the sequence list
            Point3d[] reorderedPointsArray = new Point3d[points.Count];
            for (int i = 0; i < sequence.Count; i++)
            {
                int index = sequence[i];
                if (index < 0 || index >= points.Count)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Sequence index out of range.");
                    return;
                }
                reorderedPointsArray[index] = points[i];
            }
            List<Point3d> orderedPoints = new List<Point3d>();
            orderedPoints.AddRange(reorderedPointsArray);

            // Step 2: Create the GH_Structure<GH_Point>
            GH_Structure<GH_Point> pointTree = new GH_Structure<GH_Point>();
            int branchIndex = 0;

            for (int i = 0; i < orderedPoints.Count; i += n)
            {
                // Create a new branch
                GH_Path path = new GH_Path(branchIndex++);
                List<GH_Point> branchPoints = new List<GH_Point>();

                // Add points to the branch
                for (int j = i; j < i + n && j < orderedPoints.Count; j++)
                {
                    branchPoints.Add(new GH_Point(orderedPoints[j]));
                }

                // Add the branch to the tree
                pointTree.AppendRange(branchPoints, path);
            }

            // Output the tree
            DA.SetDataTree(0, pointTree);
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
                return Resources.OrganizaPtsfromList;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("6C1E9864-AA0F-487B-9D7D-6697DA75BD9C"); }
        }
    }
}