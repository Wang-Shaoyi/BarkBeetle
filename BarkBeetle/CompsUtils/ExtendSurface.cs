using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using BarkBeetle.Utils;
using BarkBeetle.GeometriesPackage;
using BarkBeetle.Skeletons;

namespace BarkBeetle.CompsUtils
{
    public class ExtendSurface : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the OrganizePtTreeFromSrf class.
        /// </summary>
        public ExtendSurface()
          : base("Extend surface", "Extend surface",
              "Extend a surface on uv",
              "BarkBeetle", "Utils")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Surface", "S", "Surface", GH_ParamAccess.item);
            pManager.AddNumberParameter("uDistance", "u_d", "Extend distance on u direction", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("vDistance", "v_d", "Extend distance on v direction", GH_ParamAccess.item, 0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Extended surface", "S", "Extended surface", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Initialize
            Surface surface = null;
            double uD = 0.0;
            double vD = 0.0;

            //Set inputs
            if (!DA.GetData(0, ref surface)) return;
            if (!DA.GetData(1, ref uD)) return;
            if (!DA.GetData(2, ref vD)) return;

            // Error message.
            if (surface == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No surface");
                return;
            }

            SkeletonPackageManager rgManager = new SkeletonPackageManager();
            rgManager.SetComponent(this);
            Surface extendedSurface = rgManager.ProcessExtendedSurface(uD, vD, surface);

            DA.SetData(0, extendedSurface);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Resources.ExtendSurface;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("0D5EB2A5-FA41-4230-A3B6-384CE03354F3"); }
        }
    }
}