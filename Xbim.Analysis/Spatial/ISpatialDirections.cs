using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.Kernel;

namespace Xbim.Analysis.Spatial
{
    public interface ISpatialDirections
    {
        bool NorthOf(IfcProduct first, IfcProduct second);
        bool SouthOf(IfcProduct first, IfcProduct second);
        bool WestOf(IfcProduct first, IfcProduct second);
        bool EastOf(IfcProduct first, IfcProduct second);
        bool Above(IfcProduct first, IfcProduct second);
        bool Below(IfcProduct first, IfcProduct second);
    }
}
