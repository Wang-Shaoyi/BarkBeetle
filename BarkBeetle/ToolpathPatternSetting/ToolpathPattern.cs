using BarkBeetle.GeometriesPackage;
using Grasshopper.Kernel.Geometry;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BarkBeetle.Utils;
using Rhino.Collections;

namespace BarkBeetle.ToolpathBaseSetting
{
    internal abstract class ToolpathPattern
    {
        public virtual string ToolpathBaseName { get; set; } = "toolpath";
        private Curve curve { get; set; }
        public Curve Curve
        {
            get { return curve; }
            set { curve = value; }
        }

        private List<Curve> curves { get; set; }
        public List<Curve> Curves
        {
            get { return curves; }
            set { curves = value; }
        }

        private Point3d[,,] cornerPts { get; set; }
        public Point3d[,,] CornerPts
        {
            get { return cornerPts; }
            set { cornerPts = value; }
        }

        private Point3d seamPt { get; set; }
        public Point3d SeamPt
        {
            get { return seamPt; }
        }

        private SkeletonPackage skeletonPackage { get; set; }
        public SkeletonPackage SkeletonPackage
        {
            get { return skeletonPackage; }
        }

        private double pathWidth { get; set; }
        public double PathWidth
        {
            get { return pathWidth; }
        }

        public ToolpathPattern(SkeletonPackage gP, Point3d seam, double pw) 
        {
            pathWidth = pw;
            seamPt = seam;
            skeletonPackage = gP;

            ConstructToolpathBase();
        }

        public abstract void ConstructToolpathBase();

    }
}
