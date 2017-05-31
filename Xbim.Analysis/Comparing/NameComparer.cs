using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;

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


        private HashSet<IIfcRoot> _processed = new HashSet<IIfcRoot>();
        public ComparisonResult Compare<T>(T baseline, IModel revisedModel) where T : IIfcRoot
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

        public ComparisonResult GetResidualsFromRevision<T>(IModel revisedModel) where T : IIfcRoot
        {
            var result = new ComparisonResult(null, this);
            result.Candidates.AddRange(revisedModel.Instances.OfType<IIfcRoot>().Where(r => !_processed.Contains(r) && r.Name != null));
            return result;
        }

        public IEnumerable<ComparisonResult> Compare<T>(IModel baseline, IModel revised) where T : IIfcRoot
        {
            foreach (var b in baseline.Instances.OfType<T>())
            {
                yield return Compare<T>(b, revised);
            }
            yield return GetResidualsFromRevision<T>(revised);
        }

        public IEnumerable<Difference> GetDifferences(IIfcRoot baseline, IIfcRoot revision)
        {
            throw new NotImplementedException();
        }
    }
}
