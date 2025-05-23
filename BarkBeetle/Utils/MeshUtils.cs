﻿using Rhino.Geometry;
using Rhino;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BarkBeetle.ToolpathStackSetting;
using Grasshopper.Kernel.Types;
using Rhino.Geometry.Collections;
using static System.Net.Mime.MediaTypeNames;
using System.Security.Cryptography;

namespace BarkBeetle.Utils
{
    internal class MeshUtils
    {
        public static Mesh MeshFromToolpathStack(ToolpathStack toolpathStack, double portion)
        {
            List<List<GH_Plane>> orientPlanes = toolpathStack.OrientPlanes;
            List<List<GH_Number>> speedFactors = toolpathStack.SpeedFactors;
            double h = toolpathStack.LayerHeight;
            double d = 0.0;


            if (toolpathStack.Patterns.BottomPattern != null) d = toolpathStack.Patterns.BottomPattern.PathWidth/2;
            else d = toolpathStack.Patterns.MainPatterns[0].PathWidth / 2;

            List<Line> lines = new List<Line>();

            int count = 0;
            for (int i = 0; i < orientPlanes.Count;  i++)
            {
                List<GH_Plane> planes = orientPlanes[i];
                List<GH_Number> speeds = speedFactors[i];

                for (int j = 0; j < planes.Count; j++)
                {
                    Plane p = planes[j].Value;
                    Vector3d z = p.ZAxis; 

                    if(z.Z < 0)
                    {
                        z = -z;
                    }

                    Line sdl = new Line(p.Origin, z, h / speeds[j].Value);
                    lines.Add(sdl);
                    count++;
                }
            }
            int subCount = (int)(count * portion);

            List<Line> subLines = lines.GetRange(0, subCount);
            Mesh loftmesh = LoftMesh(subLines);
            Mesh extrudedMesh = ExtrudeMesh(toolpathStack, loftmesh, d);

            return extrudedMesh;
        }

        public static Mesh LoftMesh(List<Line> lines)
        {
            // 1. Initialize
            Mesh mesh = new Mesh();

            // 2. Go through all lines
            List<Point3d> startPoints = new List<Point3d>();
            List<Point3d> endPoints = new List<Point3d>();

            foreach (var line in lines)
            {
                startPoints.Add(line.From);
                endPoints.Add(line.To);
            }

            // 3. And points to mesh vertices
            foreach (var pt in startPoints)
                mesh.Vertices.Add(pt);

            foreach (var pt in endPoints)
                mesh.Vertices.Add(pt);

            int count = startPoints.Count;

            // 4. connect faces
            for (int i = 0; i < count - 1; i++)
            {
                int next = (i + 1) % count;
                mesh.Faces.AddFace(i, next, next + count, i + count);
            }

            // 5. compute normals
            mesh.Normals.ComputeNormals();
            mesh.Compact();
            return mesh;
        }

