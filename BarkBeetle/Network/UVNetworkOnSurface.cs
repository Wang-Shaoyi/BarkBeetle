using BarkBeetle.Utils;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarkBeetle.Network
{
    internal class UVNetworkOnSurface: UVNetwork
    {
        public UVNetworkOnSurface(Surface surface, GH_Structure<GH_Point> ptsTree, double stripWidth):base(surface, ptsTree,stripWidth)
        {
            OrganizedPtsTree = PointDataUtils.SurfaceClosestPtTree(ExtendedSurface, ptsTree);
            

            int uCount = OrganizedPtsTree.PathCount;
            int vCount = OrganizedPtsTree.Branches.Max(b => b.Count);

            GH_Vector[,,] uvVectors = new GH_Vector[uCount, vCount, 2];
            GH_Point[,] organizedPtsArray = new GH_Point[uCount, vCount];


            UVCurves = CurveUtils.GetUVCurvesVecPt(OrganizedPtsTree, ref uvVectors, ref organizedPtsArray);
            UVVectors = uvVectors;
            OrganizedPtsArray = organizedPtsArray;

        }
    }
}
