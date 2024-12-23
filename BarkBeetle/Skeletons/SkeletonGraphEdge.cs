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
    internal class SkeletonGraphEdge:SkeletonGraph
    {
        // 0: all sides
        // 1: edge 1
        // 2: edge 2
        // 3: edge 3
        // 4: edge 4

        public SkeletonGraphEdge(UVNetwork network, int option): base(network, option) 
        {
        }

        public override BBPoint[,] OrganizeSkeletonStructure()
        {
            GH_Point[,] organizedPtsArray = UVNetwork.OrganizedPtsArray;
            GH_Vector[,,] uvVectors = UVNetwork.UVVectors;
            int uCnt = organizedPtsArray.GetLength(0);
            int vCnt = organizedPtsArray.GetLength(1);
            BBPoint[,] bbPoints = new BBPoint[uCnt, vCnt];

            switch (edgeOption)
            {
                case 0:
                    bbPoints = new BBPoint[uCnt,vCnt];

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
                    int turnCount = 0;

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
                        if ((nextR < 0 || nextR >= uCnt || nextC < 0 || nextC >= vCnt || visited[nextR, nextC]) && i != uCnt * vCnt - 1)
                        {
                            turnCount++;

                            if (turnCount != 4)
                            {
                                // Turn counter-clockwise
                                direction = (direction + 1) % 4;
                                turn = 1;
                                nextR = r + dr[direction];
                                nextC = c + dc[direction];
                            }

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

                        if (turnCount == 4)
                        {
                            bbPoints[r, c].NextIndex = (0, 0);
                            break;
                        }

                        // Add branch index
                        if (i == 0)
                        {
                            bbPoints[r, c].BranchIndex = (r, c + 1);
                        }

                        r = nextR;
                        c = nextC;
                    }
                    break;

                // Set all elements except the specified edge to null

                case 1: // Preserve the first column, set other columns to null
                    bbPoints = new BBPoint[uCnt, 1];

                    for (int i = 0; i < uCnt; i++)
                    {
                        Vector3d vecU = uvVectors[i, 0, 0].Value;
                        Vector3d vecV = uvVectors[i, 0, 1].Value;

                        ////// Calculate turn and next//////
                        Vector3d mainVec = vecU;
                        Vector3d subVec = vecV;

                        ////// Create Points //////
                        // Create this point
                        bbPoints[i, 0] = new BBPoint(
                            organizedPtsArray[i, 0].Value, 0,
                            mainVec, subVec);

                        // Add next index
                        if (i < uCnt - 1)
                        {
                            bbPoints[i, 0].NextIndex = (i+1, 0);
                        }
                    }
                    break;
                case 2: // Preserve the first row, set other rows to null
                    bbPoints = new BBPoint[1, vCnt];

                    for (int i = 0; i < vCnt; i++)
                    {
                        Vector3d vecU = uvVectors[0, i, 0].Value;
                        Vector3d vecV = uvVectors[0, i, 1].Value;

                        ////// Calculate turn and next//////
                        Vector3d mainVec = vecV;
                        Vector3d subVec = vecU;

                        ////// Create Points //////
                        // Create this point
                        bbPoints[0, i] = new BBPoint(
                            organizedPtsArray[0, i].Value, 0,
                            mainVec, subVec);

                        // Add next index
                        if (i < vCnt - 1)
                        {
                            bbPoints[0 , i].NextIndex = (0, i+1);
                        }
                    }
                    break;

                case 3: // Preserve the last column, set other columns to null
                    bbPoints = new BBPoint[uCnt, 1];

                    for (int i = 0; i < uCnt; i++)
                    {
                        Vector3d vecU = uvVectors[i, vCnt-1, 0].Value;
                        Vector3d vecV = uvVectors[i, vCnt - 1, 1].Value;

                        ////// Calculate turn and next//////
                        Vector3d mainVec = vecU;
                        Vector3d subVec = vecV;

                        ////// Create Points //////
                        // Create this point
                        bbPoints[i, 0] = new BBPoint(
                            organizedPtsArray[i, vCnt - 1].Value, 0,
                            mainVec, subVec);

                        // Add next index
                        if (i < uCnt - 1)
                        {
                            bbPoints[i, 0].NextIndex = (i + 1, 0);
                        }
                    }
                    break;
                case 4: // Preserve the last row, set other rows to null
                    bbPoints = new BBPoint[1, vCnt];

                    for (int i = 0; i < vCnt; i++)
                    {
                        Vector3d vecU = uvVectors[uCnt - 1, i, 0].Value;
                        Vector3d vecV = uvVectors[uCnt - 1, i, 1].Value;

                        ////// Calculate turn and next//////
                        Vector3d mainVec = vecV;
                        Vector3d subVec = vecU;

                        ////// Create Points //////
                        // Create this point
                        bbPoints[0, i] = new BBPoint(
                            organizedPtsArray[uCnt - 1, i].Value, 0,
                            mainVec, subVec);

                        // Add next index
                        if (i < vCnt - 1)
                        {
                            bbPoints[0, i].NextIndex = (0, i + 1);
                        }
                    }
                    break;
            }

            return bbPoints;
        }


    }
}
