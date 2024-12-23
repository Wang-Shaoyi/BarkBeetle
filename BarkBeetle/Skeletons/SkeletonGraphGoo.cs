using BarkBeetle.Network;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarkBeetle.Skeletons
{
    internal class SkeletonGraphGoo : GH_Goo<SkeletonGraph>
    {
        // Construct geometry
        public SkeletonGraphGoo() : this(null) { }

        public SkeletonGraphGoo(SkeletonGraph geometry)
        {
            Value = geometry;
        }

        public override bool IsValid => Value != null;

        public override string TypeName => "Skeleton Graph";

        public override string TypeDescription => "Contains a SkeletonGraph";

        public override IGH_Goo Duplicate()
        {
            // TODO: How to deep copy here?
            return new SkeletonGraphGoo(Value);
        }

        public override string ToString()
        {
            return "BarkBeetle Skeleton Object";
        }

        public override bool CastFrom(object source)
        {
            if (source is SkeletonGraph geometry)
            {
                Value = geometry;
                return true;
            }
            return false;
        }
    }
}
