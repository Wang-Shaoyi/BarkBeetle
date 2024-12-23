using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;


using BarkBeetle.Utils;
using Rhino.Display;
using BarkBeetle.Pattern;
using BarkBeetle.Skeletons;
using System.Security.Cryptography;


namespace BarkBeetle.CompsToolpath
{
    public class PatternSnakeComp : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Toolpath class.
        /// </summary>
        public PatternSnakeComp()
          : base("Snake Infill Pattern", "Snake Pattern",
              "A snake infill shape generated from the skeleton as a layer of the toolpath",
              "BarkBeetle", "3-Pattern")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Skeleton Graph", "Skeleton", "BarkBeetle Skeleton Graph object", GH_ParamAccess.item);
            pManager.AddPointParameter("Seam Point", "Seam Point", "Seam point of the toolpath (start point)", GH_ParamAccess.item, new Point3d(0,0,0));
            pManager.AddNumberParameter("Path Width", "Path Width", "Seam point of the toolpath (start point)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Spacing", "Spacing", "How much spacing between toolpaths", GH_ParamAccess.item, 0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpath Pattern", "Pattern", "BarkBeetle ToolpathPattern object", GH_ParamAccess.item);
            pManager.AddCurveParameter("Pattern Curve", "Curve", "Toolpath curve for a layer", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Initialize
            SkeletonGraphGoo goo = null;
            GH_Point ghpt = null;
            double pathWidth = 0;
            double spacing = 0;

            //Set inputs
            if (!DA.GetData(0, ref goo)) return;
            SkeletonGraph skeletonGraph = goo.Value;
            if (!DA.GetData(1, ref ghpt)) return;
            if (!DA.GetData(2, ref pathWidth)) return;
            if (!DA.GetData(3, ref spacing)) return;


            // Error message.
            if (ghpt == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No point");
                return;
            }
            if (pathWidth <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "path width must be larger than 0");
                return;
            }
            if (skeletonGraph.UVNetwork.StripWidth < pathWidth * 6)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Strip width should be larger or path width should be smaller");
                return;
            }

            // Run Function
            ToolpathPattern pattern = new ToolpathPatternSnake(skeletonGraph, ghpt.Value,pathWidth, spacing);
            GH_Curve crv = new GH_Curve(pattern.CoutinuousCurve);

            ToolpathPatternGoo baseGoo = new ToolpathPatternGoo(pattern);

            // Finally assign the spiral to the output parameter.
            DA.SetData(0, baseGoo);
            DA.SetData(1, crv);
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Resources.SnakeInfilll;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("681C7461-6585-431F-8261-DC70441C8E4A"); }
        }
    }
}