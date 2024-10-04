using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace BarkBeetle
{
    public class BarkBeetleInfo : GH_AssemblyInfo
    {
        public override string Name => "BarkBeetle";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "";

        public override Guid Id => new Guid("6bd2745d-d9bc-40ea-beb7-1a93520e49a9");

        //Return a string identifying you or your company.
        public override string AuthorName => "Shaoyi Wang";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "shaoyiwang@berkeley.edu";

        //Return a string representing the version.  This returns the same version as the assembly.
        public override string AssemblyVersion => GetType().Assembly.GetName().Version.ToString();
    }
}