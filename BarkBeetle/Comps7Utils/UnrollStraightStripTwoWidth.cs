using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using BarkBeetle.Utils;

namespace BarkBeetle.Comps7Utils
{
    public class UnrollStraightStripTwoWidth : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the UnrollStripTwoWidth class.
        /// </summary>
        public UnrollStraightStripTwoWidth()
          : base("Unroll Straight UV Strip (different width)", "Unroll UV Strip",
              "Unroll and label straight (geodesic) strips in two directions",
              "BarkBeetle", "7-Utils")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves A", "Curves A", "The first set of input curves.", GH_ParamAccess.list);
            pManager.AddCurveParameter("Curves B", "Curves B", "The second set of input curves.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Tolerance", "Tolerance", "The intersection tolerance.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Width A", "Width A", "The width of the rectangles for curves A.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Width B", "Width B", "The width of the rectangles for curves B.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Distance", "Distance", "The distance between the rectangles.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Hole Radius", "Hole Radius", "The radius of the holes.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Font Size", "Font Size", "The font size for the labels.", GH_ParamAccess.item);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Strips A", "Strips A", "The strips created for curves A.", GH_ParamAccess.list);
            pManager.AddCurveParameter("Strips B", "Strips B", "The strips created for curves B.", GH_ParamAccess.list);
            pManager.AddPointParameter("Points A", "Points A", "Intersection points on curves A.", GH_ParamAccess.list);
            pManager.AddPointParameter("Points B", "Points B", "Intersection points on curves B.", GH_ParamAccess.list);
            pManager.AddCircleParameter("Holes A", "Holes A", "Intersection holes on strips for curves A.", GH_ParamAccess.list);
            pManager.AddCircleParameter("Holes B", "Holes B", "Intersection holes on strips for curves B.", GH_ParamAccess.list);
            pManager.AddCurveParameter("Labels A", "Labels A", "Labels for strips and points on curves A.", GH_ParamAccess.list);
            pManager.AddCurveParameter("Labels B", "Labels B", "Labels for strips and points on curves B.", GH_ParamAccess.list);
            pManager.AddCurveParameter("Original Labels", "Original Labels", "Labels on the original curves.", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 输入数据
            var curvesA = new List<Curve>();
            var curvesB = new List<Curve>();
            double tolerance = 0, widthA = 0, widthB = 0, distance = 0, holeRadius = 0, fontSize = 0;

            if (!DA.GetDataList(0, curvesA)) return;
            if (!DA.GetDataList(1, curvesB)) return;
            if (!DA.GetData(2, ref tolerance)) return;
            if (!DA.GetData(3, ref widthA)) return;
            if (!DA.GetData(4, ref widthB)) return;
            if (!DA.GetData(5, ref distance)) return;
            if (!DA.GetData(6, ref holeRadius)) return;
            if (!DA.GetData(7, ref fontSize)) return;

            // 初始化结果容器
            var rectanglesOnA = new List<GH_Curve>();
            var rectanglesOnB = new List<GH_Curve>();
            var pointsOnA = new List<GH_Point>();
            var pointsOnB = new List<GH_Point>();
            var holesOnA = new List<GH_Circle>();
            var holesOnB = new List<GH_Circle>();
            var labelsOnA = new List<GH_Curve>();
            var labelsOnB = new List<GH_Curve>();
            var intersectionLabels = new List<GH_Curve>();

            // 计算偏移
            double yOffsetA = -widthA / 2;
            double yOffsetB = widthB / 2 + distance;

            // 调用静态方法处理逻辑
            Unroll.CreateRectanglesAndLabels2(
                curvesA, curvesB, tolerance, widthA, widthB, distance,
                holeRadius, fontSize,
                rectanglesOnA, rectanglesOnB, pointsOnA, pointsOnB,
                holesOnA, holesOnB, labelsOnA, labelsOnB, intersectionLabels);

            // 设置输出
            DA.SetDataList(0, rectanglesOnA);
            DA.SetDataList(1, rectanglesOnB);
            DA.SetDataList(2, pointsOnA);
            DA.SetDataList(3, pointsOnB);
            DA.SetDataList(4, holesOnA);
            DA.SetDataList(5, holesOnB);
            DA.SetDataList(6, labelsOnA);
            DA.SetDataList(7, labelsOnB);
            DA.SetDataList(8, intersectionLabels);
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Resources.UnrollUVStrip;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("8A1A4FAA-190E-4B72-B6CD-3033B73D20B3"); }
        }
    }
}