using System;
using System.Collections.Generic;
using Xbim.Ifc4.Interfaces;

namespace Xbim.Analysis
{
    public interface IModelComparer
    {
        Dictionary<IIfcRoot, ChangeType> Compare(IEnumerable<IIfcRoot> Baseline, IEnumerable<IIfcRoot> Delta);
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
