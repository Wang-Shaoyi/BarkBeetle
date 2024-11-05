using Grasshopper.Kernel.Geometry;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BarkBeetle.Utils;
using BarkBeetle.Skeletons;
using Rhino.Collections;

namespace BarkBeetle.Pattern
{
    internal abstract class ToolpathPattern
    {
        // 0 Skeleton
        private SkeletonGraph skeleton { get; set; }
        public SkeletonGraph Skeleton
        {
            get { return skeleton; }
        }

        // 1 Pattern bundle curves
        private List<Curve> bundleCurves { get; set; }
        public List<Curve> BundleCurves
        {
            get { return bundleCurves; }
            set { bundleCurves = value; }
        }

        // 2 Pattern continuous curves
        private Curve coutinuousCurve { get; set; }
        public Curve CoutinuousCurve
        {
            get { return coutinuousCurve; }
            set { coutinuousCurve = value; }
        }

        // 3 Pattern corner points
        private Point3d[,,] cornerPts { get; set; }
        public Point3d[,,] CornerPts
        {
            get { return cornerPts; }
            set { cornerPts = value; }
        }

        // 4 Seam points
        private Point3d seamPt { get; set; }
        public Point3d SeamPt
        {
            get { return seamPt; }
        }

        // 5 Path width
        private double pathWidth { get; set; }
        public double PathWidth
        {
            get { return pathWidth; }
        }

        public ToolpathPattern(SkeletonGraph sG, Point3d seam, double pw) 
        {
            pathWidth = pw;
            seamPt = seam;
            skeleton = sG;

            ConstructToolpathPattern();
        }

        public abstract void ConstructToolpathPattern();



    }
}
