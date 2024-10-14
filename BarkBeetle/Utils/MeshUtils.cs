using Rhino.Geometry;
using Rhino;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarkBeetle.Utils
{
    internal class MeshUtils
    {
        public static Mesh CatmullClark(Mesh inputMesh)
        {
            if (inputMesh == null) return null;

            // Create the new mesh
            Mesh newMesh = new Mesh();

            // Store new points for faces, edges, and vertices
            Dictionary<int, Point3d> facePoints = new Dictionary<int, Point3d>();
            Dictionary<(int, int), Point3d> edgePoints = new Dictionary<(int, int), Point3d>();
            Dictionary<int, Point3d> vertexPoints = new Dictionary<int, Point3d>();

            // 1. Compute face points (center of each face)
            for (int i = 0; i < inputMesh.Faces.Count; i++)
            {
                MeshFace face = inputMesh.Faces[i];
                Point3d faceCenter = inputMesh.Faces.GetFaceCenter(i);
                facePoints[i] = faceCenter;
            }

            // 2. Compute edge points (midpoint of each edge)
            for (int i = 0; i < inputMesh.TopologyEdges.Count; i++)
            {
                IndexPair edgeVertices = inputMesh.TopologyEdges.GetTopologyVertices(i);
                int v1 = edgeVertices.I;
                int v2 = edgeVertices.J;

                // Compute the midpoint using linear interpolation
                Point3d midpoint = Interpolate(inputMesh.Vertices[v1], inputMesh.Vertices[v2], 0.5);
                edgePoints[(v1, v2)] = midpoint;
                edgePoints[(v2, v1)] = midpoint;  // Store bidirectionally to avoid recomputation
            }

            // 3. Compute new vertex positions (adjusted vertices)
            for (int i = 0; i < inputMesh.Vertices.Count; i++)
            {
                Point3d vertex = inputMesh.Vertices[i];

                // Find connected faces and edges
                var connectedFaces = inputMesh.TopologyVertices.ConnectedFaces(i);
                var connectedEdges = inputMesh.TopologyVertices.ConnectedEdges(i);

                // Compute the average of face centers
                Point3d faceAverage = new Point3d();
                foreach (int f in connectedFaces)
                    faceAverage += facePoints[f];
                faceAverage /= connectedFaces.Length;

                // Compute the average of edge midpoints
                Point3d edgeAverage = new Point3d();
                foreach (int e in connectedEdges)
                {
                    IndexPair edge = inputMesh.TopologyEdges.GetTopologyVertices(e);
                    Point3d midpoint = Interpolate(inputMesh.Vertices[edge.I], inputMesh.Vertices[edge.J], 0.5);
                    edgeAverage += midpoint;
                }
                edgeAverage /= connectedEdges.Length;

                // Compute the new vertex position
                Point3d newVertex = (faceAverage + 2 * edgeAverage + (connectedEdges.Length - 3) * vertex) /
                                    connectedEdges.Length;
                vertexPoints[i] = newVertex;
            }

            // 4. Build the new mesh with quad faces
            for (int i = 0; i < inputMesh.Faces.Count; i++)
            {
                MeshFace face = inputMesh.Faces[i];
                Point3d facePoint = facePoints[i];

                // Collect the points for each quad
                List<Point3d> quadPoints = new List<Point3d>();

                for (int j = 0; j < 4; j++)
                {
                    int v1 = face[j];
                    int v2 = face[(j + 1) % 4];

                    // Add the original vertex and the edge midpoint
                    quadPoints.Add(vertexPoints[v1]);
                    quadPoints.Add(edgePoints[(v1, v2)]);
                }

                // Add the new quad face to the mesh
                newMesh.Vertices.AddVertices(quadPoints);
                newMesh.Faces.AddFace(0, 1, 2, 3);
            }

            // Compute normals and optimize the mesh
            newMesh.Normals.ComputeNormals();
            newMesh.Compact();

            return newMesh;
        }

        // Linear interpolation between two points
        private static Point3d Interpolate(Point3d a, Point3d b, double t)
        {
            return new Point3d(
                a.X + t * (b.X - a.X),
                a.Y + t * (b.Y - a.Y),
                a.Z + t * (b.Z - a.Z)
            );
        }
    }
}
