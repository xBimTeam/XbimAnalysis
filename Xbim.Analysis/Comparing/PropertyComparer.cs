using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Analysis.Extensions;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;

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

        private List<IIfcRoot> _processed = new List<IIfcRoot>();
        public ComparisonResult Compare<T>(T baseline, IModel revisedModel) where T : IIfcRoot
        {
            var baseModel = baseline.Model;
            if (baseModel == revisedModel)
                throw new ArgumentException("Baseline should be from the different model than revised model.");

            //this comparison makes a sense only for IIfcObjectDefinition and it's descendants
            var objDef = baseline as IIfcObjectDefinition;
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

        public ComparisonResult GetResidualsFromRevision<T>(IModel revisedModel) where T : IIfcRoot
        {
            var result = new ComparisonResult(null, this);
            var isInCache = new Func<IIfcRoot, bool>(r => { return _cacheRevision.Where(c => c.IfcObjectDefinition == r).FirstOrDefault() != null; });
            var isNotProcessed = new Func<IIfcRoot, bool>(r => { return !_processed.Contains(r); });
            result.Candidates.AddRange(revisedModel.Instances.OfType<IIfcRoot>().Where(r => isNotProcessed(r) && isInCache(r)));
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

        #endregion

        #region Helpers


        private class PropertyHashedObjectDefinition
        {
            private int _hash;
            private IIfcObjectDefinition _objDef;
            public IIfcObjectDefinition IfcObjectDefinition { get { return _objDef; } }

            private PropertyHashedObjectDefinition(IIfcObjectDefinition objDef, int hash)
            {
                _objDef = objDef;
                _hash = hash;
            }

            //public PropertyHashedObjectDefinition(IIfcObjectDefinition objDef)
            //{
            //    _objDef = objDef;
            //    IEnumerable<IIfcPropertySet> pSets = null;
            //    var o = objDef as IIfcObject;
            //    if (o != null)
            //        pSets = o.GetAllPropertySets();
            //    var t = objDef as IIfcTypeObject;
            //    if (t != null)
            //        pSets = t.DefinedByProperties();

            //    _hash = 0;
            //    foreach (var pSet in pSets)
            //        _hash += pSet.GetPSetHash();
            //}

            /// <summary>
            /// This is more efficient than doing the same object by object as it
            /// doesn't traverse relations more times.
            /// </summary>
            /// <param name="model">Model to be used</param>
            /// <returns>List of hashed object definitions</returns>
            public static List<PropertyHashedObjectDefinition> CreateFrom(IModel model)
            {
                var result = new List<PropertyHashedObjectDefinition>();
                
                //process IIfcTypeObjects
                var types = model.Instances.OfType<IIfcTypeObject>();
                foreach (var type in types)
                {
                    if (type.HasPropertySets == null) continue;
                    int hash = 0;
                    foreach (var pSet in type.HasPropertySets.OfType<IIfcPropertySet>())
                        hash += pSet.GetPSetHash();
                    result.Add(new PropertyHashedObjectDefinition(type, hash));
                }

                //process IIfcObjects
                var objs = model.Instances.OfType<IIfcObject>();
                var cache = new Dictionary<IIfcObjectDefinition, int?>();
                //init cache with null hases
                foreach (var obj in objs)
                    cache.Add(obj, null);
                var rels = model.Instances.OfType<IIfcRelDefinesByProperties>();
                foreach (var rel in rels)
                {
                    var pSet = rel.RelatingPropertyDefinition as IIfcPropertySet;
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
                IEnumerable<IIfcPropertySet> set1 = null;
                IEnumerable<IIfcPropertySet> set2 = null;

                var o2 = objDef.IfcObjectDefinition as IIfcObject;
                var o1 = _objDef as IIfcObject;
                if (o2 != null && o1 != null)
                {
                    set1 = o1.GetAllPropertySets();
                    set2 = o2.GetAllPropertySets();
                }

                var t1 = _objDef as IIfcTypeObject;
                var t2 = objDef._objDef as IIfcTypeObject;
                if (t1 != null && t2 != null)
                {
                    set1 = t1.GetAllPropertySets();
                    set2 = t2.GetAllPropertySets();
                }

                if (set1 != null && set2 != null)
                    return ComparePsetsSet(set1, set2);
                else
                    return false;

            }

            private bool ComparePSets(IIfcPropertySet baseline, IIfcPropertySet revision)
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

            private bool HasEquivalent(IIfcProperty property, IIfcPropertySet revisionPset)
            {
                //check if property with the same name even exist
                var candidate = revisionPset.HasProperties.Where(p => p.Name == property.Name).FirstOrDefault();
                if (candidate == null)
                    return false;

                //check actual value
                switch (property.GetType().Name)
                {
                    case "IfcPropertySingleValue":
                        var single = candidate as IIfcPropertySingleValue;
                        if (single == null) return false;
                        var revVal = single.NominalValue;
                        var baseVal = ((IIfcPropertySingleValue)(property)).NominalValue;
                        var revStr = revVal == null ? "" : revVal.ToString();
                        var baseStr = baseVal == null ? "" : baseVal.ToString();
                        if (baseStr != revStr)
                            return false;
                        break;
                    case "IfcPropertyEnumeratedValue":
                        var enumerated = candidate as IIfcPropertyEnumeratedValue;
                        if (enumerated == null) return false;
                        var baseEnum = property as IIfcPropertyEnumeratedValue;
                        if (baseEnum.EnumerationValues.Count != enumerated.EnumerationValues.Count)
                            return false;
                        foreach (var e in baseEnum.EnumerationValues)
                            if (!enumerated.EnumerationValues.Contains(e))
                                return false;
                        break;
                    case "IfcPropertyBoundedValue":
                        var bounded = candidate as IIfcPropertyBoundedValue;
                        if (bounded == null) return false;
                        var baseBounded = property as IIfcPropertyBoundedValue;
                        if (bounded.LowerBoundValue != baseBounded.LowerBoundValue)
                            return false;
                        if (baseBounded.UpperBoundValue != bounded.UpperBoundValue)
                            return false;
                        break;
                    case "IfcPropertyTableValue":
                        var table = candidate as IIfcPropertyTableValue;
                        if (table == null) return false;
                        var baseTable = property as IIfcPropertyTableValue;
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
                        var reference = candidate as IIfcPropertyReferenceValue;
                        if (reference == null) return false;
                        var baseRef = property as IIfcPropertyReferenceValue;
                        if (reference.UsageName != baseRef.UsageName)
                            return false;
                        if (reference.PropertyReference.GetType() != baseRef.PropertyReference.GetType())
                            return false;
                        //should go deeper but it would be too complicated for now
                        break;
                    case "IfcPropertyListValue":
                        var list = candidate as IIfcPropertyListValue;
                        if (list == null) return false;
                        var baseList = property as IIfcPropertyListValue;
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

            private bool ComparePsetsSet(IEnumerable<IIfcPropertySet> baseline, IEnumerable<IIfcPropertySet> revision)
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

            private static int psetsOrder(IIfcPropertySet x, IIfcPropertySet y)
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
