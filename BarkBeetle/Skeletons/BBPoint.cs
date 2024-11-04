using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarkBeetle.Skeletons
{
    internal class BBPoint
    {
        public Point3d CurrentPt3d;
        public Vector3d VectorU;
        public Vector3d VectorV;
        public int TurningType; //turn(-1 clockwise, 0 no turn, 1 counter-clockwise)
        public (int, int) NextIndex = (-1, -1); 
        public (int, int) BranchIndex = (-1, -1); 

        public BBPoint(Point3d currentPt3d, int turn, Vector3d u, Vector3d v)
        {
            CurrentPt3d = currentPt3d;
            TurningType = turn;
            VectorU = u;
            VectorV = v;
        }


        public static BBPoint FindByIndex((int, int) index, BBPoint[,] bbPointsMatrix)
        {
            int u = index.Item1;
            int v = index.Item2;

            if (u >= 0 && u < bbPointsMatrix.GetLength(0) && v >= 0 && v < bbPointsMatrix.GetLength(1))
            {
                return bbPointsMatrix[u, v];
            }
            return null;
        }

        public bool IsNextIndexAssigned()
        {
            return NextIndex != (-1, -1);
        }

        public bool IsBranchIndexAssigned()
        {
            return BranchIndex != (-1, -1);
        }

    }
}
