using BarkBeetle.Network;
using BarkBeetle.Utils;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace BarkBeetle.Skeletons
{
    internal class SkeletonGraphSnake:SkeletonGraph
    {
        public SkeletonGraphSnake(UVNetwork network, int edgeOption = 0) : base(network, edgeOption) { }

        public override BBPoint[,] OrganizeSkeletonStructure()
        {
            GH_Point[,] organizedPtsArray = UVNetwork.OrganizedPtsArray;
            GH_Vector[,,] uvVectors = UVNetwork.UVVectors;
            int uCnt = organizedPtsArray.GetLength(0);
            int vCnt = organizedPtsArray.GetLength(1);
            int countLastLine = Math.Abs(uCnt - vCnt) + 1;
            BBPoint[,] bbPoints = new BBPoint[uCnt,vCnt];

            int r = 0;
            int c = 0;

            // record directions
            int[] dr = { 1, 0, -1, 0};
            int[] dc = { 0, 1, 0, 1 };
            int[] du = { 0, -1, 0, 1 };
            int[] dv = { 1, 0, 1, 0 };
            int[] dturn = { 1, 1, -1, -1 };
            int direction = 0;

            for (int i = 0; i < uCnt * vCnt; i++)
            {
                int turn = 0;
                Vector3d vecU = uvVectors[r, c, 0].Value;
                Vector3d vecV = uvVectors[r, c, 1].Value;

                ////// Calculate turn and next//////
                int nextR = r + dr[direction];
                int nextC = c + dc[direction];
                Vector3d mainVec = dr[direction] * vecU + dc[direction] * vecV;
                Vector3d subVec = du[direction] * vecU + dv[direction] * vecV;

                // When turning
                if ((r == uCnt - 1 || r == 0) && i != uCnt * vCnt - 1 && i != 0)
                {
                    turn = dturn[direction];
                    direction = (direction + 1) % 4;
                    
                    nextR = r + dr[direction];
                    nextC = c + dc[direction];
                    mainVec = dr[direction] * vecU + dc[direction] * vecV;
                    subVec = du[direction] * vecU + dv[direction] * vecV;
                }

                ////// Create Points //////
                // Create this point
                bbPoints[r, c] = new BBPoint(
                    organizedPtsArray[r, c].Value, turn,
                    mainVec, subVec);

                // Add next index
                if (i < uCnt * vCnt - 1)
                {
                    bbPoints[r, c].NextIndex = (nextR, nextC);
                }

                // Add branch index
                if (dc[direction] == 0 && i <= uCnt * vCnt - 1 - uCnt)
                {
                    bbPoints[r, c].BranchIndex = (r , c + 1);
                }

                r = nextR;
                c = nextC;
            }

            return bbPoints;
        }

    }
}
