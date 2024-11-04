using BarkBeetle.Network;
using BarkBeetle.Utils;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarkBeetle.Skeletons
{
    internal class SkeletonGraphSpiral:SkeletonGraph
    {
        public SkeletonGraphSpiral(UVNetwork network): base(network) { }

        public override BBPoint[,] OrganizeSkeletonStructure()
        {
            GH_Point[,] organizedPtsArray = UVNetwork.OrganizedPtsArray;
            GH_Vector[,,] uvVectors = UVNetwork.UVVectors;
            int uCnt = organizedPtsArray.GetLength(0);
            int vCnt = organizedPtsArray.GetLength(1);
            int countLastLine = Math.Abs(uCnt - vCnt) + 1;
            BBPoint[,] bbPoints = new BBPoint[uCnt,vCnt];

            // store if each point was visited
            bool[,] visited = new bool[uCnt, vCnt];
            int r = 0;
            int c = 0;
            int turn = 0;

            // record directions
            int[] dr = { 1, 0, -1, 0 };
            int[] dc = { 0, 1, 0, -1 };
            int[] du = { 0, -1, 0, 1 };
            int[] dv = { 1, 0, -1, 0 };
            int direction = 0;

            for (int i = 0; i < uCnt * vCnt; i++)
            {
                visited[r, c] = true;
                turn = 0;

                Vector3d vecU = uvVectors[r, c, 0].Value;
                Vector3d vecV = uvVectors[r, c, 1].Value;

                ////// Calculate turn and next//////
                int nextR = r + dr[direction];
                int nextC = c + dc[direction];
                Vector3d mainVec = dr[direction] * vecU + dc[direction] * vecV;
                Vector3d subVec = du[direction] * vecU + dv[direction] * vecV;

                // When turning
                if (nextR < 0 || nextR >= uCnt || nextC < 0 || nextC >= vCnt || visited[nextR, nextC])
                {
                    // Turn counter-clockwise
                    direction = (direction + 1) % 4;
                    turn = 1;
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
                if (i < uCnt * vCnt -1)
                {
                    bbPoints[r, c].NextIndex = (nextR, nextC);
                }

                // Add branch index
                if (turn == 0 && i <= uCnt * vCnt - 1 - countLastLine)
                {
                    bbPoints[r, c].BranchIndex = (r + du[direction], c + dv[direction]);
                }

                r = nextR;
                c = nextC;
            }

            return bbPoints;
        }

    }
}
