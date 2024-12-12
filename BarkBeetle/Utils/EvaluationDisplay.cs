using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Rhino.Geometry;
using Grasshopper.Kernel;
using Rhino;
using BarkBeetle.ToolpathStackSetting;
using System.Drawing.Imaging;
using System.Security.Cryptography;


namespace BarkBeetle.Utils
{

    internal class EvaluationDisplay
    {
        /// <summary>
        /// Evaluating curvature and twisitng
        /// </summary>
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
        /// Evaluating overhang
        /// </summary>
        public void EvaluateDiscontinueAngles(ToolpathStack toolpathStack, int displayThickness, out List<Curve> allSegments, out List<double> allAngles, out List<Color> legendColors, out List<double> legendTags)
        {
            List<Curve> layerCurves = toolpathStack.LayerCurves.Select(ghCrv => ghCrv.Value).ToList();

            Curve topCrv = layerCurves[layerCurves.Count - 1];

            List<Point3d> topDiscontinuePoints = CurveUtils.GetDiscontinuityPoints(topCrv, out List<Curve> segments);

            // Step 1: get all points
            Point3d[,] pointArray = new Point3d[layerCurves.Count, topDiscontinuePoints.Count];
            for (int i = 0; i < layerCurves.Count; i++)
            {
                Curve currentCrv = layerCurves[i];
                for (int j = 0; j < topDiscontinuePoints.Count; j++)
                {
                    currentCrv.ClosestPoint(topDiscontinuePoints[j], out double t);
                    pointArray[i, j] = currentCrv.PointAt(t);
                }
            }

            // Step 2: calculate angles
            allSegments = new List<Curve>();
            allAngles = new List<double>();
            for (int j = 0; j < topDiscontinuePoints.Count; j++)
            {
                for (int i = 0; i < layerCurves.Count - 1; i++)
                {
                    Line newLine = new Line(pointArray[i, j], pointArray[i + 1, j]);
                    allSegments.Add(newLine.ToNurbsCurve());
                    Vector3d newVector = pointArray[i + 1, j] - pointArray[i, j];
                    double angle = Vector3d.VectorAngle(newVector, Vector3d.ZAxis);
                    allAngles.Add(Math.Round(angle,2));
                }
            }

            legendColors = new List<Color>();
            legendTags = new List<double>();
            GenerateLegend(allAngles.Min(), allAngles.Max(), legendColors, legendTags, 10);

        }

        /// <summary>
        /// Below is color related
        /// </summary>
        // generate the legend
        public void GenerateLegend(double minValue, double maxValue, List<Color> legendColors, List<double> legendTags, int legendSteps)
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

        public Color[] CreateColormap()
        {
            // This is the sunset colormap, friendly for color blind people
            return new Color[]
            {
                Color.FromArgb(54, 75, 154),
                Color.FromArgb(74, 123, 183),
                Color.FromArgb(110, 166, 205),
                Color.FromArgb(152, 202, 225),
                Color.FromArgb(194, 228, 239),
                Color.FromArgb(234, 236, 204),
                Color.FromArgb(254, 218, 139),
                Color.FromArgb(253, 179, 102),
                Color.FromArgb(246, 126, 75),
                Color.FromArgb(221, 61, 45),
                Color.FromArgb(165, 0, 38)
            };
        }

        public Color MapToColor(double t, Color[] colormap)
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
