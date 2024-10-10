using BarkBeetle.Utils;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BarkBeetle.Skeletons;

namespace BarkBeetle.GeometriesPackage
{
    internal class GeometryPackage
    {
        private double stripWidth;
        public double StripWidth
        {
            get { return stripWidth; }
        }

        // 1 organized points
        private GH_Structure<GH_Point> organizedPtsTree;
        public GH_Structure<GH_Point> OrganizedPtsTree
        {
            get { return organizedPtsTree; }
        }

        // 2 extended surface
        private Surface extendedSurface;
        public Surface ExtendedSurface
        {
            get { return extendedSurface; }
        }

        // 3 Skeleton (OrganizedPtsArray, SkeletonStructure, SkeletonPoints)
        private Skeleton skeleton;
        public Skeleton Skeleton
        {
            get { return skeleton; }
        }

        // 4 Curve Geometry: uv curves that interpolates the points (optional for GeometryPackage constructor)
        private List<List<GH_Curve>> uvCurves;
        public List<List<GH_Curve>> UVCurves
        {
            get { return uvCurves; }
            set { uvCurves = value; }
        }

        // 5 uv vector for each point on surface (optional for GeometryPackage constructor)
        private GH_Vector[,,] uvVectors;
        public GH_Vector[,,] UVVectors
        {
            get { return uvVectors; }
            set { uvVectors = value; }
        }

        // Constructor
        public GeometryPackage(double stripWidth, Surface extendedSurface, GH_Structure<GH_Point> organizedPtsTree, string skeletonOption)
        {
            this.stripWidth = stripWidth;
            this.extendedSurface = extendedSurface;
            this.organizedPtsTree = organizedPtsTree;
            if (skeletonOption == Resources.SpiralSkeletonString) this.skeleton = new SkeletonSpiral(organizedPtsTree);
        }
    }

}

