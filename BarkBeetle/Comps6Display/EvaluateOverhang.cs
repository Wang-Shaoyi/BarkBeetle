using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using Grasshopper.Kernel.Data;
using BarkBeetle.ToolpathStackSetting;
using BarkBeetle.Utils;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Rhino.Render.ChangeQueue;

namespace BarkBeetle.Comps6Display
{
    public class EvaluateOverhang : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public EvaluateOverhang()
          : base("Evaluate Overhang", "Overhang",
              "Evaulate the overhang angles of the curves",
              "BarkBeetle", "6-Display & Analysis")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpath Stack", "TS", "BarkBeetle ToolpathStack object", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Display Thickness", "Thickness", "Display thickness of the segments", GH_ParamAccess.item, 1);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddColourParameter("Legend Colors", "C", "Colors for legend visualization", GH_ParamAccess.list);
            pManager.AddNumberParameter("Legend Tags", "T", "Tags for legend visualization", GH_ParamAccess.list);
            pManager.AddCurveParameter("Curves", "C", "Output curves to measure angle.", GH_ParamAccess.list);
        }

        private List<Curve> allSegments;
        private List<double> allAngles;
        private ToolpathStack toolpathStack;
        int thickness = 1;

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Initialize
            ToolpathStackGoo goo = null;

            //Set inputs
            if (!DA.GetData(0, ref goo)) return;
            toolpathStack = goo.Value;
            if (!DA.GetData(1, ref thickness)) return;

            EvaluationDisplay display = new EvaluationDisplay();
            display.EvaluateDiscontinueAngles(toolpathStack, thickness, out allSegments, out allAngles, out List<Color> legendColors, out List<double> legendTags);

            DA.SetDataList(0, legendColors);
            DA.SetDataList(1, legendTags);
            DA.SetDataList(2, allSegments);
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (this.Hidden) return;
            if (this.Locked) return;
            if (toolpathStack == null) return;
            if (allAngles == null || allSegments == null ||  allSegments.Count == 0 || allAngles.Count == 0) return;

            EvaluationDisplay display = new EvaluationDisplay();
            Color[] colormap = display.CreateColormap();

            for (int j = 0; j < allSegments.Count; j++)
            {
                Curve line = allSegments[j];
                double angle = allAngles[j];

                double normalized = (angle - allAngles.Min()) / (allAngles.Max() - allAngles.Min());
                Color color = display.MapToColor(normalized, colormap);
                args.Display.DrawCurve(line, color, thickness);
            }

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
                return Resources.overhang;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("2053C91F-6E64-479C-B417-6DFA0AEC34A7"); }
        }
    }
}