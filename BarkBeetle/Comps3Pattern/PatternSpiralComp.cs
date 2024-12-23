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
    public class PatternSpiralComp : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Toolpath class.
        /// </summary>
        public PatternSpiralComp()
          : base("Spiral Toolpath Pattern", "Spiral Pattern",
              "A spiral shape generated from the skeleton as a layer of the toolpath",
              "BarkBeetle", "3-Pattern")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Skeleton Graph", "SG", "BarkBeetle Skeleton Graph object", GH_ParamAccess.item);
            pManager.AddPointParameter("Seam Point", "Pt", "Seam point of the toolpath (start point)", GH_ParamAccess.item, new Point3d(0,0,0));
            pManager.AddNumberParameter("Path Width", "pw", "Width of the print path", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpath Pattern", "TP", "BarkBeetle ToolpathPattern object", GH_ParamAccess.item);
            pManager.AddCurveParameter("Curve", "C", "Toolpath curve for a layer", GH_ParamAccess.item);
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

            //Set inputs
            if (!DA.GetData(0, ref goo)) return;
            SkeletonGraph skeletonGraph = goo.Value;
            if (!DA.GetData(1, ref ghpt)) return;
            if (!DA.GetData(2, ref pathWidth)) return;


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

            // Run Function
            ToolpathPattern pattern = new ToolpathPatternSpiral(skeletonGraph, ghpt.Value,pathWidth);
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
                return Resources.SpiralToolpathBase;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("22E2B075-3C2B-4C83-A73B-D6F2D97C06C3"); }
        }
    }
}