using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Analysis.Comparitors;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.UtilityResource;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.Analysis
{
    public delegate void MessageCallback(string message);
    public class VersionComparison
    {
        private XbimModel Baseline { get; set; }
        private XbimModel Revision { get; set; }
        private List<IfcRoot> WorkingCopyBaseline;
        private List<IfcRoot> WorkingCopyDelta;

        public event MessageCallback OnMessage;
        private void Message(String message)
        {
            if (OnMessage != null) OnMessage(message);
        }

        public Dictionary<IfcRoot, ChangeType> EntityLabelChanges = new Dictionary<IfcRoot, ChangeType>();
        public Dictionary<Int32, Int32> EntityMapping = new Dictionary<Int32, Int32>();
        public List<IfcRoot> Deleted = new List<IfcRoot>();
        public List<IfcRoot> Added = new List<IfcRoot>();
        public List<IfcGloballyUniqueId> DuplicateBaseItems = new List<IfcGloballyUniqueId>();

        public Int32 StartComparison(XbimModel baseline, XbimModel revision, string filter = "")
        {

            Baseline = baseline;
            Revision = revision;

            Int32 ret = 0;
            if (filter == "")
            {
                // default behaviour (maintained during code review) is to test only for IfcProducts
                //
                WorkingCopyBaseline = Baseline.Instances.OfType<IfcProduct>().ToList<IfcRoot>();
                WorkingCopyDelta = Revision.Instances.OfType<IfcProduct>().ToList<IfcRoot>();

                //get guids into dictionary
                var MyTemp = WorkingCopyBaseline.Select(p => new baselineitem { GUID = p.GlobalId, Label = p.EntityLabel, Name = p.Name, MyType = p.GetType() }).ToList();

                //list duplicate Guids in Model
                DuplicateBaseItems.AddRange(MyTemp.GroupBy(k => k.GUID).Where(g => g.Count() > 1).Select(g => g.Key));
                if (DuplicateBaseItems.Count > 0)
                {
                    Message(String.Format("Warning {0} Duplicate Guids found in Model", DuplicateBaseItems.Count));
                }
                var comparer = new VersionGuidComparitor();
                var MyDictionary = MyTemp.Distinct<baselineitem>(comparer).ToDictionary(k => k.GUID, v => v); //think GroupBy?

                ret += StartProductsComparison();
            }
            else
            {
                IfcType ot = IfcMetaData.IfcType(filter.ToUpper());
                if (ot != null)
                {
                    if (ot.Type.IsSubclassOf(typeof(IfcRoot)))
                    {
                        WorkingCopyBaseline = new List<IfcRoot>(Baseline.Instances.OfType(filter, false).Cast<IfcRoot>());
                        WorkingCopyDelta = new List<IfcRoot>(Revision.Instances.OfType(filter, false).Cast<IfcRoot>());
                        ret += StartProductsComparison();
                    }
                }
            }
            return ret;
        }


        public Int32 StartProductsComparison()
        {
            if (ItemsNeedMatching())
                CheckGuids();
            if (ItemsNeedMatching())
                CheckNames();
            if (ItemsNeedMatching())
                CheckRelationships();
            if (ItemsNeedMatching())
                CheckProperties();
            if (ItemsNeedMatching())
                CheckGeometry();

            Deleted.AddRange(WorkingCopyBaseline);
            Added.AddRange(WorkingCopyDelta);

            Message(String.Format("All Checks Complete. {0} items unresolved", WorkingCopyBaseline.Count + WorkingCopyDelta.Count));
            if (WorkingCopyBaseline.Count > 0)
            {
                Message("Cannot resolve Baseline item(s):");
                foreach (var i in WorkingCopyBaseline)
                    Message(String.Format("Cannot resolve Missing GUID: {0} (EntityLabel: {1})", i.GlobalId, i.EntityLabel));
            }
            if (WorkingCopyDelta.Count > 0)
            {
                Message("Cannot resolve Delta item(s):");
                foreach (var i in WorkingCopyDelta)
                {
                    Message(String.Format("Cannot resolve Added GUID: {0} (EntityLabel: {1})", i.GlobalId, i.EntityLabel));
                }
            }

            Message("Map from Entity Labels is as follows (Baseline -> Delta)");
            foreach (var key in EntityMapping)
            {
                Message(String.Format("{0} -> {1}", key.Key, EntityMapping[key.Key]));
            }

            return WorkingCopyBaseline.Count + WorkingCopyDelta.Count;
        }

        private bool ItemsNeedMatching()
        {
            return
                (WorkingCopyDelta != null && WorkingCopyDelta.Count > 0)
                &&
                (WorkingCopyBaseline != null && WorkingCopyBaseline.Count > 0);
        }

        private void CheckNames()
        {
            Message("Starting Name Check");
            NameComparison n = new NameComparison();

            var results = n.Compare(WorkingCopyBaseline, WorkingCopyDelta);

            //update working copies as we go with those still yet to resolve
            WorkingCopyBaseline.Clear(); WorkingCopyDelta.Clear();

            foreach (var item in results.Where(x => x.Value == ChangeType.Matched))
            {
                Message(String.Format("Found a Match for type {1} with Name: {0}", item.Key.Name, item.Key.GetType().ToString()));
                EntityLabelChanges[item.Key] = item.Value;
            }
            foreach (var item in results.Where(x => x.Value == ChangeType.Added))
            {
                WorkingCopyDelta.Add(item.Key);
                Message(String.Format("Found a new item of type {1} with Name: {0}", item.Key.Name, item.Key.GetType().ToString()));
                EntityLabelChanges[item.Key] = item.Value;
            }
            foreach (var item in results.Where(x => x.Value == ChangeType.Deleted))
            {
                WorkingCopyBaseline.Add(item.Key);
                Message(String.Format("Found a missing item of type {1} with Name: {0}", item.Key.Name, item.Key.GetType().ToString()));
                EntityLabelChanges[item.Key] = item.Value;
            }
            foreach (var item in results.Where(x => x.Value == ChangeType.Unknown))
            {
                WorkingCopyBaseline.Add(item.Key);
                Message(String.Format("Found duplicate possibilities for item of type {1} with Name: {0}", item.Key.Name, item.Key.GetType().ToString()));
                EntityLabelChanges[item.Key] = item.Value;
            }

            var m = n.GetMap();
            foreach (var key in m)
            {
                EntityMapping[key.Key] = m[key.Key];
            }

            Message("Name Check - Complete");
        }
        private void CheckGuids()
        {
            Message("Starting Guid Check");
            GuidComparison g = new GuidComparison();

            var results = g.Compare(WorkingCopyBaseline, WorkingCopyDelta);

            //update working copies as we go with those still yet to resolve
            WorkingCopyBaseline.Clear(); WorkingCopyDelta.Clear();

            foreach (var item in results.Where(x => x.Value == ChangeType.Matched))
            {
                Message(String.Format("Found a Match for type {1} with GUID: {0}", item.Key.GlobalId, item.Key.GetType().ToString()));
                EntityLabelChanges[item.Key] = item.Value;
            }
            foreach (var item in results.Where(x => x.Value == ChangeType.Added))
            {
                WorkingCopyDelta.Add(item.Key);
                Message(String.Format("Found a new item of type {1} with GUID: {0}", item.Key.GlobalId, item.Key.GetType().ToString()));
                EntityLabelChanges[item.Key] = item.Value;
            }
            foreach (var item in results.Where(x => x.Value == ChangeType.Deleted))
            {
                WorkingCopyBaseline.Add(item.Key);
                Message(String.Format("Found a missing item of type {1} with GUID: {0}", item.Key.GlobalId, item.Key.GetType().ToString()));
                EntityLabelChanges[item.Key] = item.Value;
            }
            foreach (var item in results.Where(x => x.Value == ChangeType.Unknown))
            {
                WorkingCopyBaseline.Add(item.Key);
                Message(String.Format("Found duplicate possibilities for item of type {1} with GUID: {0}", item.Key.GlobalId, item.Key.GetType().ToString()));
                EntityLabelChanges[item.Key] = item.Value;
            }

            var m = g.GetMap();
            foreach (var key in m)
            {
                EntityMapping[key.Key] = m[key.Key];
            }
            Message("Guid Check - Complete");
        }

        private void CheckGeometry()
        {
            Message("Starting - Geometry Check");
            GeometryComparer gc = new GeometryComparer();
            Xbim3DModelContext baseContext = new Xbim3DModelContext(Baseline);
            Xbim3DModelContext revisionContext = new Xbim3DModelContext(Revision);
            //we have to sort out comparision and units, this assumes they are both in the same units at the moment
            //suggest all geometry is kept in metres SI
            var results = gc.Compare(baseContext, revisionContext, Baseline.ModelFactors.OneMilliMetre);
            //update working copies as we go with those still yet to resolve
            WorkingCopyBaseline.Clear(); WorkingCopyDelta.Clear();

            foreach (var item in results.Where(x => x.Value == ChangeType.Matched))
            {
                Message(String.Format("Found a Match for type {1} with GUID: {0}", item.Key.GlobalId, item.Key.GetType().ToString()));
                EntityLabelChanges[item.Key] = item.Value;
            }
            foreach (var item in results.Where(x => x.Value == ChangeType.Added))
            {
                WorkingCopyDelta.Add(item.Key);
                Message(String.Format("Found a new item of type {1} with GUID: {0}", item.Key.GlobalId, item.Key.GetType().ToString()));
                EntityLabelChanges[item.Key] = item.Value;
            }
            foreach (var item in results.Where(x => x.Value == ChangeType.Deleted))
            {
                WorkingCopyBaseline.Add(item.Key);
                Message(String.Format("Found a missing item of type {1} with GUID: {0}", item.Key.GlobalId, item.Key.GetType().ToString()));
                EntityLabelChanges[item.Key] = item.Value;
            }
            foreach (var item in results.Where(x => x.Value == ChangeType.Unknown))
            {
                WorkingCopyBaseline.Add(item.Key);
                Message(String.Format("Found duplicate possibilities for item of type {1} with GUID: {0}", item.Key.GlobalId, item.Key.GetType().ToString()));
                EntityLabelChanges[item.Key] = item.Value;
            }

            var m = gc.GetMap();
            foreach (var key in m)
            {
                EntityMapping[key.Key] = m[key.Key];
            }

            Message("Geometry Check - complete");
        }
        private void CheckRelationships()
        {
            Message("Starting - Relationship Check");
            Message("Check Not Implemented Yet");
            Message("Relationship Check - complete");
        }
        private void CheckProperties()
        {
            Message("Starting - Property Check");
            Message("Check Not Implemented Yet");
            Message("Property Check - complete");
        }
    }

    /// <summary>
    /// Comparitor which only compares IfcRoot Guids (to help detect and ignore duplicates)
    /// </summary>
    internal class VersionGuidComparitor : IEqualityComparer<baselineitem>
    {
        public VersionGuidComparitor()
        {
        }

        public bool Equals(baselineitem x, baselineitem y)
        {
            return x.GUID.Equals(y.GUID);
        }

        public int GetHashCode(baselineitem obj)
        {
            return obj.GetHashCode();
        }
    }
    internal struct baselineitem
    {
        internal IfcGloballyUniqueId GUID;
        internal int Label;
        internal String Name;
        internal Type MyType;
}
}
