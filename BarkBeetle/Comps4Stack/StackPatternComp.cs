using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;


using BarkBeetle.Utils;
using Grasshopper.Kernel.Data;
using BarkBeetle.Pattern;
using BarkBeetle.Skeletons;

namespace BarkBeetle.CompsToolpath
{
    public class StackPatternComp : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Extract_RefinedGeometry class.
        /// </summary>
        public StackPatternComp()
          : base("Stack Toolpath Pattern", "Stack Pattern",
              "Stack multiple patterns together",
              "BarkBeetle", "4-Stack")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Top Pattern", "Top", "BarkBeetle Toolpath Pattern object", GH_ParamAccess.item);
            pManager.AddNumberParameter("Top Count", "TC", "Number of top layers", GH_ParamAccess.item, 0);
            pManager.AddGenericParameter("Middle Pattern(s)", "Middle", "BarkBeetle Toolpath Pattern object, may have multiple", GH_ParamAccess.list);
            pManager.AddGenericParameter("Bottom Pattern", "Bottom", "BarkBeetle Toolpath Pattern object", GH_ParamAccess.item);
            pManager.AddNumberParameter("Bottom Count", "TP", "Number of bottom layers", GH_ParamAccess.item, 0);
            

            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Stack Patterns", "Patterns", "BarkBeetle Stack Patterns object", GH_ParamAccess.item);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Initialize
            ToolpathPatternGoo bottomPgoo = null;
            ToolpathPatternGoo topPgoo = null;
            List<ToolpathPatternGoo> mainPsgoo = new List<ToolpathPatternGoo>();
            double bottomC = 0;
            double topC = 0;

            List<ToolpathPattern> mainPs = new List<ToolpathPattern>();
            ToolpathPattern bottomP = null;
            ToolpathPattern topP = null;

            //Set inputs
            if (DA.GetDataList(2, mainPsgoo)) 
            { 
                foreach (var pgoo in mainPsgoo) mainPs.Add(pgoo.Value);
            }
            if (DA.GetData(3, ref bottomPgoo)) { bottomP = bottomPgoo.Value; }
            if (DA.GetData(0, ref topPgoo)) { topP = topPgoo.Value; }
            DA.GetData(4, ref bottomC);
            DA.GetData(1, ref topC);

            int bottomCInt = (int)bottomC;
            int topCInt = (int)topC;

            //Run

            StackPatterns stackP = new StackPatterns(mainPs, bottomP, topP, bottomCInt, topCInt);

            StackPatternsGoo stackGoo = new StackPatternsGoo(stackP);

            // Output
            DA.SetData(0, stackGoo);
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
                return Resources.StackPattern;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("399C0193-D572-4059-8F1D-1974A1FF6FFA"); }
        }
    }
}