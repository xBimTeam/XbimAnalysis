using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc4.Interfaces;

namespace Xbim.Analysis.Comparitors
{
    public class NameComparison : IModelComparer
    {
        private Dictionary<Int32, Int32> map = new Dictionary<Int32, Int32>();
        public Dictionary<Int32, Int32> GetMap() { return map; }

        public Dictionary<IIfcRoot, ChangeType> Compare(IEnumerable<IIfcRoot> Baseline, IEnumerable<IIfcRoot> Delta)
        {
            results.Clear();

            var baseline = new List<IIfcRoot>(Baseline);
            var delta = new List<IIfcRoot>(Delta);

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
        private Dictionary<IIfcRoot, ChangeType> results = new Dictionary<IIfcRoot, ChangeType>();
        private void Match(List<IIfcRoot> start, List<IIfcRoot> delta, bool ReturnMappingFromBaseline=true)
        {
            var collection = new List<IIfcRoot>(start);
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
