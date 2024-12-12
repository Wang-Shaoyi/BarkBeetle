using Grasshopper.Kernel.Data;
using Grasshopper;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel.Types;
using Rhino.Collections;
using Rhino.Geometry.Intersect;
using Microsoft.VisualBasic;

namespace BarkBeetle.Utils
{
    internal class Unroll
    {
        # region unroll from curves
        public static void CreateRectangles1(
            List<Curve> curves,
            double width,
            double distance,
            double fontSize,
            List<GH_Curve> rectangles,
            List<GH_Curve> indicesTextOnCurve,
            List<GH_Curve> indicesTextOnPlane)
        {
            double yOffset = -width / 2;

            for (int i = 0; i < curves.Count; i++)
            {
                Curve curve = curves[i];
                double length = curve.GetLength();

                // 标注曲线名称 (曲线上的)
                TextEntity textEntityCurve = new TextEntity
                {
                    Plane = new Plane(curve.PointAtStart, new Vector3d(0, 0, 1)),
                    PlainText = "Crv_" + i,
                    TextHeight = fontSize,
                    Justification = TextJustification.Middle
                };

                foreach (Curve crv in textEntityCurve.Explode())
                {
                    indicesTextOnCurve.Add(new GH_Curve(crv));
                }

                // 创建拉直的矩形
                Line baseLine = new Line(new Point3d(0, yOffset, 0), new Point3d(length, yOffset, 0));
                Polyline rect = CreateRectangle(baseLine, width);
                rectangles.Add(new GH_Curve(new PolylineCurve(rect)));

                // 标注曲线名称 (平面上的)
                TextEntity textEntityPlane = new TextEntity
                {
                    Plane = new Plane(new Point3d(-distance, yOffset, 0), new Vector3d(0, 0, 1)),
                    PlainText = "Crv_" + i,
                    TextHeight = fontSize,
                    Justification = TextJustification.Right
                };

                foreach (Curve crv in textEntityPlane.Explode())
                {
                    indicesTextOnPlane.Add(new GH_Curve(crv));
                }

                yOffset -= distance + width; // 更新下一个矩形的间隔
            }
        }

        public static void ProcessIntersections1(
            List<Curve> curves,
            double tolerance,
            double width,
            double distance,
            double holeRadius,
            double fontSize,
            List<GH_Point> points,
            List<GH_Circle> holes,
            List<GH_Curve> indicesTextOnCurve,
            List<GH_Curve> indicesTextOnPlane)
        {
            int intersectionIndex = 0;

            for (int i = 0; i < curves.Count; i++)
            {
                Curve curveA = curves[i];

                for (int j = i + 1; j < curves.Count; j++)
                {
                    Curve curveB = curves[j];

                    var intersections = Rhino.Geometry.Intersect.Intersection.CurveCurve(curveA, curveB, tolerance, tolerance);

                    if (intersections != null)
                    {
                        foreach (var intersection in intersections)
                        {
                            Point3d pt = intersection.PointA;

                            // 曲线 A 的交点
                            double paramA = intersection.ParameterA;
                            double lengthA = curveA.GetLength(new Interval(curveA.Domain.Min, paramA));
                            Point3d pointA = new Point3d(lengthA, -width / 2 - i * (distance + width), 0);
                            Circle circleA = new Circle(pointA, holeRadius);
                            points.Add(new GH_Point(pointA));
                            holes.Add(new GH_Circle(circleA));

                            // 曲线 B 的交点
                            double paramB = intersection.ParameterB;
                            double lengthB = curveB.GetLength(new Interval(curveB.Domain.Min, paramB));
                            Point3d pointB = new Point3d(lengthB, -width / 2 - j * (distance + width), 0);
                            Circle circleB = new Circle(pointB, holeRadius);
                            points.Add(new GH_Point(pointB));
                            holes.Add(new GH_Circle(circleB));

                            // 标注点序号
                            TextEntity textEntityA = new TextEntity
                            {
                                Plane = new Plane(pointA + new Vector3d(holeRadius, 0, 0), new Vector3d(0, 0, 1)),
                                PlainText = intersectionIndex.ToString(),
                                TextHeight = fontSize,
                                Justification = TextJustification.Left
                            };

                            Curve[] textCurvesA = textEntityA.Explode();
                            foreach (Curve crv in textCurvesA)
                            {
                                indicesTextOnPlane.Add(new GH_Curve(crv));
                            }

                            TextEntity textEntityB = new TextEntity
                            {
                                Plane = new Plane(pointB + new Vector3d(holeRadius, 0, 0), new Vector3d(0, 0, 1)),
                                PlainText = intersectionIndex.ToString(),
                                TextHeight = fontSize,
                                Justification = TextJustification.Left
                            };

                            Curve[] textCurvesB = textEntityB.Explode();
                            foreach (Curve crv in textCurvesB)
                            {
                                indicesTextOnPlane.Add(new GH_Curve(crv));
                            }

                            TextEntity textEntityCurve = new TextEntity
                            {
                                Plane = new Plane(pt, new Vector3d(0, 0, 1)),
                                PlainText = intersectionIndex.ToString(),
                                TextHeight = fontSize,
                                Justification = TextJustification.Left
                            };

                            Curve[] textCurvesOnCurve = textEntityCurve.Explode();
                            foreach (Curve crv in textCurvesOnCurve)
                            {
                                indicesTextOnCurve.Add(new GH_Curve(crv));
                            }

                            intersectionIndex++;
                        }
                    }
                }
            }
        }

