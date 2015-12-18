using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Analysis.Extensions;
using Xbim.Common;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.PropertyResource;

namespace Xbim.Analysis.Comparing
{
    public class PropertyComparer : IModelComparerII
    {

        List<PropertyHashedObjectDefinition> _cacheBase;
        List<PropertyHashedObjectDefinition> _cacheRevision;

        public PropertyComparer(IModel baseline, IModel revision)
        {
            if (revision == null || baseline == null)
                throw new ArgumentNullException();

            //create hash of the models for the future use (this is more efficient)
            _cacheBase = PropertyHashedObjectDefinition.CreateFrom(baseline);
            _cacheRevision = PropertyHashedObjectDefinition.CreateFrom(revision);
        }

        #region Model Comparer implementation
        public string Name
        {
            get { return "Xbim Property Comparer"; }
        }

        public string Description
        {
            get { return "Compares objects based on their simple properties."; }
        }

        public ComparisonType ComparisonType
        {
            get { return Comparing.ComparisonType.PROPERTIES; }
        }

        private int _weight = 60;
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

        private List<IfcRoot> _processed = new List<IfcRoot>();
        public ComparisonResult Compare<T>(T baseline, IO.XbimModel revisedModel) where T : Ifc2x3.Kernel.IfcRoot
        {
            var baseModel = baseline.ModelOf;
            if (baseModel == revisedModel)
                throw new ArgumentException("Baseline should be from the different model than revised model.");

            //this comparison makes a sense only for IfcObjectDefinition and it's descendants
            var objDef = baseline as IfcObjectDefinition;
            if (objDef == null) return null;

            var baseHashed = _cacheBase.Where(b => b.IfcObjectDefinition == objDef).FirstOrDefault();
            if (baseHashed == null) return null; //there is nothing to compare. 

            //hash filter
            var candidateHashes = _cacheRevision.Where(h => h.GetHashCode() == baseHashed.GetHashCode());
            //precise filter
            candidateHashes = candidateHashes.Where(h => h.Equals(baseHashed));

            //create result
            var result = new ComparisonResult(baseline, this);
            foreach (var candidate in candidateHashes)
            {
                result.Candidates.Add(candidate.IfcObjectDefinition);
                _processed.Add(candidate.IfcObjectDefinition);
            }
            return result;
        }

