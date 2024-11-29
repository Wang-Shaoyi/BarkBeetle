using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Rhino.Geometry;
using Grasshopper.Kernel;
using Rhino;

namespace BarkBeetle.Utils
{

    internal class CurvatureDisplay
    {
        public void DisplayCurvature(List<Surface> surfaces, int density, int type, int outputUnit, out List<Mesh> meshes, out List<Color> legendColors, out List<double> legendTags)
        {
            //Initialize
            meshes = new List<Mesh>();
            legendColors = new List<Color>();
            legendTags = new List<double>();

            // Calculate unit scale based on outputUnit
            double scale = CalculateUnitScale(outputUnit);

            // Store all curvature values
            List<List<double>> allCurvatures = new List<List<double>>();

            // Color map setup
            Color[] colormap = CreateColormap();
            double minCurvature = double.MaxValue;
            double maxCurvature = double.MinValue;

            // Process each surface
            foreach (var surface in surfaces)
            {
                // Step 1: Convert surface to mesh
                Mesh mesh = Mesh.CreateFromSurface(surface, new MeshingParameters(density));

                // Step 2: Compute curvature
                List<double> curvatures = new List<double>();
                for (int i = 0; i < mesh.Vertices.Count; i++)
                {
                    Point3d pt = mesh.Vertices[i];
                    surface.ClosestPoint(pt, out double u, out double v);

                    double curvature = 0;
                    switch (type)
                    {
                        case 0:
                            curvature = surface.CurvatureAt(u, v).Mean / scale;
                            break;
                        case 1:
                            curvature = surface.CurvatureAt(u, v).Gaussian / scale;
                            break;
                        case 2:
                            curvature = surface.CurvatureAt(u, v).Kappa(0) / scale;
                            break;
                        case 3:
                            curvature = surface.CurvatureAt(u, v).Kappa(1) / scale;
                            break;
                        default:
                            curvature = surface.CurvatureAt(u, v).Mean / scale;
                            break;
                    }
                    curvatures.Add(curvature);

                    // Update min/max curvature
                    if (curvature < minCurvature) minCurvature = curvature;
                    if (curvature > maxCurvature) maxCurvature = curvature;
                }

                // Add the mesh and curvatures to output
                meshes.Add(mesh);
                allCurvatures.Add(curvatures);
            }

            for (int j = 0; j < meshes.Count; j++)
            { 
                Mesh mesh = meshes[j];
                List<double> curvatures = allCurvatures[j];

                // Step 3: Assign colors to mesh vertices
                for (int i = 0; i < mesh.Vertices.Count; i++)
                {
                    double normalized = (curvatures[i] - minCurvature) / (maxCurvature - minCurvature);
                    normalized = Math.Max(0, Math.Min(1, normalized)); // Clamp to [0, 1]
                    Color color = MapToColor(normalized, colormap);
                    mesh.VertexColors.Add(color);
                }
            }

            // Step 4: Generate legend colors and tags
            GenerateLegend(minCurvature,maxCurvature, legendColors, legendTags,10);
        }

        public void DisplayTwist(List<Surface> surfaces,List<Curve> curves,int meshDensity, int outputUnit, out List<Mesh> meshes, out List<Color> legendColors, out List<double> legendTags)
        {
            //Initialize
            meshes = new List<Mesh>();
            legendColors = new List<Color>();
            legendTags = new List<double>();

            // Calculate unit scale based on outputUnit
            double scale = CalculateUnitScale(outputUnit);

            // Store all twist values for global normalization
            List<List<double>> allTwistValues = new List<List<double>>();

            // Color map setup
            Color[] colormap = CreateColormap();
            double minTwistValue = double.MaxValue;
            double maxTwistValue = double.MinValue;

            // Process each surface-curve pair
            for (int i = 0; i < surfaces.Count; i++)
            {
                Surface surface = surfaces[i];
                Curve curve = curves[i];

                // Step 1: Convert surface to mesh
                Mesh mesh = Mesh.CreateFromSurface(surface, new MeshingParameters(meshDensity));

                // Step 2: Compute twist values along the curve
                List<double> twistValues = new List<double>();

                for (int j = 0; j < mesh.Vertices.Count; j++)
                {
                    Point3d pt = mesh.Vertices[j];
                    curve.ClosestPoint(pt, out double t);
                    double twistValue = ComputeTwistAt(surface, curve, t, scale);
                    twistValues.Add(twistValue);

                    // Update min/max curvature
                    if (twistValue < minTwistValue) minTwistValue = twistValue;
                    if (twistValue > maxTwistValue) maxTwistValue = twistValue;
                }

                // Add the mesh and curvatures to output
                meshes.Add(mesh);
                allTwistValues.Add(twistValues);
            }

            for (int j = 0; j < meshes.Count; j++)
            {
                Mesh mesh = meshes[j];
                List<double> twists = allTwistValues[j];

                // Step 3: Assign colors to mesh vertices
                for (int i = 0; i < mesh.Vertices.Count; i++)
                {
                    double normalized = (twists[i] - minTwistValue) / (maxTwistValue - minTwistValue);
                    normalized = Math.Max(0, Math.Min(1, normalized)); // Clamp to [0, 1]
                    Color color = MapToColor(normalized, colormap);
                    mesh.VertexColors.Add(color);
                }
            }

            // Step 4: Generate legend colors and tags
            GenerateLegend(minTwistValue, maxTwistValue, legendColors, legendTags, 10);
        }

