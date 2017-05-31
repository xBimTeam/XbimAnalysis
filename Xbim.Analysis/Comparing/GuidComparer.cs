using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;

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

        private HashSet<IIfcRoot> _processed = new HashSet<IIfcRoot>();
        public ComparisonResult Compare<T>(T baseline, IModel revisedModel) where T : IIfcRoot
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

        public ComparisonResult GetResidualsFromRevision<T>(IModel revisedModel) where T : IIfcRoot
        {
            var result = new ComparisonResult(null, this);
            result.Candidates.AddRange(revisedModel.Instances.OfType<IIfcRoot>().Where(r => !_processed.Contains(r)));
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
