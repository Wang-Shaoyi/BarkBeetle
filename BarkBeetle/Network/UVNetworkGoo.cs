using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarkBeetle.Network
{
    internal class UVNetworkGoo: GH_Goo<UVNetwork>
    {
        // Construct geometry
        public UVNetworkGoo() : this(null) { }

        public UVNetworkGoo(UVNetwork geometry)
        {
            Value = geometry;
        }

        public override bool IsValid => Value != null;

        public override string TypeName => "UVNetwork";

        public override string TypeDescription => "Contains a UVNetwork";

        public override IGH_Goo Duplicate()
        {
            // TODO: How to deep copy here?
            return new UVNetworkGoo(Value);
        }

        public override string ToString()
        {
            return "BarkBeetle UVNetwork Object";
        }

        public override bool CastFrom(object source)
        {
            if (source is UVNetwork geometry)
            {
                Value = geometry;
                return true;
            }
            return false;
        }
    }
}
