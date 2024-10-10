using BarkBeetle.GeometriesPackage;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarkBeetle.Toolpath
{
    internal class ToolpathBase
    {
        public Curve Curve { get; set; }

        public Point3d seamPt;
        public string type;
        GeometryPackage geometryPackage;
        private double pathWidth;

        public ToolpathBase(GeometryPackage gP, Point3d seam, double pw) 
        {
            pathWidth = pw;
            seamPt = seam;
            geometryPackage = gP;
            // Toolpath base
        }

        //private Curve SpiralToolpath()
        //{
        //    // Set up all needed properties
        //    Surface surface = geometryPackage.ExtendedSurface;

        //    GH_Point[,] organizedPtsArray = geometryPackage.Skeleton.OrganizedPtsArray;
        //    List<GH_Point> gH_Points = geometryPackage.Skeleton.SkeletonPoints;
        //    List<(int, int, int)> skeletonStructure = geometryPackage.Skeleton.SkeletonStructure;

        //    double stripWidth = geometryPackage.StripWidth;
        //    int depthNum = (int)(stripWidth / (pathWidth * 2));

        //    // Set up an empty array
        //    Point3d[,,] ptArray3D = new Point3d[gH_Points.Count, 4, depthNum];



        //}
    }
}
