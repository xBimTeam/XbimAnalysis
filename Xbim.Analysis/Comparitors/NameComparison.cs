using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.Kernel;

namespace Xbim.Analysis.Comparitors
{
    public class NameComparison : IModelComparer
    {
        private Dictionary<Int32, Int32> map = new Dictionary<Int32, Int32>();
        public Dictionary<Int32, Int32> GetMap() { return map; }

        public Dictionary<IfcRoot, ChangeType> Compare(IEnumerable<IfcRoot> Baseline, IEnumerable<IfcRoot> Delta)
        {
            results.Clear();

            var baseline = new List<IfcRoot>(Baseline);
            var delta = new List<IfcRoot>(Delta);

            Match(baseline, delta);
            Match(delta, baseline, false);

            foreach (var i in baseline)
            {
                try
                {
                    if (!results.ContainsKey(i))
                    {
                        results.Add(i, ChangeType.Deleted);
                    }
                }
                catch (ArgumentException) { }
            }
            foreach (var i in delta)
            {
                try
                {
                    if (!results.ContainsKey(i))
                    {
                        results.Add(i, ChangeType.Added);
                    }
                }
                catch (ArgumentException) { }
            }

            return results;
        }
        private Dictionary<IfcRoot, ChangeType> results = new Dictionary<IfcRoot, ChangeType>();
        private void Match(List<IfcRoot> start, List<IfcRoot> delta, bool ReturnMappingFromBaseline=true)
        {
            var collection = new List<IfcRoot>(start);
            foreach (var i in collection)
            {
                var b = delta.Where(x => x.Name == i.Name && x.GetType() == i .GetType());
                if (b.Count() == 1) //if we have only 1 result, it should be a match
                {
                    var j = b.First();
                    if (!results.ContainsKey(j))
                    {
                        results.Add(j, ChangeType.Matched);
                        if(ReturnMappingFromBaseline)
                            map.Add(i.EntityLabel, j.EntityLabel);
                        else
                            map.Add(j.EntityLabel, i.EntityLabel);
                    }
                    delta.Remove(j);
                    start.Remove(i);
                }
                else if (b.Count() > 1)
                { // if we have multiple matches
                    foreach (var j in b)
                    {
                        if(!results.ContainsKey(j))
                            results.Add(j, ChangeType.Unknown);
                    }
                }
            }
        }
    }
}
