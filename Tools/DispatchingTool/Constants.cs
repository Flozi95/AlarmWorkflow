using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DispatchingTool
{
    static class Constants
    {
        internal const int OfpInterval = 2000;
        internal const int OfpMaxAge = (7 * 24 * 60);
        internal const bool OfpOnlyNonAcknowledged = true;
    }
}