        public static void CreateRectanglesAndLabels2(
        List<Curve> curvesA, List<Curve> curvesB, double tolerance,
        double widthA, double widthB, double distance, double holeRadius, double fontSize,
        List<GH_Curve> rectanglesOnA, List<GH_Curve> rectanglesOnB,
        List<GH_Point> pointsOnA, List<GH_Point> pointsOnB,
        List<GH_Circle> holesOnA, List<GH_Circle> holesOnB,
        List<GH_Curve> labelsOnA, List<GH_Curve> labelsOnB,
        List<GH_Curve> originLabels)
        {
            int intersectionIndex = 0;
            double yOffsetA = -widthA / 2;

            for (int i = 0; i < curvesA.Count; i++)
            {
                Curve curveA = curvesA[i];
                double lengthA = curveA.GetLength();

                //标号 -- 曲面
                TextEntity textEntityCurveA = new TextEntity
                {
                    Plane = new Plane(curveA.PointAtStart, new Vector3d(0, 0, 1)),
                    PlainText = "Crv_A" + i.ToString(),
                    TextHeight = fontSize,
                    Justification = TextJustification.Middle
                };

                Curve[] textCurvesOnCurveA = textEntityCurveA.Explode();
                foreach (Curve crv in textCurvesOnCurveA)
                {
                    originLabels.Add(new GH_Curve(crv));
                }

                // 创建拉直的矩形长条（A组）
                Line baseLineA = new Line(new Point3d(0, yOffsetA, 0), new Point3d(lengthA, yOffsetA, 0));
                Polyline rectA = CreateRectangle(baseLineA, widthA);
                rectanglesOnA.Add(new GH_Curve(rectA.ToNurbsCurve()));

                //标号 -- 平面
                TextEntity textEntityCurvePlaneA = new TextEntity
                {
                    Plane = new Plane(new Point3d(-distance, yOffsetA, 0), new Vector3d(0, 0, 1)),
                    PlainText = "Crv_A" + i.ToString(),
                    TextHeight = fontSize,
                    Justification = TextJustification.Right
                };

                Curve[] textCurvesOnCurvePlaneA = textEntityCurvePlaneA.Explode();
                foreach (Curve crv in textCurvesOnCurvePlaneA)
                {
                    labelsOnA.Add(new GH_Curve(crv));
                }

                // 处理相交
                double yOffsetB = widthB / 2 + distance;

                for (int j = 0; j < curvesB.Count; j++)
                {
                    Curve curveB = curvesB[j];

                    if (i == 0)
                    {
                        double lengthB = curveB.GetLength();

                        // 在原来曲线上标号
                        TextEntity textEntityCurveB = new TextEntity
                        {
                            Plane = new Plane(curveB.PointAtStart, new Vector3d(0, 0, 1)),
                            PlainText = "Crv_B" + j.ToString(),
                            TextHeight = fontSize,
                            Justification = TextJustification.Middle
                        };

                        Curve[] textCurvesOnCurveB = textEntityCurveB.Explode();
                        foreach (Curve crv in textCurvesOnCurveB)
                        {
                            originLabels.Add(new GH_Curve(crv));
                        }

                        // 创建拉直的矩形长条（B组）
                        Line baseLineB = new Line(new Point3d(0, yOffsetB, 0), new Point3d(lengthB, yOffsetB, 0));
                        Polyline rectB = CreateRectangle(baseLineB, widthB);
                        rectanglesOnB.Add(new GH_Curve(rectB.ToNurbsCurve()));

                        //标号 -- 平面
                        TextEntity textEntityCurvePlaneB = new TextEntity
                        {
                            Plane = new Plane(new Point3d(-distance, yOffsetB, 0), new Vector3d(0, 0, 1)),
                            PlainText = "Crv_B" + j.ToString(),
                            TextHeight = fontSize,
                            Justification = TextJustification.Right
                        };

                        Curve[] textCurvesOnCurvePlaneB = textEntityCurvePlaneB.Explode();
                        foreach (Curve crv in textCurvesOnCurvePlaneB)
                        {
                            labelsOnB.Add(new GH_Curve(crv));
                        }
                    }

                    var intersections = Rhino.Geometry.Intersect.Intersection.CurveCurve(curveA, curveB, tolerance, tolerance);

                    if (intersections != null)
                    {
                        foreach (var intersection in intersections)
                        {
                            Point3d pt = intersection.PointA;

                            TextEntity textEntityOriginal = new TextEntity
                            {
                                Plane = new Plane(pt + new Vector3d(holeRadius, 0, 0), new Vector3d(0, 0, 1)),
                                PlainText = intersectionIndex.ToString(),
                                TextHeight = fontSize,
                                Justification = TextJustification.Left // 左对齐
                            };

                            Curve[] textCurvesOriginal = textEntityOriginal.Explode();
                            foreach (Curve crv in textCurvesOriginal)
                            {
                                originLabels.Add(new GH_Curve(crv));
                            }

                            // 获取在曲线A上的参数和长度
                            double paramA = intersection.ParameterA;
                            Interval domainA = new Interval(curveA.Domain.Min, paramA);
                            double lengthAOnCurve = curveA.GetLength(domainA);

                            // 获取在曲线B上的参数和长度
                            double paramB = intersection.ParameterB;
                            Interval domainB = new Interval(curveB.Domain.Min, paramB);
                            double lengthBOnCurve = curveB.GetLength(domainB);

                            // 在矩形长条上生成点
                            Point3d pointA = new Point3d(lengthAOnCurve, yOffsetA, 0);
                            Circle circleA = new Circle(new Plane(pointA, new Vector3d(0,0,1)), holeRadius);
                            Point3d pointB = new Point3d(lengthBOnCurve, yOffsetB, 0);
                            Circle circleB = new Circle(new Plane(pointB, new Vector3d(0, 0, 1)), holeRadius);

                            // 添加点到结果
                            pointsOnA.Add(new GH_Point(pointA));
                            pointsOnB.Add(new GH_Point(pointB));
                            holesOnA.Add(new GH_Circle(circleA));
                            holesOnB.Add(new GH_Circle(circleB));

                            // 添加序号为Text
                            TextEntity textEntityA = new TextEntity
                            {
                                Plane = new Plane(pointA + new Vector3d(holeRadius, 0, 0), new Vector3d(0, 0, 1)),
                                PlainText = intersectionIndex.ToString(),
                                TextHeight = fontSize,
                                Justification = TextJustification.Left // 左对齐
                            };

                            TextEntity textEntityB = new TextEntity
                            {
                                Plane = new Plane(pointB + new Vector3d(holeRadius, 0, 0), new Vector3d(0, 0, 1)),
                                PlainText = intersectionIndex.ToString(),
                                TextHeight = fontSize,
                                Justification = TextJustification.Left // 左对齐
                            };

                            Curve[] textCurvesA = textEntityA.Explode();
                            foreach (Curve crv in textCurvesA)
                            {
                                labelsOnA.Add(new GH_Curve(crv));
                            }

                            Curve[] textCurvesB = textEntityB.Explode();
                            foreach (Curve crv in textCurvesB)
                            {
                                labelsOnB.Add(new GH_Curve(crv));
                            }

                            intersectionIndex++;
                        }
                    }
                    yOffsetB += distance + widthB;
                }
                yOffsetA -= distance + widthA;// 更新A组下一个矩形的间隔
            }
        }

