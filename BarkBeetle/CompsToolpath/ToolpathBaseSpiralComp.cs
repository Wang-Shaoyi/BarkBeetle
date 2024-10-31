using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using BarkBeetle.GeometriesPackage;
using BarkBeetle.Utils;
using Rhino.Display;
using BarkBeetle.ToolpathBaseSetting;
using System.Security.Cryptography;


namespace BarkBeetle.CompsToolpath
{
    public class ToolpathBaseSpiralComp : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Toolpath class.
        /// </summary>
        public ToolpathBaseSpiralComp()
          : base("Spiral Toolpath Base", "Spiral Base",
              "A spiral shape generated from the skeleton as the first layer of the toolpath",
              "BarkBeetle", "Toolpath")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Skeleton Package", "SP", "BarkBeetle Skeleton Package object", GH_ParamAccess.item);
            pManager.AddPointParameter("Seam Point", "Pt", "Seam point of the toolpath (start point)", GH_ParamAccess.item, new Point3d(0,0,0));
            pManager.AddNumberParameter("Path Width", "pw", "Seam point of the toolpath (start point)", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpath Base", "TB", "BarkBeetle ToolpathBase object", GH_ParamAccess.item);
            pManager.AddCurveParameter("Toolpath Base Curve", "C", "Toolpath curve for the first layer", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Initialize
            SkeletonPackageGoo goo = null;
            GH_Point ghpt = null;
            double pathWidth = 0;

            //Set inputs
            if (!DA.GetData(0, ref goo)) return;
            SkeletonPackage refinedGeometry = goo.Value;
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
            ToolpathPattern toolpath = new ToolpathPatterhSpiral(refinedGeometry,ghpt.Value,pathWidth);
            GH_Curve crv = new GH_Curve(toolpath.Curve);

            ToolpathPatternGoo baseGoo = new ToolpathPatternGoo(toolpath);

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