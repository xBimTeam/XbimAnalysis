using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.Kernel;

namespace Xbim.Analysis.Comparing
{
    public class GuidComparer : IModelComparerII
    {
        public string Name
        {
            get { return "Xbim Globally Unique ID Comparer"; }
        }

        public string Description
        {
            get { return "Objects are supposed to be the same if they have the same GUID."; }
        }

        public ComparisonType ComparisonType
        {
            get { return ComparisonType.GUID; }
        }

        private int _weight = 80;
        public int Weight
        {
            get
            {
                return _weight;
            }
            set
            {
                _weight = value;
            }
        }

        private HashSet<IfcRoot> _processed = new HashSet<IfcRoot>();
        public ComparisonResult Compare<T>(T baseline, IO.XbimModel revisedModel) where T : Ifc2x3.Kernel.IfcRoot
        {
            var result = new ComparisonResult(baseline, this);
            var candidates = revisedModel.Instances.Where<T>(r => r.GlobalId == baseline.GlobalId);
            foreach (var c in candidates)
            {
                result.Candidates.Add(c);
                if (!_processed.Contains(c))
                    _processed.Add(c);
            }
            return result;
        }

        public ComparisonResult GetResidualsFromRevision<T>(IO.XbimModel revisedModel) where T : Ifc2x3.Kernel.IfcRoot
        {
            var result = new ComparisonResult(null, this);
            result.Candidates.AddRange(revisedModel.Instances.Where<T>(r => !_processed.Contains(r)));
            return result;
        }

        public IEnumerable<ComparisonResult> Compare<T>(IO.XbimModel baseline, IO.XbimModel revised) where T : Ifc2x3.Kernel.IfcRoot
        {
            foreach (var b in baseline.Instances.OfType<T>())
            {
                yield return Compare<T>(b, revised);
            }
            yield return GetResidualsFromRevision<T>(revised);
        }

        public IEnumerable<Difference> GetDifferences(Ifc2x3.Kernel.IfcRoot baseline, Ifc2x3.Kernel.IfcRoot revision)
        {
            throw new NotImplementedException();
        }
    }
}
