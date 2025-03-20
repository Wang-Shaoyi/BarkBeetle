using BarkBeetle.Network;
using BarkBeetle.Utils;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BarkBeetle.Skeletons
{
    internal class SkeletonGraphLinear:SkeletonGraph
    {

        public SkeletonGraphLinear(UVNetwork network, int option): base(network, option) 
        {
        }

        public override BBPoint[,] OrganizeSkeletonStructure()
        {
            GH_Point[,] organizedPtsArray = UVNetwork.OrganizedPtsArray;
            GH_Vector[,,] uvVectors = UVNetwork.UVVectors;
            int uCnt = 2;
            int vCnt = organizedPtsArray.GetLength(1);
            BBPoint[,] bbPoints = new BBPoint[uCnt, vCnt];

            Curve mainCrv = UVNetwork.UVCurves[0][0].Value;

            int c = 0;
            int turn = 0;


            for (int i = 0; i < vCnt; i++)
            {
                Point3d currentPoint = organizedPtsArray[0, c].Value;
                

                Vector3d mainVec = uvVectors[0, c, 0].Value;
                Vector3d subVec = uvVectors[0, c, 1].Value;

                ////// Calculate turn and next//////
                int nextC = c + 1;

                if(CurveUtils.IsPointADiscontinuity(mainCrv, currentPoint,30))
                {
                    turn = CurveUtils.DetermineCurveDirection(mainCrv, currentPoint);
                }
                else turn = 0;

                ////// Create Points //////
                // Create this point
                bbPoints[0, c] = new BBPoint(
                    currentPoint, turn,
                    mainVec, subVec);

                // Add next index
                if (i < vCnt - 1)
                {
                    bbPoints[0, c].NextIndex = (0, nextC);
                }

                // Add branch index
                if (organizedPtsArray[1, c] != null)
                {
                    bbPoints[0, c].BranchIndex = (1, c);
                    bbPoints[1, c] = new BBPoint(
                    organizedPtsArray[1, c].Value, 0,
                    uvVectors[1, c, 0].Value, uvVectors[1, c, 1].Value);
                }

                c = nextC;
            }

            return bbPoints;
        }


    }
}
