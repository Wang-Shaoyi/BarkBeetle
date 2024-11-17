using Grasshopper.Kernel.Data;
using Grasshopper;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel.Types;

namespace BarkBeetle.Utils
{
    internal class Unroll
    {
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
    }
}

