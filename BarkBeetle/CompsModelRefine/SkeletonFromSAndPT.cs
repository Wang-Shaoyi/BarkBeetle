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

using BarkBeetle.RefinedGeometry;

namespace BarkBeetle.CompsModelRefine
{
    public class SkeletonFromSAndPT : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the SkeletonFromSAndPT class.
        /// </summary>
        public SkeletonFromSAndPT()
          : base("SkeletonFromSAndPT", "Skeleton",
              "Skeleton is a data tree re-sorted by a certain sequence",
              "BarkBeetle", "Model Refine")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Surface", "S", "Base surface to organize the skeleton", GH_ParamAccess.item);
            pManager.AddPointParameter("Points Tree", "PT", "Input a point tree (m by n)", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Strip Width", "w", "Input the strip width", GH_ParamAccess.item, 0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Skeleton", "Sk", "Re-sorted the sequence of points", GH_ParamAccess.tree);
            pManager.AddGenericParameter("RefinedGeometry", "RG", "Refined Geometry object", GH_ParamAccess.item);
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
            double stripWidth = 0;

            //Set inputs
            if (!DA.GetData(0, ref surface)) return;
            if (!DA.GetDataTree(1, out pointsTree)) return;
            if (!DA.GetData(2, ref stripWidth)) return;

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

            // Run Function
            RefinedGeometryManager rgManager = new RefinedGeometryManager();
            rgManager.SetComponent(this);
            rgManager.SetRefinedGeometry(stripWidth, surface, pointsTree);
            rgManager.ProcessSkeletonPoints();
            GH_Structure<GH_Point> skeleton = rgManager.refinedGeometry.Skeleton;

            RefinedGeometryGoo geometryGoo = new RefinedGeometryGoo(rgManager.refinedGeometry);

            // Finally assign the spiral to the output parameter.
            DA.SetDataTree(0, skeleton);
            DA.SetData(1, geometryGoo);
        }

        public static GH_Structure<GH_Point> ProcessPointsOnSurface(Surface surface, GH_Structure<GH_Point> pointsTree, GH_Component component)
        {
            bool needFlip = false;

            // 1. Check if the tree needs to flip
            // Get surface uv direction at (0,0)
            Plane frame;
            surface.FrameAt(0, 0, out frame);
            Vector3d uSurface = frame.XAxis;
            Vector3d vSurface = frame.YAxis;

            // Get the first point of the tree
            GH_Path firstBranchPath = pointsTree.Paths[0];
            IList firstBranch = pointsTree.get_Branch(firstBranchPath);
            Point3d firstPoint = ((GH_Point)firstBranch[0]).Value;
            double uFirst, vFirst;
            surface.ClosestPoint(firstPoint, out uFirst, out vFirst);

            // Get the second point of the tree
            GH_Path secondBranchPath = pointsTree.Paths[1];
            IList secondBranch = pointsTree.get_Branch(secondBranchPath);
            Point3d secondPoint = ((GH_Point)secondBranch[0]).Value;

            // Calculate angle
            Vector3d uNewFirstPt = secondPoint - firstPoint;
            double uuAngle = Vector3d.VectorAngle(uNewFirstPt, uSurface);
            double uvAngle = Vector3d.VectorAngle(uNewFirstPt, vSurface);
            double uuAngleMin = Math.Min(uuAngle, Math.PI - uuAngle);
            double uvAngleMin = Math.Min(uvAngle, Math.PI - uvAngle);
            if (uuAngleMin > uvAngleMin) { needFlip = true; }

            // Flip if needed
            if (needFlip) pointsTree = TreeHelper.FlipMatrix(pointsTree, component);

            // 2. Turn GH_Structure to a 2D array
            List<int> treeSize = TreeHelper.GetTreeLayerLengths(pointsTree, component);
            int branch1Size = treeSize[0];
            int branch2Size = treeSize[1];

            // Initialize 2D array
            (double u, double v, Point3d point)[,] uvPointArray = new (double u, double v, Point3d point)[branch2Size, branch1Size];

            // Store all points and their UV in the array, and get the domain of these points
            double uMin = double.MaxValue;
            double vMin = double.MaxValue;
            int dir1 = 0;
            foreach (GH_Path path in pointsTree.Paths)
            {
                int dir2 = 0;
                foreach (GH_Point ghPt in pointsTree.get_Branch(path))
                {
                    Point3d pt = ghPt.Value;
                    double u, v;
                    if (surface.ClosestPoint(pt, out u, out v))
                    {
                        uvPointArray[dir1, dir2] = (u, v, pt);
                        if (u < uMin) uMin = u;
                        if (v < vMin) vMin = v;
                    }
                    dir2++;
                }
                dir1++;
            }

            // 3. Resort the sequence of the points to meet surface UV direction
            int dir1Count = uvPointArray.GetLength(0);
            int dir2Count = uvPointArray.GetLength(1);
            if (uFirst == uMin && vFirst == vMin)
            {
                // Check flip
                Console.WriteLine("1");
            }
            else if (uFirst != uMin && vFirst != vMin)
            {
                // Reverse u and v
                for (int i = 0; i < dir1Count / 2; i++)
                {
                    for (int j = 0; j < dir2Count / 2; j++)
                    {
                        var temp = uvPointArray[i, j];
                        uvPointArray[i, j] = uvPointArray[dir1Count - 1 - i, dir2Count - 1 - j];
                        uvPointArray[dir1Count - 1 - i, dir2Count - 1 - j] = temp;
                    }
                }
                Console.WriteLine("2");
            }
            else if (uFirst != uMin && vFirst == vMin)
            {
                // Reverse u
                for (int i = 0; i < dir1Count / 2; i++)
                {
                    for (int j = 0; j < dir2Count; j++)
                    {
                        var temp = uvPointArray[i, j];
                        uvPointArray[i, j] = uvPointArray[dir1Count - 1 - i, j];
                        uvPointArray[dir1Count - 1 - i, j] = temp;
                    }
                }
                Console.WriteLine("3");
            }
            else if (uFirst == uMin && vFirst != vMin)
            {
                // Reverse v
                for (int i = 0; i < dir1Count; i++)
                {
                    for (int j = 0; j < dir2Count / 2; j++)
                    {
                        var temp = uvPointArray[i, j];
                        uvPointArray[i, j] = uvPointArray[i, dir2Count - 1 - j];
                        uvPointArray[i, dir2Count - 1 - j] = temp;
                    }
                }
                Console.WriteLine("4");
            }
            else { Console.WriteLine("None"); }

            // 4. Turn back to GH_Structure
            GH_Structure<GH_Point> pointsTreeOut = new GH_Structure<GH_Point>();

            // Get the new dimensions
            int rowCount = uvPointArray.GetLength(0);
            int colCount = uvPointArray.GetLength(1);

            // Add the points to the tree
            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < colCount; j++)
                {
                    Point3d point = uvPointArray[i, j].point;
                    GH_Path path = new GH_Path(i);
                    pointsTreeOut.Append(new GH_Point(point), path);
                }
            }

            return pointsTreeOut;
        }


        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Resources.SkeletonFromSAndPT;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("2185F38E-9194-40A0-A386-5CA6A3335FF9"); }
        }
    }
}