        private double CalculateUnitScale(int outputUnit)
        {
            Rhino.RhinoDoc doc = Rhino.RhinoDoc.ActiveDoc;
            Rhino.UnitSystem unitSystem = doc.ModelUnitSystem;

            switch (outputUnit)
            {
                case 0:
                    return Rhino.RhinoMath.UnitScale(unitSystem, Rhino.UnitSystem.Meters);
                case 1:
                    return Rhino.RhinoMath.UnitScale(unitSystem, Rhino.UnitSystem.Centimeters);
                case 2:
                    return Rhino.RhinoMath.UnitScale(unitSystem, Rhino.UnitSystem.Millimeters);
                default:
                    return 1;
            }
        }

        private double ComputeTwistAt(Surface surface, Curve curve, double t, double scale)
        {
            Point3d pointOnCurve = curve.PointAt(t);
            surface.ClosestPoint(pointOnCurve, out double u, out double v);
            Vector3d normalAtPoint = surface.NormalAt(u, v);

            double smallStep = 0.01;
            double tNext = t + smallStep;
            if (tNext >= curve.Domain.Max)
            {
                tNext = t - smallStep;
            }
            Point3d nextPointOnCurve = curve.PointAt(tNext);
            surface.ClosestPoint(nextPointOnCurve, out u, out v);
            Vector3d normalAtNextPoint = surface.NormalAt(u, v);

            double angleRadians = Vector3d.VectorAngle(normalAtPoint, normalAtNextPoint);
            double segmentLength = pointOnCurve.DistanceTo(nextPointOnCurve) * scale;

            return angleRadians / segmentLength;
        }

        /// <summary>
        /// Below is color related
        /// </summary>
        // generate the legend
        private void GenerateLegend(double minValue, double maxValue, List<Color> legendColors, List<double> legendTags, int legendSteps)
        {
            double step = (maxValue - minValue) / (legendSteps - 1);
            Color[] colormap = CreateColormap();

            for (int i = 0; i < legendSteps; i++)
            {
                double value = minValue + i * step;
                legendTags.Add(value);
                double normalized = (value - minValue) / (maxValue - minValue);
                legendColors.Add(MapToColor(normalized, colormap));
            }
        }

        private Color[] CreateColormap()
        {
            return new Color[]
            {
                Color.Blue,
                Color.Cyan,
                Color.Green,
                Color.Yellow,
                Color.Orange,
                Color.Red
            };
        }

        private Color MapToColor(double t, Color[] colormap)
        {
            // Clamp t to the valid range [0, 1]
            t = Math.Max(0, Math.Min(1, t));

            // Calculate index and blend factor
            int index = (int)Math.Floor(t * (colormap.Length - 1));
            double blend = t * (colormap.Length - 1) - index;

            // Ensure index is within bounds
            Color c1 = colormap[index];
            Color c2 = colormap[Math.Min(index + 1, colormap.Length - 1)];

            // Safely calculate RGB values
            int r = Math.Max(0, Math.Min(255, (int)(c1.R * (1 - blend) + c2.R * blend)));
            int g = Math.Max(0, Math.Min(255, (int)(c1.G * (1 - blend) + c2.G * blend)));
            int b = Math.Max(0, Math.Min(255, (int)(c1.B * (1 - blend) + c2.B * blend)));

            return Color.FromArgb(r, g, b);
        }
    }
}
