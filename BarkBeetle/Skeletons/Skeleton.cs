using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BarkBeetle.Utils;

namespace BarkBeetle.Skeletons
{
    internal abstract class Skeleton
    {
        /// <summary>
        /// Properties
        /// </summary>
        public virtual string SkeletonName { get; set; } = "skeletonBase";

        // 1 pts array organized from uv
        private GH_Point[,] organizedPtsArray { get; set; }
        public GH_Point[,] OrganizedPtsArray
        {
            get { return organizedPtsArray; }
        }

        // 2 skeleton structure
        // store each point with sequence as (uNum, vNum, turn(-1 clockwise, 0 no turn, 1 clockwise))
        private List<(int, int, int)> skeletonStructure;
        public List<(int, int, int)> SkeletonStructure
        {
            get { return skeletonStructure; }
        }

        // 3 skeleton points
        private List<GH_Point> skeletonPoints { get; set; }
        public List<GH_Point> SkeletonPoints
        {
            get { return skeletonPoints; }
        }

        // 4 skeleton curve
        private GH_Curve skeletonCurve { get; set; }
        public GH_Curve SkeletonCurve
        {
            get { return skeletonCurve; }
            set { skeletonCurve = value; }
        }

        // 5 uv vector for each point along the skeleton direction (optional for GeometryPackage constructor)
        private GH_Vector[,,] uvVectors;
        public GH_Vector[,,] UVVectors
        {
            get { return uvVectors; }
            set { uvVectors = value; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public Skeleton(GH_Structure<GH_Point> organizedPtsTree)
        {
            organizedPtsArray = TreeHelper.ConvertGHStructureToArray(organizedPtsTree);
            skeletonStructure = OrganizeSkeletonStructure();
            skeletonPoints = CreatePtListFromSkeleton();
        }

        /// <summary>
        /// Methods
        /// </summary>
        // Set up skeletonStructure from organizedPts, each skeleton has a different method
        public abstract List<(int, int, int)> OrganizeSkeletonStructure();


        // Set up skeletonPoints from skeletonStructure
        private List<GH_Point> CreatePtListFromSkeleton()
        {
            List<GH_Point> reorderedPoints = new List<GH_Point>();

            // Put points into List<GH_Point> skeletonPoints with the sequence in List<(int, int, int, int)> OrganizeSkeletonStructure()
            foreach (var (uNum, vNum, _) in skeletonStructure)
            {
                if (uNum < organizedPtsArray.GetLength(0) && vNum < organizedPtsArray.GetLength(1))
                {
                    GH_Point point = organizedPtsArray[uNum, vNum];
                    reorderedPoints.Add(point);
                }
            }
            return reorderedPoints;
        }



    }
}
