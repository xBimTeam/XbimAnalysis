using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.Analysis.Spatial
{
    public interface ISpatialFunctions
    {
        double Distance(IfcProduct first, IfcProduct second);
        IfcProduct Buffer(IfcProduct product);
        IfcProduct ConvexHull(IfcProduct product);
        IfcProduct Intersection(IfcProduct first, IfcProduct second);
        IfcProduct Union(IfcProduct first, IfcProduct second);
        IfcProduct Difference(IfcProduct first, IfcProduct second);
        IfcProduct SymDifference(IfcProduct first, IfcProduct second);
    }
}
