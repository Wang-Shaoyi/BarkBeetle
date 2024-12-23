using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace BarkBeetle.Utils
{
    internal class GcodeRelated
    {
        public static List<string> ConvertPlanesToGCodeWithSpeed(List<GH_Plane> planes, List<double> speeds)
        {
            List<string> gcodeCommands = new List<string>();

            if (planes.Count != speeds.Count)
            {
                throw new ArgumentException("Planes list and speeds list must have the same length.");
            }

            gcodeCommands.Add("M100 ; Open valve");

            for (int i = 0; i < planes.Count; i++)
            {
                Plane plane = planes[i].Value;
                double speed = speeds[i];

                // Get plane information
                Point3d origin = plane.Origin;
                Vector3d xAxis = plane.XAxis;
                Vector3d yAxis = plane.YAxis;
                Vector3d zAxis = plane.ZAxis;

                // Calculate rotation angle
                double aAngle = Vector3d.VectorAngle(Vector3d.XAxis, xAxis); 
                double bAngle = Vector3d.VectorAngle(Vector3d.YAxis, yAxis); 
                double cAngle = Vector3d.VectorAngle(Vector3d.ZAxis, zAxis); 

                // Generate Gcode
                string command = string.Format("G1 X{0:F3} Y{1:F3} Z{2:F3} A{3:F3} B{4:F3} C{5:F3} F{6:F3}",
                    origin.X, origin.Y, origin.Z, aAngle, bAngle, cAngle, speed);
                gcodeCommands.Add(command);
            }

            gcodeCommands.Add("M101 ; Close valve");

            return gcodeCommands;
        }
    }
}
