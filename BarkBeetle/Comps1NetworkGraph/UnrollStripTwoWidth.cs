using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using BarkBeetle.Utils;

namespace BarkBeetle.Comps1NetworkGraph
{
    public class UnrollStripTwoWidth : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the UnrollStripTwoWidth class.
        /// </summary>
        public UnrollStripTwoWidth()
          : base("Unroll Strip Two Width", "UnrollStripTwoWidth",
              "Description",
              "BarkBeetle", "1-Network")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves A", "A", "The first set of input curves.", GH_ParamAccess.list);
            pManager.AddCurveParameter("Curves B", "B", "The second set of input curves.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Tolerance", "T", "The intersection tolerance.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Width A", "WA", "The width of the rectangles for curves A.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Width B", "WB", "The width of the rectangles for curves B.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Distance", "D", "The distance between the rectangles.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Hole Radius", "HR", "The radius of the holes.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Font Size", "FS", "The font size for the labels.", GH_ParamAccess.item);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Rectangles A", "RA", "The rectangles created for curves A.", GH_ParamAccess.list);
            pManager.AddCurveParameter("Rectangles B", "RB", "The rectangles created for curves B.", GH_ParamAccess.list);
            pManager.AddPointParameter("Points A", "PA", "Intersection points on curves A.", GH_ParamAccess.list);
            pManager.AddPointParameter("Points B", "PB", "Intersection points on curves B.", GH_ParamAccess.list);
            pManager.AddCircleParameter("Holes A", "HA", "Intersection holes on rectangles for curves A.", GH_ParamAccess.list);
            pManager.AddCircleParameter("Holes B", "HB", "Intersection holes on rectangles for curves B.", GH_ParamAccess.list);
            pManager.AddCurveParameter("Labels A", "LA", "Labels for rectangles and points on curves A.", GH_ParamAccess.list);
            pManager.AddCurveParameter("Labels B", "LB", "Labels for rectangles and points on curves B.", GH_ParamAccess.list);
            pManager.AddCurveParameter("Original Labels", "OL", "Labels on the original curves.", GH_ParamAccess.list);
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
                return null;
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