        #region extrude mesh
        public static Mesh ExtrudeMesh(ToolpathStack toolpathStack, Mesh M, double d)
        {
            // Compute normals
            if (M.Normals.Count == 0)
                M.Normals.ComputeNormals();

            MeshVertexList vertices = M.Vertices;
            MeshFaceList faces = M.Faces;
            MeshVertexNormalList normals = M.Normals;

            List<Point3d> stackCrvPts = ConvertPlanesToPoints(toolpathStack.OrientPlanes);

            int vertexCount = vertices.Count;
            int controlCount = vertexCount / 2;
            List<Point3d> topVertices = new List<Point3d>();
            List<Point3d> bottomVertices = new List<Point3d>();


            // generate top and bottom vertices from normals
            for (int i = 0; i < vertexCount; i++)
            {
                Vector3d normal = normals[i];
                normal.Unitize();
                double offset = d;

                Point3d vertix = new Point3d(vertices[i].X, vertices[i].Y, vertices[i].Z);
                bool vertixDiscontinuity = false;

                //// TODO: This is for more precise modeling, will move to another function later
                //Point3d closestPt = PointDataUtils.FindNearestPoint(vertix, stackCrvPts);
                //Curve stackCrv = toolpathStack.FinalCurve.Value;
                //vertixDiscontinuity = CurveUtils.IsPointADiscontinuity(stackCrv, closestPt, 30);

                // update top and buttom vertices
                if (vertixDiscontinuity)
                {
                    topVertices.Add(vertix + normal * offset * 1.414);
                    bottomVertices.Add(vertix - normal * offset * 1.414);
                }
                else
                {
                    topVertices.Add(vertix + normal * offset);
                    bottomVertices.Add(vertix - normal * offset);
                }
            }

            // Construct new mesh and add vertices
            Mesh resultMesh = new Mesh();
            resultMesh.Vertices.AddVertices(topVertices);
            resultMesh.Vertices.AddVertices(bottomVertices);

            // add faces
            AddFaces(resultMesh, faces, vertexCount);

            resultMesh.UnifyNormals();
            resultMesh.Normals.ComputeNormals();
            resultMesh.Compact();
            return resultMesh;
        }
        private static void AddFaces(Mesh mesh, MeshFaceList faces, int vertexCount)
        {
            foreach (MeshFace face in faces)
            {
                // top
                mesh.Faces.AddFace(face);

                // bottom
                if (face.IsQuad)
                {
                    mesh.Faces.AddFace(
                        face.A + vertexCount, face.B + vertexCount,
                        face.C + vertexCount, face.D + vertexCount);
                }
                else
                {
                    mesh.Faces.AddFace(
                        face.A + vertexCount, face.B + vertexCount, face.C + vertexCount);
                }
            }

            // side
            for (int i = 0; i < vertexCount; i++)
            {
                if (i != vertexCount - 1 && i != vertexCount / 2 -1)
                {
                    int next = i + 1;
                    mesh.Faces.AddFace(i, next, next + vertexCount, i + vertexCount);
                }
            }

            //Caps
            mesh.Faces.AddFace(0, vertexCount / 2, vertexCount / 2  + vertexCount, vertexCount);
            mesh.Faces.AddFace(vertexCount -1 , vertexCount / 2 - 1, vertexCount / 2 + vertexCount -1, 2* vertexCount-1);
        }
        #endregion


        public static List<Point3d> ConvertPlanesToPoints(List<List<GH_Plane>> planes)
        {
            List<Point3d> points = new List<Point3d>(); // 创建一个新的点列表

            foreach (List<GH_Plane> planeList in planes)
            {
                foreach (GH_Plane plane in planeList)
                {
                    points.Add(plane.Value.Origin); // 将每个平面的原点添加到点列表中
                }
            }

            return points; // 返回包含所有原点的点列表
        }

        #region smooth mesh
        // Smooth a mesh (like weaver bird)
        // Reference: https://lotsacode.wordpress.com/2013/04/10/catmull-clark-surface-subdivider-in-c/
        public static Mesh Subdivide(Mesh inputMesh)
        {
            if (inputMesh == null) return null;

            Mesh subdividedMesh = new Mesh();

            // 1. Compute face points (average of all points in the face)
            Dictionary<int, Point3d> facePoints = CreateFacePoints(inputMesh);

            // 2. Compute edge points (average of edge midpoint and adjacent face points)
            Dictionary<(int, int), Point3d> edgePoints = CreateEdgePoints(inputMesh, facePoints);

            // 3. Compute new vertex positions (updated using face and edge averages)
            Dictionary<int, Point3d> vertexPoints = CreateVertexPoints(inputMesh, edgePoints, facePoints);

            // 4. Create new faces by subdividing original mesh faces
            CreateFaces(inputMesh, subdividedMesh, vertexPoints, edgePoints, facePoints);

            // Compute normals and finalize the mesh
            subdividedMesh.Normals.ComputeNormals();
            subdividedMesh.Compact();

            return subdividedMesh;
        }

        // Compute face points (average of face vertices)
        private static Dictionary<int, Point3d> CreateFacePoints(Mesh mesh)
        {
            Dictionary<int, Point3d> facePoints = new Dictionary<int, Point3d>();
            for (int i = 0; i < mesh.Faces.Count; i++)
            {
                facePoints[i] = mesh.Faces.GetFaceCenter(i);
            }
            return facePoints;
        }

