using BarkBeetle.Pattern;
using BarkBeetle.Utils;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Xml.Schema.XmlSchemaInference;

namespace BarkBeetle.Network
{
    internal class UVNetworkFromPointList: UVNetwork
    {
        public UVNetworkFromPointList(Surface surface, Mesh mesh, List<GH_Point> ptsList, double stripWidth , NetworkReferenceOption option)
        {
            StripWidth = stripWidth;

            // Create a simple tree
            GH_Structure<GH_Point> ptsTree = new GH_Structure<GH_Point>();
            GH_Path path = new GH_Path(0);
            foreach (GH_Point pt in ptsList)
            {
                ptsTree.Append(pt, path);
            }

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
            int uCount = 1;
            int vCount = ptsList.Count;

            GH_Vector[,,] uvVectors = new GH_Vector[uCount, vCount, 2];
            GH_Point[,] organizedPtsArray = new GH_Point[uCount, vCount];

            GH_Curve uvcrv = new GH_Curve(CurveUtils.CreatePolyCurveOnSurface(surface, ptsList.Select(ghPt => ghPt.Value).ToList()));

            for (int i = 0; i < vCount; i++)
            {
                organizedPtsArray[0,i] = ptsList[i];

                uvcrv.Value.ClosestPoint(ptsList[i].Value, out double t);
                Vector3d tangent = uvcrv.Value.TangentAt(t);
                uvVectors[0,i,0] = new GH_Vector(tangent);

                ExtendedSurface.ClosestPoint(ptsList[i].Value, out double u, out double v);
                Vector3d normal = surface.NormalAt(u, v);
                Vector3d crossProduct = Vector3d.CrossProduct(normal, tangent);
                uvVectors[0, i, 1] = new GH_Vector(crossProduct);
            }

            List<GH_Curve> uvcrvs = new List<GH_Curve> { uvcrv };
            UVCurves = new List<List<GH_Curve>> { uvcrvs };
            UVVectors = uvVectors;
            OrganizedPtsArray = organizedPtsArray;


        }
    }
}
