using BarkBeetle.Pattern;
using Eto.Forms;
using Rhino.DocObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;
using BarkBeetle.Utils;
using Grasshopper.Kernel.Types;

namespace BarkBeetle.ToolpathStackSetting
{
    internal class StackOnTop
    {
        public static ToolpathStack CreatStackOnTop(ToolpathStack originalStack, ToolpathPattern toolpathPattern, double newLH, double newTotalH, out ToolpathPattern newPattern) 
        {
            newPattern = toolpathPattern.DeepCopy();

            Surface topSurface = originalStack.Surfaces[originalStack.Surfaces.Count - 1].Value.Surfaces[0].Duplicate() as Surface;
            Surface oldSurface = toolpathPattern.BaseSrf.Duplicate() as Surface;
            List<Point3d> points = CurveUtils.GetExplodedCurveVertices(newPattern.CoutinuousCurve, originalStack.LayerHeight * 5);
            Curve newBaseCrv = CurveUtils.RemapPolyCurveOnNewSurface(topSurface, oldSurface, points);
            newPattern.CoutinuousCurve = newBaseCrv;

            if (originalStack is StackBetween || originalStack is StackOffset)
            {
                Surface dupSurface = topSurface.Duplicate() as Surface;
                double offsetDistance = newLH;

                // Check normal direction
                Vector3d normal = dupSurface.NormalAt(0.5, 0.5);
                if (normal.Z < 0)
                {
                    offsetDistance = -offsetDistance; // if normal is negative, flip offset direction
                }
                Surface offsetSurface = dupSurface.Offset(offsetDistance, 0.01);

                newPattern.BaseSrf = offsetSurface;

                ToolpathStack newStack = new StackOffset(new StackPatterns(new List<ToolpathPattern> { newPattern }), newLH, originalStack.AngleGlobal, newTotalH, originalStack.RefGeo, originalStack.RotateAngle);
                return newStack;
            }

            else if (originalStack is StackVertical)
            {
                ToolpathPattern verticalPattern = toolpathPattern.DeepCopy();

                newPattern.BaseSrf = topSurface;

                ToolpathStack newStack = new StackVertical(new StackPatterns(new List<ToolpathPattern> { newPattern }), newLH, originalStack.AngleGlobal, newTotalH + newLH, originalStack.RefGeo, originalStack.RotateAngle);
                newStack.Surfaces.RemoveAt(0);
                newStack.LayerCurves.RemoveAt(0);
                newStack.FinalCurve = newStack.CreateStackFinalCurve();
                List<List<GH_Number>> sf = new List<List<GH_Number>>();
                newStack.OrientPlanes = newStack.CreateStackOrientPlanes(newStack.RotateAngle, ref sf); // Will calculate speed factor here
                newStack.SpeedFactors = sf;
                return newStack;
            }

            return originalStack;
        }
    }
}
