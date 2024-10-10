using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarkBeetle.GeometriesPackage
{
    internal class GeometryPackageGoo : GH_Goo<GeometryPackage>
    {
        // Construct geometry
        public GeometryPackageGoo() : this(null) { }

        public GeometryPackageGoo(GeometryPackage geometry)
        {
            Value = geometry;
        }

        public override bool IsValid => Value != null;

        public override string TypeName => "RefinedGeometry";

        public override string TypeDescription => "Contains a RefinedGeometry object";

        public override IGH_Goo Duplicate()
        {
            // TODO: How to deep copy here?
            return new GeometryPackageGoo(Value);  
        }

        public override string ToString()
        {
            return "BarkBeetle RefinedGeometry Object";
        }

        //// 渲染输出对象的简化版本
        //public override bool CastTo<Q>(ref Q target)
        //{
        //    if (typeof(Q).IsAssignableFrom(typeof(Surface)))
        //    {
        //        target = (Q)(object)Value.GetSurface();
        //        return true;
        //    }
        //    if (typeof(Q).IsAssignableFrom(typeof(GH_Structure<GH_Point>)))
        //    {
        //        target = (Q)(object)Value.GetSkeleton();
        //        return true;
        //    }
        //    return false;
        //}

        public override bool CastFrom(object source)
        {
            if (source is GeometryPackage geometry)
            {
                Value = geometry;
                return true;
            }
            return false;
        }
    }
}
