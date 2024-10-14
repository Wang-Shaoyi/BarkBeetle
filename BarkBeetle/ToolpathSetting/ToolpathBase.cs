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

namespace BarkBeetle.Toolpath
{
    internal abstract class ToolpathBase
    {
        public Curve Curve { get; set; }
        public List<Curve> Curves { get; set; }

        public Point3d[,,] CornerPts { get; set; }
        public Point3d seamPt;
        public virtual string ToolpathType { get; set; } = "toolpath";
        public GeometryPackage geometryPackage;
        public double pathWidth;

        public ToolpathBase(GeometryPackage gP, Point3d seam, double pw) 
        {
            pathWidth = pw;
            seamPt = seam;
            geometryPackage = gP;

            ConstructToolpathBase();
        }

        public abstract void ConstructToolpathBase();

    }
}