        private static Polyline CreateRectangle(Line baseLine, double width)
        {
            double halfWidth = width / 2.0;

            Point3d pt1 = baseLine.From + new Vector3d(0, -halfWidth, 0);
            Point3d pt2 = baseLine.To + new Vector3d(0, -halfWidth, 0);
            Point3d pt3 = baseLine.To + new Vector3d(0, halfWidth, 0);
            Point3d pt4 = baseLine.From + new Vector3d(0, halfWidth, 0);

            return new Polyline(new List<Point3d> { pt1, pt2, pt3, pt4, pt1 });
        }
        #endregion

        #region unroll surface
        public static void UnrollIntersectSurfacesAndLabeling(
            List<Curve> curves, List<Surface> surfaces, double tolerance,
            double distance, double holeRadius, double fontSize, out List<GH_Curve> stripBoundaries,
            out List<GH_Point> points, out List<GH_Circle> holes, out List<GH_Curve> indicesTextOnCurve, out List<GH_Curve> indicesTextOnPlane)
        {
            // Initialize
            stripBoundaries = new List<GH_Curve>();
            points = new List<GH_Point>();
            holes = new List<GH_Circle> ();
            indicesTextOnCurve = new List<GH_Curve>();
            indicesTextOnPlane = new List<GH_Curve>();

            // Create a List<List> for storing all intersection points. Each sublist refers to a curve.
            List<List<Point3d>> intersectionPts = new List<List<Point3d>>();
            List<List<double>> allLengths = new List<List<double>>();
            List<List<int>> intersectionIndexes = new List<List<int>>(); // to store the index for each intersection point
            int intersectionIndex = -1;
            for (int i=0; i<curves.Count; i++)
            {
                intersectionPts.Add(new List<Point3d>());
                allLengths.Add(new List<double> ());
                intersectionIndexes.Add(new List<int>());
            }

            List<Point3d> intersectionPointsOnSurface = new List<Point3d>();
            List<int> intersectionIndexOnSurface = new List<int>();
            int storeInt = -1;
            // Go through all intersections, create labels on curves, and fill the two lists above
            for (int i = 0; i < curves.Count; i++)
            {
                Curve curveA = curves[i];

                for (int j = i + 1; j < curves.Count; j++)
                {
                    Curve curveB = curves[j];

                    var intersections = Rhino.Geometry.Intersect.Intersection.CurveCurve(curveA, curveB, tolerance, tolerance);

                    if (intersections != null)
                    {
                        foreach (var intersection in intersections)
                        {
                            Point3d ptA = intersection.PointA;
                            bool check = PointDataUtils.IsPointNearList(ptA, intersectionPointsOnSurface, tolerance, out int indexAll);
                            // Process for labels on curve
                            if (!check)
                            {
                                intersectionIndex += 1;
                                intersectionPointsOnSurface.Add(ptA);
                                intersectionIndexOnSurface.Add(intersectionIndex);
                                // Add labels on curve
                                TextEntity textEntityCurve = new TextEntity
                                {
                                    Plane = new Plane(ptA, new Vector3d(0, 0, 1)),
                                    PlainText = intersectionIndex.ToString(),
                                    TextHeight = fontSize,
                                    Justification = TextJustification.Center
                                };

                                Curve[] textCurvesOnCurve = textEntityCurve.Explode();
                                foreach (Curve crv in textCurvesOnCurve)
                                {
                                    indicesTextOnCurve.Add(new GH_Curve(crv));
                                }
                            }
                            else
                            {
                                storeInt = intersectionIndex;
                                intersectionIndex = intersectionIndexOnSurface[indexAll];
                            }

                            // Process for A
                            if (!PointDataUtils.IsPointNearList(ptA, intersectionPts[i], tolerance, out int indexA))
                            {
                                intersectionPts[i].Add(ptA);
                                double lengthA = curveA.GetLength(new Interval(curveA.Domain.Min, intersection.ParameterA));
                                allLengths[i].Add(lengthA);
                                intersectionIndexes[i].Add(intersectionIndex);
                            }

                            // Process for B
                            Point3d ptB = intersection.PointB;
                            if (!PointDataUtils.IsPointNearList(ptB, intersectionPts[j], tolerance, out int indexB))
                            {
                                intersectionPts[j].Add(ptB);
                                double lengthB = curveB.GetLength(new Interval(curveB.Domain.Min, intersection.ParameterB));
                                allLengths[j].Add(lengthB);
                                intersectionIndexes[j].Add(intersectionIndex);
                            }

                            if (check)
                            {
                                intersectionIndex = storeInt;
                            }
                        }
                    }
                }
            }

            double yOffset = 0;
            // Go through all surfaces to unroll and label
            for (int i = 0; i < surfaces.Count; i++)
            {
                Surface surface = surfaces[i];
                Curve curve = curves[i];
                List<Point3d> pointsX = intersectionPts[i];
                List<int> indexX = intersectionIndexes[i];
                List<double> lengths = allLengths[i];

                // Label on curve
                TextEntity textEntityCurve = new TextEntity
                {
                    Plane = new Plane(curve.PointAtStart, new Vector3d(0, 0, 1)),
                    PlainText = "Crv_" + i,
                    TextHeight = fontSize,
                    Justification = TextJustification.Middle
                };
                foreach (Curve crv in textEntityCurve.Explode()) indicesTextOnCurve.Add(new GH_Curve(crv));

                //unroll surface
                Surface unrolledSrf = BrepUtils.UnrollSurfaceWithCurve(surface, curve, pointsX, out Curve unrolledCurve, out List<Point3d> unrolledPointsX);

                // Get max Y to organize the layout
                BoundingBox bbox = unrolledSrf.GetBoundingBox(true);
                double width = bbox.Max.X - bbox.Min.X;

                yOffset += distance + width;

                // Get and locate unrolled boundary
                Transform rotation = Transform.Rotation(- Math.PI / 2, Vector3d.ZAxis, Point3d.Origin);
                Transform translation = Transform.Translation(- yOffset, 0, 0);
                Transform combinedTransform = rotation * translation;

                //get boundary
                Brep brepSurface = unrolledSrf.ToBrep();
                Brep rotatedBrep = brepSurface.DuplicateBrep();
                rotatedBrep.Transform(combinedTransform);

                Curve[] boundaryCrv = rotatedBrep.DuplicateEdgeCurves();
                foreach (Curve crv in boundaryCrv)
                {
                    stripBoundaries.Add(new GH_Curve(crv));
                }

                //go through all points, label them, and give them holes
                for (int j = 0; j < lengths.Count; j++)
                {
                    // Also move points
                    //Point3d ptX = unrolledPointsX[j];
                    Point3d ptX = unrolledCurve.PointAtLength(lengths[j]);
                    ptX.Transform(combinedTransform);
                    int currentId = indexX[j];
                    // Label
                    TextEntity textEntity = new TextEntity
                    {
                        Plane = new Plane(ptX + new Vector3d(holeRadius, 0, 0), new Vector3d(0, 0, 1)),
                        PlainText = currentId.ToString(),
                        TextHeight = fontSize,
                        Justification = TextJustification.Left
                    };
                    Curve[] textCurves = textEntity.Explode();
                    foreach (Curve crv in textCurves) indicesTextOnPlane.Add(new GH_Curve(crv));

                    Circle circle = new Circle(ptX, holeRadius);
                    points.Add(new GH_Point(ptX));
                    holes.Add(new GH_Circle(circle));

                }

                // Label the curves
                TextEntity textEntityPlane = new TextEntity
                {
                    Plane = new Plane(new Point3d(distance, yOffset - distance, 0), new Vector3d(0, 0, 1)),
                    PlainText = "Crv_" + i,
                    TextHeight = fontSize,
                    Justification = TextJustification.Left
                };

                foreach (Curve crv in textEntityPlane.Explode())
                {
                    indicesTextOnPlane.Add(new GH_Curve(crv));
                }
            }
        }

