using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BarkBeetle.Utils;
using BarkBeetle.Network;
using Rhino.Geometry;

namespace BarkBeetle.Skeletons
{
    internal abstract class SkeletonGraph
    {
        /// <summary>
        /// Properties
        /// </summary>
        /// 

        // 0 network
        private UVNetwork uvNetwork;
        public UVNetwork UVNetwork
        {
            get { return uvNetwork; }
        }

        // 1 pts array organized from uv
        private BBPoint[,] bbPointArray { get; set; }
        public BBPoint[,] BBPointArray
        {
            get { return bbPointArray; }
            set { bbPointArray = value; }
        }

        // 2 pts list
        private List<GH_Point> skeletonPtList { get; set; }
        public List<GH_Point> SkeletonPtList
        {
            get { return skeletonPtList; }
            set { skeletonPtList = value; }
        }

        // 3 skeleton curve
        private GH_Curve skeletonMainCurve { get; set; }
        public GH_Curve SkeletonMainCurve
        {
            get { return skeletonMainCurve; }
            set { skeletonMainCurve = value; }
        }

        // 4 skeleton branch curve
        private List<GH_Curve> skeletonBranchCurves { get; set; }
        public List<GH_Curve> SkeletonBranchCurves
        {
            get { return skeletonBranchCurves; }
            set { skeletonBranchCurves = value; }
        }

        // 5 skeleton branch curve
        private List<GH_Vector> skeletonVectors { get; set; }
        public List<GH_Vector> SkeletonVectors
        {
            get { return skeletonVectors; }
            set { skeletonVectors = value; }
        }

        public int edgeOption;

        /// <summary>
        /// Constructor
        /// </summary>
        public SkeletonGraph(UVNetwork network, int edgeOption)
        {
            this.edgeOption = edgeOption % 4;
            uvNetwork = network;
            bbPointArray = OrganizeSkeletonStructure();
            ProcessSkeletonCurves();
            
        }

        /// <summary>
        /// Methods
        /// </summary>
        // Set up skeletonStructure from organizedPts, each skeleton has a different method
        public abstract BBPoint[,] OrganizeSkeletonStructure();

        public void ProcessSkeletonCurves()
        {
            Surface surface = uvNetwork.ExtendedSurface.Duplicate() as Surface;

            int uCnt = uvNetwork.OrganizedPtsArray.GetLength(0);
            int vCnt = uvNetwork.OrganizedPtsArray.GetLength(1);

            List<GH_Point> pts = new List<GH_Point>();
            List<Curve> surfaceCurves = new List<Curve>();
            List<GH_Curve> branches = new List<GH_Curve>();
            List<GH_Vector> vectors = new List<GH_Vector>();

            BBPoint curBBPoint = BBPointArray[0, 0];

            for (int i = 0; i < CountNonNull(BBPointArray); i++)
            {
                Point3d currentPt = curBBPoint.CurrentPt3d;
                pts.Add(new GH_Point(currentPt));
                vectors.Add(new GH_Vector(curBBPoint.VectorU));
                vectors.Add(new GH_Vector(curBBPoint.VectorV));

                if (curBBPoint.IsBranchIndexAssigned())
                {
                    // Draw branches
                    Point3d branchPt = BBPoint.FindByIndex(curBBPoint.BranchIndex, BBPointArray).CurrentPt3d;
                    Curve curveBranch = surface.InterpolatedCurveOnSurface(new List<Point3d> { currentPt, branchPt },0.1);
                    branches.Add(new GH_Curve(curveBranch));
                }

                if (curBBPoint.IsNextIndexAssigned())
                {
                    BBPoint nextBBPoint = BBPoint.FindByIndex(curBBPoint.NextIndex, BBPointArray);
                    // Draw main curve
                    Point3d nextPt = nextBBPoint.CurrentPt3d;
                    Curve curve = surface.InterpolatedCurveOnSurface(new List<Point3d> { currentPt, nextPt }, 0.01);
                    surfaceCurves.Add(curve);
                    // Get to next point
                    curBBPoint = nextBBPoint;
                }
                else break;
            }

            Curve[] surfaceCurve = Curve.JoinCurves(surfaceCurves, 0.01);

            skeletonPtList = pts;
            skeletonMainCurve = new GH_Curve(surfaceCurve[0]);
            skeletonBranchCurves = branches;
            skeletonVectors = vectors;
        }

        private static int CountNonNull(BBPoint[,] bbPointArray)
        {
            int nonNullCount = 0;
            int rows = bbPointArray.GetLength(0);
            int cols = bbPointArray.GetLength(1);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (bbPointArray[r, c] != null)
                    {
                        nonNullCount++; 
                    }
                }
            }

            return nonNullCount; 
        }
    }
}
