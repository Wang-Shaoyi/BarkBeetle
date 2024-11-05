using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarkBeetle.Pattern
{
    internal class StackPatterns
    {
        public List<ToolpathPattern> MainPatterns = null;
        public ToolpathPattern BottomPattern = null;
        public ToolpathPattern TopPattern = null;
        public int BottomCount = 0;
        public int TopCount = 0;

        public StackPatterns(
            List<ToolpathPattern> mainPatterns = null,
            ToolpathPattern bottomP = null, ToolpathPattern topP = null,
            int bottomC = 0, int topC = 0)
        {
            MainPatterns = mainPatterns;
            BottomPattern = bottomP;
            TopPattern = topP;
            BottomCount = bottomC;
            TopCount = topC;
        }
    }
}
