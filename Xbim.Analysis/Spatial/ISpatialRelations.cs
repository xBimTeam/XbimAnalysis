using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.Analysis.Spatial
{
    public interface ISpatialRelations
    {
        bool Equals(IfcProduct first, IfcProduct second);
        bool Disjoint(IfcProduct first, IfcProduct second);
        bool Intersects(IfcProduct first, IfcProduct second);
        bool Touches(IfcProduct first, IfcProduct second);
        bool Within(IfcProduct first, IfcProduct second);
        bool Contains(IfcProduct first, IfcProduct second);
        bool Relate(IfcProduct first, IfcProduct second);
        //these two do not make a sense in pure 3D
        //bool? Crosses(IfcProduct first, IfcProduct second);
        //bool? Overlaps(IfcProduct first, IfcProduct second);

        IEnumerable<IfcProduct> GetEqualTo(IfcProduct prod);
        IEnumerable<IfcProduct> GetDisjointFrom(IfcProduct prod);
        IEnumerable<IfcProduct> GetIntersectingWith(IfcProduct prod);
        IEnumerable<IfcProduct> GetTouching(IfcProduct prod);
        IEnumerable<IfcProduct> GetContainedProducts(IfcProduct prod);
        IEnumerable<IfcProduct> GetRelatingProducts(IfcProduct prod);
    }
}
