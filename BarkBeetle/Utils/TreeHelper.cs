using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Rhino.Runtime.ViewCaptureWriter;

namespace BarkBeetle.Utils
{
    internal class TreeHelper
    {
        //Check if a tree is 2D format
        public static bool CheckTreeFormat2D<T>(GH_Structure<T> tree) where T : IGH_Goo
        {
            // 1. Simplify the pointTree to remove unnecessary path indices
            tree.Simplify(GH_SimplificationMode.CollapseLeadingOverlaps);

            // 2. Check if there is only one level of paths (single layer path)
            foreach (GH_Path path in tree.Paths)
            {
                if (path.Length != 1)
                {
                    return false;
                }
            }

            // 3. If it's a single-layer path, check if all branches have the same number of points
            int referenceCount = tree.get_Branch(0).Count;
            for (int i = 1; i < tree.Branches.Count; i++)
            {
                int currentCount = tree.get_Branch(i).Count;
                if (currentCount != referenceCount) return false;
            }

            return true;
        }

        //Get tree size
        public static List<int> GetTreeLayerLengths<T>(GH_Structure<T> tree, GH_Component component) where T : IGH_Goo
        {
            tree.Simplify(GH_SimplificationMode.CollapseLeadingOverlaps);
            if (!CheckTreeFormat2D(tree))
            {
                return null;
            }
            List<int> layerLengths = new List<int>();

            foreach (GH_Path path in tree.Paths)
            {
                int branchLength = tree.get_Branch(path).Count;
                layerLengths.Add(branchLength);
            }

            int row = layerLengths[0];
            int col = layerLengths.Count;

            return new List<int> { row, col };
        }

        // Flip tree
        public static GH_Structure<T> FlipMatrix<T>(GH_Structure<T> tree, GH_Component component) where T : IGH_Goo
        {
            tree.Simplify(GH_SimplificationMode.CollapseLeadingOverlaps);

            if (!CheckTreeFormat2D(tree))
            {
                component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid tree format: The tree is not in a proper 2D format.");
                return null;
            }

            GH_Structure<T> flippedTree = new GH_Structure<T>();
            int rowCount = tree.Paths.Count;
            int maxColCount = tree.get_Branch(tree.Paths[0]).Count;

            for (int col = 0; col < maxColCount; col++)
            {
                GH_Path newPath = new GH_Path(col);

                for (int row = 0; row < rowCount; row++)
                {
                    IList branch = tree.get_Branch(tree.Paths[row]);
                    if (branch.Count > col)
                    {
                        T item = (T)branch[col];
                        flippedTree.Append(item, newPath);
                    }
                }
            }
            return flippedTree;
        }
    }
}