        public ComparisonResult GetResidualsFromRevision<T>(IO.XbimModel revisedModel) where T : Ifc2x3.Kernel.IfcRoot
        {
            var result = new ComparisonResult(null, this);
            var isInCache = new Func<IfcRoot, bool>(r => { return _cacheRevision.Where(c => c.IfcObjectDefinition == r).FirstOrDefault() != null; });
            var isNotProcessed = new Func<IfcRoot, bool>(r => { return !_processed.Contains(r); });
            result.Candidates.AddRange(revisedModel.Instances.Where<T>(r => isNotProcessed(r) && isInCache(r)));
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

        #endregion

        #region Helpers


        private class PropertyHashedObjectDefinition
        {
            private int _hash;
            private IfcObjectDefinition _objDef;
            public IfcObjectDefinition IfcObjectDefinition { get { return _objDef; } }

            private PropertyHashedObjectDefinition(IfcObjectDefinition objDef, int hash)
            {
                _objDef = objDef;
                _hash = hash;
            }

            public PropertyHashedObjectDefinition(IfcObjectDefinition objDef)
            {
                _objDef = objDef;
                IEnumerable<IfcPropertySet> pSets = null;
                var o = objDef as IfcObject;
                if (o != null)
                    pSets = o.PropertySets;
                var t = objDef as IfcTypeObject;
                if (t != null)
                    pSets = t.PropertySets;

                _hash = 0;
                foreach (var pSet in pSets)
                    _hash += pSet.GetPSetHash();
            }

            /// <summary>
            /// This is more efficient than doing the same object by object as it
            /// doesn't traverse relations more times.
            /// </summary>
            /// <param name="model">Model to be used</param>
            /// <returns>List of hashed object definitions</returns>
            public static List<PropertyHashedObjectDefinition> CreateFrom(IModel model)
            {
                var result = new List<PropertyHashedObjectDefinition>();
                
                //process IfcTypeObjects
                var types = model.Instances.OfType<IfcTypeObject>();
                foreach (var type in types)
                {
                    if (type.HasPropertySets == null) continue;
                    int hash = 0;
                    foreach (var pSet in type.HasPropertySets.OfType<IfcPropertySet>())
                        hash += pSet.GetPSetHash();
                    result.Add(new PropertyHashedObjectDefinition(type, hash));
                }

                //process IfcObjects
                var objs = model.Instances.OfType<IfcObject>();
                var cache = new Dictionary<IfcObject, int?>();
                //init cache with null hases
                foreach (var obj in objs)
                    cache.Add(obj, null);
                var rels = model.Instances.OfType<IfcRelDefinesByProperties>();
                foreach (var rel in rels)
                {
                    var pSet = rel.RelatingPropertyDefinition as IfcPropertySet;
                    if (pSet == null) continue;
                    var pSetHash = pSet.GetPSetHash();
                    foreach (var o in rel.RelatedObjects)
                    {
                        if (cache[o] == null)
                            cache[o] = pSetHash;
                        else
                            cache[o] += pSetHash;
                    }
                }
                //turn cache to the result
                foreach (var o in cache.Keys)
                    if (cache[o] != null)
                        result.Add(new PropertyHashedObjectDefinition(o, (int)cache[o]));

                return result;
            }

            #region Hash and Euals overrides
            public override int GetHashCode()
            {
                return _hash;
            }

            public override bool Equals(object obj)
            {
                //check type compatibility
                var objDef = obj as PropertyHashedObjectDefinition;
                if (objDef == null) return false;

                //do the proper equality comparison based on the properties.
                IEnumerable<IfcPropertySet> set1 = null;
                IEnumerable<IfcPropertySet> set2 = null;

                var o2 = objDef.IfcObjectDefinition as IfcObject;
                var o1 = _objDef as IfcObject;
                if (o2 != null && o1 != null)
                {
                    set1 = o1.PropertySets;
                    set2 = o2.PropertySets;
                }

                var t1 = _objDef as IfcTypeObject;
                var t2 = objDef._objDef as IfcTypeObject;
                if (t1 != null && t2 != null)
                {
                    set1 = t1.PropertySets;
                    set2 = t2.PropertySets;
                }

                if (set1 != null && set2 != null)
                    return ComparePsetsSet(set1, set2);
                else
                    return false;

            }

            private bool ComparePSets(IfcPropertySet baseline, IfcPropertySet revision)
            {
                if (baseline.Name != revision.Name)
                    return false;
                if (baseline.HasProperties.Count != revision.HasProperties.Count)
                    return false;
                foreach (var prop in baseline.HasProperties)
                    if (!HasEquivalent(prop, revision))
                        return false;
                return true;

            }

            private bool HasEquivalent(IfcProperty property, IfcPropertySet revisionPset)
            {
                //check if property with the same name even exist
                var candidate = revisionPset.HasProperties.Where(p => p.Name == property.Name).FirstOrDefault();
                if (candidate == null)
                    return false;

                //check actual value
                switch (property.GetType().Name)
                {
                    case "IfcPropertySingleValue":
                        var single = candidate as IfcPropertySingleValue;
                        if (single == null) return false;
                        var revVal = single.NominalValue;
                        var baseVal = ((IfcPropertySingleValue)(property)).NominalValue;
                        var revStr = revVal == null ? "" : revVal.ToString();
                        var baseStr = baseVal == null ? "" : baseVal.ToString();
                        if (baseStr != revStr)
                            return false;
                        break;
                    case "IfcPropertyEnumeratedValue":
                        var enumerated = candidate as IfcPropertyEnumeratedValue;
                        if (enumerated == null) return false;
                        var baseEnum = property as IfcPropertyEnumeratedValue;
                        if (baseEnum.EnumerationValues.Count != enumerated.EnumerationValues.Count)
                            return false;
                        foreach (var e in baseEnum.EnumerationValues)
                            if (!enumerated.EnumerationValues.Contains(e))
                                return false;
                        break;
                    case "IfcPropertyBoundedValue":
                        var bounded = candidate as IfcPropertyBoundedValue;
                        if (bounded == null) return false;
                        var baseBounded = property as IfcPropertyBoundedValue;
                        if (bounded.LowerBoundValue != baseBounded.LowerBoundValue)
                            return false;
                        if (baseBounded.UpperBoundValue != bounded.UpperBoundValue)
                            return false;
                        break;
                    case "IfcPropertyTableValue":
                        var table = candidate as IfcPropertyTableValue;
                        if (table == null) return false;
                        var baseTable = property as IfcPropertyTableValue;
                        if (baseTable.DefiningValues.Count != table.DefiningValues.Count)
                            return false;
                        //check all table items
                        foreach (var item in baseTable.DefiningValues)
                        {
                            var revDefiningValue = table.DefiningValues.Where(v => v.ToString() == item.ToString()).FirstOrDefault();
                            if (revDefiningValue == null) return false;
                            var revIndex = table.DefiningValues.IndexOf(revDefiningValue);
                            var baseIndex = baseTable.DefiningValues.IndexOf(item);
                            if (table.DefinedValues[revIndex].ToString() != baseTable.DefinedValues[baseIndex].ToString())
                                return false;
                        }
                        break;
                    case "IfcPropertyReferenceValue":
                        var reference = candidate as IfcPropertyReferenceValue;
                        if (reference == null) return false;
                        var baseRef = property as IfcPropertyReferenceValue;
                        if (reference.UsageName != baseRef.UsageName)
                            return false;
                        if (reference.PropertyReference.GetType() != baseRef.PropertyReference.GetType())
                            return false;
                        //should go deeper but it would be too complicated for now
                        break;
                    case "IfcPropertyListValue":
                        var list = candidate as IfcPropertyListValue;
                        if (list == null) return false;
                        var baseList = property as IfcPropertyListValue;
                        if (baseList.ListValues.Count != list.ListValues.Count)
                            return false;
                        foreach (var item in baseList.ListValues)
                            if (!list.ListValues.Contains(item))
                                return false;
                        break;
                    default:
                        break;
                }
                return true;
            }

            private bool ComparePsetsSet(IEnumerable<IfcPropertySet> baseline, IEnumerable<IfcPropertySet> revision)
            {
                if (baseline.Count() != revision.Count())
                    return false;
                var baseList = baseline.ToList();
                var revisionList = revision.ToList();
                baseList.Sort(psetsOrder);
                revisionList.Sort(psetsOrder);

                for (int i = 0; i < baseList.Count; i++)
                    if (!ComparePSets(baseList[i], revisionList[i]))
                        return false;
                return true;
            }

            private static int psetsOrder(IfcPropertySet x, IfcPropertySet y)
            {
                if (x.Name == null && y.Name != null) return -1;
                if (x.Name != null && y.Name == null) return 1;
                if (x.Name == null && y.Name == null) return 0;

                return x.Name.ToString().CompareTo(y.Name.ToString());
            }
            #endregion
        }

        

        #endregion
    }
}