        // Compute edge points (average of edge midpoint and adjacent face points)
        private static Dictionary<(int, int), Point3d> CreateEdgePoints(Mesh mesh, Dictionary<int, Point3d> facePoints)
        {
            Dictionary<(int, int), Point3d> edgePoints = new Dictionary<(int, int), Point3d>();
            for (int i = 0; i < mesh.TopologyEdges.Count; i++)
            {
                IndexPair edgeVertices = mesh.TopologyEdges.GetTopologyVertices(i);
                int v1 = edgeVertices.I, v2 = edgeVertices.J;

                Point3d midpoint = Average(mesh.Vertices[v1], mesh.Vertices[v2]);
                Point3d edgePoint;

                // Check if the edge is on the boundary
                if (mesh.TopologyEdges.GetConnectedFaces(i).Length == 1)
                {
                    edgePoint = midpoint;
                }
                else
                {
                    int[] connectedFaces = mesh.TopologyEdges.GetConnectedFaces(i);
                    Point3d faceCenter1 = facePoints[connectedFaces[0]];
                    Point3d faceCenter2 = facePoints[connectedFaces[1]];
                    edgePoint = Average(midpoint, faceCenter1, faceCenter2);
                }

                edgePoints[(v1, v2)] = edgePoint;
                edgePoints[(v2, v1)] = edgePoint;  // Store in both directions
            }
            return edgePoints;
        }

        // Compute new vertex positions based on face and edge averages
        private static Dictionary<int, Point3d> CreateVertexPoints(Mesh mesh, Dictionary<(int, int), Point3d> edgePoints, Dictionary<int, Point3d> facePoints)
        {
            Dictionary<int, Point3d> vertexPoints = new Dictionary<int, Point3d>();
            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                Point3d oldVertex = mesh.Vertices[i];

                var connectedFaces = mesh.TopologyVertices.ConnectedFaces(i);
                var connectedEdges = mesh.TopologyVertices.ConnectedEdges(i);

                // Average of face points
                Point3d avgFacePoints = Average(connectedFaces.Select(f => facePoints[f]).ToArray());

                // Average of edge midpoints
                Point3d avgEdgePoints = Average(connectedEdges.Select(e => edgePoints[(mesh.TopologyEdges.GetTopologyVertices(e).I, mesh.TopologyEdges.GetTopologyVertices(e).J)]).ToArray());

                int n = connectedFaces.Length;

                // Formula: newVertex = (m1 * oldVertex) + (m2 * avgFacePoints) + (m3 * avgEdgePoints)
                double m1 = (n - 3.0) / n;
                double m2 = 1.0 / n;
                double m3 = 2.0 / n;

                Point3d newVertex = (m1 * oldVertex) + (m2 * avgFacePoints) + (m3 * avgEdgePoints);
                vertexPoints[i] = newVertex;
            }
            return vertexPoints;
        }

        // Create new faces by subdividing the original faces
        private static void CreateFaces(Mesh originalMesh, Mesh subdividedMesh, Dictionary<int, Point3d> vertexPoints, Dictionary<(int, int), Point3d> edgePoints, Dictionary<int, Point3d> facePoints)
        {
            for (int i = 0; i < originalMesh.Faces.Count; i++)
            {
                MeshFace face = originalMesh.Faces[i];
                Point3d facePoint = facePoints[i];

                // Get original vertices and edge points
                List<Point3d> newVertices = new List<Point3d>();

                int vertexCount = face.IsQuad ? 4 : 3;  // Check if it's a triangle or a quad
                for (int j = 0; j < vertexCount; j++)
                {
                    int v1 = face[j];
                    int v2 = face[(j + 1) % vertexCount];

                    newVertices.Add(vertexPoints[v1]);
                    newVertices.Add(edgePoints[(v1, v2)]);
                }

                // Add the new subdivided faces (triangles or quads)
                if (vertexCount == 4)
                {
                    // Add quads
                    subdividedMesh.Vertices.AddVertices(newVertices);
                    subdividedMesh.Faces.AddFace(0, 1, 2, 3);
                }
                else
                {
                    // Add triangles
                    subdividedMesh.Vertices.AddVertices(newVertices);
                    subdividedMesh.Faces.AddFace(0, 1, 2);
                }
            }
        }

        // Average helper function
        private static Point3d Average(params Point3d[] points)
        {
            return new Point3d(points.Average(p => p.X), points.Average(p => p.Y), points.Average(p => p.Z));
        }
        #endregion
    }
}
