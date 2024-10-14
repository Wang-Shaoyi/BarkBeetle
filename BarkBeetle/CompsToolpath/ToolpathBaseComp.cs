using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using BarkBeetle.GeometriesPackage;
using BarkBeetle.Utils;
using Rhino.Display;
using BarkBeetle.Toolpath;
using System.Security.Cryptography;
using BarkBeetle.ToolpathSetting;

namespace BarkBeetle.CompsToolpath
{
    public class ToolpathBaseComp : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Toolpath class.
        /// </summary>
        public ToolpathBaseComp()
          : base("Toolpath base", "Toolpath base",
              "The first layer of a toolpath ",
              "BarkBeetle", "Toolpath")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Refined geometry", "RG", "Refined Geometry object", GH_ParamAccess.item);
            pManager.AddPointParameter("Seam Point", "Pt", "Seam point of the toolpath (start point)", GH_ParamAccess.item, new Point3d(0,0,0));
            pManager.AddNumberParameter("Path Width", "pw", "Seam point of the toolpath (start point)", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Toolpath base curve", "C", "Toolpath curve for the first layer", GH_ParamAccess.item);
            pManager.AddCurveParameter("Toolpath base curves", "Curves", "Toolpath curve for the first layer", GH_ParamAccess.list);
            pManager.AddPointParameter("Pts", "P", "Pts", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Initialize
            GeometryPackageGoo goo = null;
            GH_Point ghpt = null;
            double pathWidth = 0;

            //Set inputs
            if (!DA.GetData(0, ref goo)) return;
            GeometryPackage refinedGeometry = goo.Value;
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
            ToolpathBase toolpath = new ToolpathBaseSpiral(refinedGeometry,ghpt.Value,pathWidth);
            GH_Curve crv = new GH_Curve(toolpath.Curve);
            List<Curve> curves = toolpath.Curves;
            GH_Structure<GH_Point> corners = TreeHelper.ConvertToGHStructure(toolpath.CornerPts);

            List<GH_Curve> ghCurves = new List<GH_Curve>();

            foreach (Curve curve in curves)
            {
                GH_Curve ghCurve = new GH_Curve(curve); 
                ghCurves.Add(ghCurve);                   
            }

            // Finally assign the spiral to the output parameter.
            //DA.SetData(0, toolpathGoo);
            DA.SetData(0, crv);
            DA.SetDataList(1, ghCurves);
            DA.SetDataTree(2, corners);
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
                return null;
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