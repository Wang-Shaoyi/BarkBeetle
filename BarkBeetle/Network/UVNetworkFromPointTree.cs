using BarkBeetle.Utils;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Xml.Schema.XmlSchemaInference;

namespace BarkBeetle.Network
{
    internal class UVNetworkFromPointTree: UVNetwork
    {
        public UVNetworkFromPointTree(Surface surface, Mesh mesh, GH_Structure<GH_Point> ptsTree, double stripWidth , NetworkReferenceOption option)
        {
            StripWidth = stripWidth;

            // Need to create the correct surface and OrganizedPtsTree here
            switch (option)
            {
                case NetworkReferenceOption.Point:
                    // Get points tree
                    OrganizedPtsTree = ptsTree;
                    surface = BrepUtils.CreateInterpolatedSurface(OrganizedPtsTree);
                    break;

                case NetworkReferenceOption.Surface:
                    if (surface == null) throw new ArgumentNullException(nameof(surface), "No surface provided");
                    break;

                case NetworkReferenceOption.Mesh:
                    if (mesh == null) throw new ArgumentNullException(nameof(mesh), "No mesh provided");
                    else
                    {
                        OrganizedPtsTree = PointDataUtils.MeshClosestPtTree(mesh, ptsTree);
                        surface = BrepUtils.CreateInterpolatedSurface(OrganizedPtsTree);
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(option), option, "Invalid reference option");
            }

            // Calculate extended surface
            ExtendedSurface = BrepUtils.ProcessExtendedSurface(stripWidth / 2, stripWidth / 2, surface);
            OrganizedPtsTree =  PointDataUtils.SurfaceClosestPtTree(ExtendedSurface, ptsTree);

            // Organize points and vectors to arrays, prepare for next steps 
            int uCount = OrganizedPtsTree.PathCount;
            int vCount = OrganizedPtsTree.Branches.Max(b => b.Count);

            GH_Vector[,,] uvVectors = new GH_Vector[uCount, vCount, 2];
            GH_Point[,] organizedPtsArray = new GH_Point[uCount, vCount];

            UVCurves = CurveUtils.GetUVCurvesVecPt(OrganizedPtsTree, ref uvVectors, ref organizedPtsArray);
            UVVectors = uvVectors;
            OrganizedPtsArray = organizedPtsArray;
        }
    }
}