        public static void UnrollSurfacesAndLabelingWithPoints(
            List<Curve> curves, List<Surface> surfaces, double tolerance, List<List<Point3d>> intersectionPts, 
            double distance, double holeRadius, double fontSize, out List<GH_Curve> stripBoundaries, 
            out List<GH_Point> points, out List<GH_Circle> holes, out List<GH_Curve> indicesTextOnCurve, out List<GH_Curve> indicesTextOnPlane,
            List<List<int>> intersectionIndexes = null)
        {
            // Initialize
            stripBoundaries = new List<GH_Curve>();
            points = new List<GH_Point>();
            holes = new List<GH_Circle>();
            indicesTextOnCurve = new List<GH_Curve>();
            indicesTextOnPlane = new List<GH_Curve>();

            double yOffset = 0;
            // Go through all surfaces to unroll and label
            for (int i = 0; i < surfaces.Count; i++)
            {
                Surface surface = surfaces[i];
                Curve curve = curves[i];
                List<Point3d> pointsX = intersectionPts[i];
                List<int> indexX = new List<int>();

                if (intersectionIndexes != null)
                {
                    indexX = intersectionIndexes[i];
                }

                List<double> lengths = new List<double>();
                foreach (Point3d point in pointsX)
                {
                    curve.ClosestPoint(point, out double t);
                    double length = curve.GetLength(new Interval(curve.Domain.Min, t));
                    lengths.Add(length);
                }

                // Label on curve
                TextEntity textEntityCurve = new TextEntity
                {
                    Plane = new Plane(curve.PointAtStart, new Vector3d(0, 0, 1)),
                    PlainText = "Crv_" + i,
                    TextHeight = fontSize,
                    Justification = TextJustification.Middle
                };
                foreach (Curve crv in textEntityCurve.Explode()) indicesTextOnCurve.Add(new GH_Curve(crv));

                //unroll surface
                Surface unrolledSrf = BrepUtils.UnrollSurfaceWithCurve(surface, curve, pointsX, out Curve unrolledCurve, out List<Point3d> unrolledPointsX);

                // Get max Y to organize the layout
                BoundingBox bbox = unrolledSrf.GetBoundingBox(true);
                double width = bbox.Max.X - bbox.Min.X;

                yOffset += distance + width;

                // Get and locate unrolled boundary
                Transform rotation = Transform.Rotation(-Math.PI / 2, Vector3d.ZAxis, Point3d.Origin);
                Transform translation = Transform.Translation(-yOffset, 0, 0);
                Transform combinedTransform = rotation * translation;

                //get boundary
                Brep brepSurface = unrolledSrf.ToBrep();
                Brep rotatedBrep = brepSurface.DuplicateBrep();
                rotatedBrep.Transform(combinedTransform);

                Curve[] boundaryCrv = rotatedBrep.DuplicateEdgeCurves();
                foreach (Curve crv in boundaryCrv)
                {
                    stripBoundaries.Add(new GH_Curve(crv));
                }

                //go through all points, label them, and give them holes
                for (int j = 0; j < pointsX.Count; j++)
                {
                    // Also move points
                    Point3d ptX = pointsX[j];
                    Point3d ptPlane = unrolledCurve.PointAtLength(lengths[j]);
                    ptPlane.Transform(combinedTransform);
                    points.Add(new GH_Point(ptPlane));

                    if (intersectionIndexes != null)
                    {
                        int currentId = indexX[j];

                        // Label
                        TextEntity textEntity = new TextEntity
                        {
                            Plane = new Plane(ptPlane + new Vector3d(holeRadius, 0, 0), new Vector3d(0, 0, 1)),
                            PlainText = currentId.ToString(),
                            TextHeight = fontSize,
                            Justification = TextJustification.Left
                        };
                        Curve[] textCurves = textEntity.Explode();

                        foreach (Curve crv in textCurves) indicesTextOnPlane.Add(new GH_Curve(crv));
                    }
                        
                    Circle circle = new Circle(ptPlane, holeRadius);
                    holes.Add(new GH_Circle(circle));
                }

                // Label the curves
                TextEntity textEntityPlane = new TextEntity
                {
                    Plane = new Plane(new Point3d(distance, yOffset - distance, 0), new Vector3d(0, 0, 1)),
                    PlainText = "Crv_" + i,
                    TextHeight = fontSize,
                    Justification = TextJustification.Left
                };

                foreach (Curve crv in textEntityPlane.Explode())
                {
                    indicesTextOnPlane.Add(new GH_Curve(crv));
                }
            }
        }

        #endregion
    }
}

