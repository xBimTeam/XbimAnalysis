using System;
using System.Collections.Generic;
using Xbim.Ifc2x3.Kernel;

namespace Xbim.Analysis
{
    public interface IModelComparer
    {
        Dictionary<IfcRoot, ChangeType> Compare(IEnumerable<IfcRoot> Baseline, IEnumerable<IfcRoot> Delta);
        Dictionary<Int32, Int32> GetMap();
    }


    public enum ChangeType { 
        Added,
        Deleted,
        Modified,
        Matched,
        Unknown
    }

}
