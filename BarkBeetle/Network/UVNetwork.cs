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
using static System.Xml.Schema.XmlSchemaInference;
using BarkBeetle.Pattern;

namespace BarkBeetle.Network
{
    internal abstract class UVNetwork
    {
        // 0 strip width
        private double stripWidth;
        public double StripWidth
        {
            get { return stripWidth; }
            set { stripWidth = value; }
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
        public UVNetwork()
        {
        }

        public enum NetworkReferenceOption
        {
            Point,
            Surface,
            Mesh  
        }
        public static NetworkReferenceOption ConvertToReferenceOption(int value)
        {
            if (value < 0 || value > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Value must be between 0 and 2.");
            }

            return (NetworkReferenceOption)value;
        }


    }
}
