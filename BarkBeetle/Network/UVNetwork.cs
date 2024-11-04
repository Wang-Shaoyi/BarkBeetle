using BarkBeetle.Skeletons;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BarkBeetle.Utils;

namespace BarkBeetle.Network
{
    internal abstract class UVNetwork
    {
        // 0 strip width
        private double stripWidth;
        public double StripWidth
        {
            get { return stripWidth; }
        }

        // 1 organized points
        private GH_Point[,] organizedPtsArray;
        public GH_Point[,] OrganizedPtsArray
        {
            get { return organizedPtsArray; }
            set { organizedPtsArray = value; }
        }

        private GH_Structure<GH_Point> organizedPtsTree;
        public GH_Structure<GH_Point> OrganizedPtsTree
        {
            get { return organizedPtsTree; }
            set { organizedPtsTree = value; }
        }

        // 2 extended surface
        private Surface extendedSurface;
        public Surface ExtendedSurface
        {
            get { return extendedSurface; }
            set { extendedSurface = value; }
        }

        // 3 Curve Geometry: uv curves that interpolates the points
        private List<List<GH_Curve>> uvCurves;
        public List<List<GH_Curve>> UVCurves
        {
            get { return uvCurves; }
            set { uvCurves = value; }
        }

        // 4 vectors
        private GH_Vector[,,] uvVectors;
        public GH_Vector[,,] UVVectors
        {
            get { return uvVectors; }
            set { uvVectors = value; }
        }

        // Constructor
        public UVNetwork(Surface surface, GH_Structure<GH_Point> ptsTree, double w)
        {
            stripWidth = w;
            this.extendedSurface = BrepUtils.ProcessExtendedSurface(stripWidth / 2, stripWidth / 2, surface);
        }

    }
}
