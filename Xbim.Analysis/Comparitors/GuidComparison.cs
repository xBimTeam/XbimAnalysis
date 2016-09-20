using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.Kernel;

namespace Xbim.Analysis.Comparitors
{
    public class GuidComparison : IModelComparer
    {
        private Dictionary<Int32, Int32> map = new Dictionary<Int32, Int32>();
        public Dictionary<Int32, Int32> GetMap() { return map; }

        public Dictionary<IfcRoot, ChangeType> Compare(IEnumerable<IfcRoot> baseline, IEnumerable<IfcRoot> delta)
        {
            //Create our dictionary for return
            Dictionary<IfcRoot, ChangeType> changes = new Dictionary<IfcRoot, ChangeType>();

            //Work from copies so we can alter collections
            List<IfcRoot> Baseline = new List<IfcRoot>(baseline);
            List<IfcRoot> Delta = new List<IfcRoot>(delta);

            foreach (var i in baseline)
            {
                //Try to get the item in baseline and revisions
                IfcRoot r = null;
                try
                {
                    var c = Delta.Where(x => x.GlobalId == i.GlobalId);

                    //Check if we have a single matching result
                    if (c.Count() == 1) r = c.First();
                    else if (c.Count() > 1) { //If we have multiple results, we can't resolve this item by guid, so mark as unknown, and break
                        if (!changes.ContainsKey(i))
                        {
                            changes.Add(i, ChangeType.Unknown);
                        }
                        break;
                    }
                }
                catch (Exception) { }

                //If we have a match, then remove from our diff list list
                if (i != null && r != null)
                {
                    Baseline.Remove(i);
                    Delta.Remove(r);
                    if (!changes.ContainsKey(i))
                    {
                        changes.Add(i, ChangeType.Matched);
                    }
                    map.Add(i.EntityLabel, r.EntityLabel);
                }
            }

            //Anything left in baseline is only in the original (ie it's been deleted)
            foreach (var i in Baseline)
            {
                if (!changes.ContainsKey(i))
                {
                    changes.Add(i, ChangeType.Deleted);
                }
            }

            //Anything left in revprods is an addition
            foreach (var i in Delta)
            {
                if (!changes.ContainsKey(i))
                {
                    changes.Add(i, ChangeType.Added);
                }
            }

            return changes;
        }
    }
}
