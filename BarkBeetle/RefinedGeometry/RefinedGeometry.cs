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

namespace BarkBeetle.RefinedGeometry
{
    internal class RefinedGeometry
    {
        // Fields
        private double stripWidth;

        // Surface
        private Surface surface;

        // points data tree
        private GH_Structure<GH_Point> pointsTree;

        // skeleton
        private GH_Structure<GH_Point> skeleton;
        public GH_Structure<GH_Point> Skeleton
        {
            get { return skeleton; }
            set
            {
                if (value != null)
                {
                    skeleton = value;
                }
            }
        }

        // extended surface
        private Surface extendedSurface;
        public Surface ExtendedSurface
        {
            get { return extendedSurface; }
            set
            {
                if (value != null)
                {
                    extendedSurface = value;
                }
            }
        }

        // Constructor
        public RefinedGeometry(double stripWidth, Surface surface, GH_Structure<GH_Point> pointsTree)
        {
            this.stripWidth = stripWidth;
            this.surface = surface;
            this.pointsTree = pointsTree;
        }

        // Getter for the processed skeleton
        //TODO: error handling



        // Getter for the extended surface
        public Surface GetExtendedSurface()
        {
            return extendedSurface;
        }

        public double GetStripWidth()
        {
            return stripWidth;
        }
       
        public Surface GetSurface()
        {
            return surface;
        }

        public GH_Structure<GH_Point> GetPointsTree()
        {
            return pointsTree;
        }
    }

}

