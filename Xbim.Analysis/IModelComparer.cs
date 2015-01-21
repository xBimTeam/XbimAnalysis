using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Xbim.Analysis.Comparing;
using Xbim.Ifc2x3.Kernel;
using Xbim.IO;
using Xbim.XbimExtensions.Interfaces;

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
