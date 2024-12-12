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
using Rhino.Geometry;
using Grasshopper;

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

            int uNum = layerLengths[0];
            int vNum = layerLengths.Count;

            return new List<int> { uNum, vNum };
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
            int uCount = tree.Paths.Count;
            int maxVCount = tree.get_Branch(tree.Paths[0]).Count;

            for (int v= 0; v < maxVCount; v++)
            {
                GH_Path newPath = new GH_Path(v);

                for (int u = 0; u < uCount; u++)
                {
                    IList branch = tree.get_Branch(tree.Paths[u]);
                    if (branch.Count > v)
                    {
                        T item = (T)branch[v];
                        flippedTree.Append(item, newPath);
                    }
                }
            }
            return flippedTree;
        }

        // Flip tree no comp
        public static GH_Structure<T> FlipMatrixNoComp<T>(GH_Structure<T> tree) where T : IGH_Goo
        {
            tree.Simplify(GH_SimplificationMode.CollapseLeadingOverlaps);

            if (!CheckTreeFormat2D(tree))
            {
                return null;
            }

            GH_Structure<T> flippedTree = new GH_Structure<T>();
            int uCount = tree.Paths.Count;
            int maxVCount = tree.get_Branch(tree.Paths[0]).Count;

            for (int v = 0; v < maxVCount; v++)
            {
                GH_Path newPath = new GH_Path(v);

                for (int u = 0; u < uCount; u++)
                {
                    IList branch = tree.get_Branch(tree.Paths[u]);
                    if (branch.Count > v)
                    {
                        T item = (T)branch[v];
                        flippedTree.Append(item, newPath);
                    }
                }
            }
            return flippedTree;
        }


        // Convert List<List<T>> to GH_Structure
        public static GH_Structure<T> ConvertToGHStructure<T>(List<List<T>> dataList) where T : IGH_Goo
        {
            GH_Structure<T> ghStructure = new GH_Structure<T>();

            for (int i = 0; i < dataList.Count; i++)
            {
                List<T> innerList = dataList[i];
                GH_Path path = new GH_Path(i);

                for (int j = 0; j < innerList.Count; j++)
                {
                    T item = innerList[j];
                    ghStructure.Append(item, path);
                }
            }
            return ghStructure;
        }

        // Convert GH_Structure to List<List<T>> 
        public static List<List<T>> ConvertGHStructureToList<T>(GH_Structure<T> ghStructure) where T : IGH_Goo
        {
            ghStructure.Simplify(GH_SimplificationMode.CollapseLeadingOverlaps);

            int uCount = ghStructure.PathCount;
            List<List<T>> list = new List<List<T>>(uCount);

            // Iterate through each branch (u direction) of the GH_Structure
            for (int u = 0; u < uCount; u++)
            {
                IList branch = ghStructure.get_Branch(ghStructure.Paths[u]);
                List<T> innerList = new List<T>(branch.Count);

                // Add each element in the branch to the inner list
                for (int v = 0; v < branch.Count; v++)
                {
                    innerList.Add((T)branch[v]);
                }

                // Add the inner list to the outer list
                list.Add(innerList);
            }

            return list;
        }

            // Convert GH_Structure to 2D array
        public static T[,] ConvertGHStructureToArray<T>(GH_Structure<T> ghStructure) where T : IGH_Goo
        {
            ghStructure.Simplify(GH_SimplificationMode.CollapseLeadingOverlaps);

            int uCount = ghStructure.PathCount;
            int vCount = ghStructure.Branches.Max(b => b.Count);

            T[,] array = new T[uCount, vCount];

            // Iterate through each u direction (branch) of the GH_Structure
            for (int u = 0; u < uCount; u++)
            {
                IList branch = ghStructure.get_Branch(ghStructure.Paths[u]);

                // Iterate through each element in the branch and add it to the 2D array
                for (int v = 0; v < branch.Count; v++)
                {
                    array[u,v] = (T)branch[v]; 
                }

                // Fill the remaining columns with default values (e.g., null for reference types)
                for (int v = branch.Count; v < vCount; v++)
                {
                    array[u, v] = default;
                }
            }
            return array;
        }

        // Convert 2D array to multi-layer tree
        public static GH_Structure<T> Convert3DArrayToGHStructure<T>(T[,] array) where T : IGH_Goo
        {
            GH_Structure<T> structure = new GH_Structure<T>();
            int dim1 = array.GetLength(0);
            int dim2 = array.GetLength(1);

            for (int i = 0; i < dim1; i++)
            {
                for (int j = 0; j < dim2; j++)
                {
                    T item = array[i, j];
                    GH_Path path = new GH_Path(i);
                    structure.Append(item, path);
                }
            }
            return structure;
        }


        public static List<T> FlattenList<T>(List<List<T>> nestedList)
        {
            return nestedList.SelectMany(subList => subList).ToList();
        }

        public static List<T> Convert3DArrayToList<T>(T[,,] array)
        {
            List<T> result = new List<T>();

            for (int i = 0; i < array.GetLength(0); i++)
            {
                for (int j = 0; j < array.GetLength(1); j++)
                {
                    for (int k = 0; k < array.GetLength(2); k++)
                    {
                        result.Add(array[i, j, k]);
                    }
                }
            }

            return result;
        }

        public static GH_Structure<T> DuplicateTree<T>(GH_Structure<T> tree, int count) where T : IGH_Goo
        {
            GH_Structure<T> newTree = new GH_Structure<T>();

            for (int i = 0; i < count; i++)
            {
                foreach (GH_Path path in tree.Paths)
                {
                    IList<T> items = tree[path];
                    GH_Path newPath = new GH_Path(new int[] { i }.Concat(path.Indices).ToArray());
                    newTree.AppendRange(items, newPath);
                }
            }
            return newTree;
        }

        public static List<T> FlattenStructure<T>(GH_Structure<T> structure) where T : IGH_Goo
        {
            List<T> flattenedItems = new List<T>();

            // 遍历 GH_Structure 的所有路径
            foreach (GH_Path path in structure.Paths)
            {
                // 获取当前路径下的分支
                IList<T> branch = structure[path];

                // 添加非空元素到列表
                foreach (T item in branch)
                {
                    if (item != null)
                    {
                        flattenedItems.Add(item);
                    }
                }
            }

            return flattenedItems;
        }

        public static List<List<T>> FlattenStructureList<T>(GH_Structure<T> structure) where T : IGH_Goo
        {
            List<List<T>> flattenedItems = new List<List<T>>();

            // 遍历 GH_Structure 的所有路径
            foreach (GH_Path path in structure.Paths)
            {
                List<T> currentList = new List<T>();
                // 获取当前路径下的分支
                IList<T> branch = structure[path];

                // 添加非空元素到列表
                foreach (T item in branch)
                {
                    if (item != null)
                    {
                        currentList.Add(item);
                    }
                }
                flattenedItems.Add(currentList);
            }

            return flattenedItems;
        }

    }
}
