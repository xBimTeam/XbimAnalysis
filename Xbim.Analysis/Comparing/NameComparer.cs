using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.Kernel;

namespace Xbim.Analysis.Comparing
{
    public class NameComparer : IModelComparerII
    {
        public string Name
        {
            get
            {
                return "Xbim Name Comparer";
            }
        }

        public string Description
        {
            get
            {
                return "This is name comparer. It compares input objects based on their names.";
            }
        }

        public ComparisonType ComparisonType
        {
            get
            {
                return ComparisonType.NAME;
            }
        }

        private int _weight = 10;
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
        public ComparisonResult Compare<T>(T baseline, IO.XbimModel revisedModel) where T : IfcRoot
        {
            //it doesn't make a sense to search for a match when original is not defined
            if (baseline.Name == null)
                return null;

            var result = new ComparisonResult(baseline, this);
            var candidates = revisedModel.Instances.Where<T>(r => r.Name == baseline.Name);
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
            result.Candidates.AddRange(revisedModel.Instances.Where<T>(r => !_processed.Contains(r) && r.Name != null));
            return result;
        }

        public IEnumerable<ComparisonResult> Compare<T>(Xbim.IO.XbimModel baseline, Xbim.IO.XbimModel revised) where T : Ifc2x3.Kernel.IfcRoot
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
