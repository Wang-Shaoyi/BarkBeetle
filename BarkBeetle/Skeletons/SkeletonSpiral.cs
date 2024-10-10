using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarkBeetle.Skeletons
{
    internal class SkeletonSpiral: Skeleton
    {
        public override string SkeletonName { get; set; } = "skeletonSpiral";

        public SkeletonSpiral(GH_Structure<GH_Point> organizedPtsTree) : base(organizedPtsTree){}


        public override List<(int, int, int)> OrganizeSkeletonStructure()
        {
            // Skeleton Structure is List<(u, v, turn(-1 clockwise, 0 no turn, 1 counter-clockwise)>
            List<(int, int, int)> skS = new List<(int, int, int)>();

            int uCnt = OrganizedPtsArray.GetLength(0);
            int vCnt = OrganizedPtsArray.GetLength(1);
            // store if each point was visited
            bool[,] visited = new bool[uCnt, vCnt];
            int r = 0;
            int c = 0;
            int turn = 0;
            // record directions
            int[] dr = { 1, 0, -1, 0 };
            int[] dc = { 0, 1, 0, -1 };
            int direction = 0;

            for (int i = 0; i < uCnt * vCnt; i++)
            {
                skS.Add((r, c, turn));
                visited[r, c] = true;
                turn = 0;

                int nextR = r + dr[direction];
                int nextC = c + dc[direction];

                if (nextR < 0 || nextR >= uCnt || nextC < 0 || nextC >= vCnt || visited[nextR, nextC])
                {
                    // Turn counter-clockwise
                    direction = (direction + 1) % 4;
                    turn = 1;
                    nextR = r + dr[direction];
                    nextC = c + dc[direction];
                }
                r = nextR;
                c = nextC;
            }

            return skS;

        }
    }
}
