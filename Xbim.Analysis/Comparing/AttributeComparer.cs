using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc2x3.IO;
using Xbim.Ifc2x3.Kernel;

namespace Xbim.Analysis.Comparing
{
    public class AttributeComparer : IModelComparerII
    {
        private readonly string _attrName;
        private XbimModel _revModel;
        private readonly IEnumerable<Type> _possibleTypes;
        private readonly HashSet<AttributeHasedRoot> _cache = new HashSet<AttributeHasedRoot>();

        public AttributeComparer(string attributeName, XbimModel revisedModel)
        {
            _attrName = attributeName;
            _revModel = revisedModel;

            //get possible types
            var rootType = IfcMetaData.IfcType(typeof(IfcRoot));
            var rootSubTypes = rootType.NonAbstractSubTypes;
            _possibleTypes = rootSubTypes.Where(t => IsSimpleValueAttribute(t, attributeName));

            //get possible objects
            var possibleObjects = revisedModel.Instances.Where<IfcRoot>(r => _possibleTypes.Contains(r.GetType()));
            foreach (var obj in possibleObjects)
            {
                var inf = obj.GetType().GetProperty(attributeName);
                var val = inf.GetValue(obj, null);
                if (val != null)
                    _cache.Add(new AttributeHasedRoot(obj, (IfcSimpleValue)val));
            }
        }

        private bool IsSimpleValueAttribute(Type type, string attrName)
        {
            var propInf = type.GetProperty(attrName);
            if (propInf == null) return false;
            var propType = propInf.PropertyType;

            var simple = typeof(IfcSimpleValue);
            var nonNulType = Nullable.GetUnderlyingType(propType);
            if (nonNulType != null)
                return simple.IsAssignableFrom(nonNulType);
            else
                return simple.IsAssignableFrom(propType);
        }

        Type GetNullableType(Type type)
        {
            // Use Nullable.GetUnderlyingType() to remove the Nullable<T> wrapper if type is already nullable.
            //type = Nullable.GetUnderlyingType(type);
            //if (type.IsValueType)
                return typeof(Nullable<>).MakeGenericType(type);
            //else
            //    return type;
        }

        #region Model comparer implementation
        public string Name
        {
            get { return "Xbim Attribute Comparer"; }
        }

        public string Description
        {
            get { return "Comparer arbitrary simple value attribute of the object using reflection."; }
        }

        public ComparisonType ComparisonType
        {
            get { return Comparing.ComparisonType.CUSTOM; }
        }

        private int _weight = 30;
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
        public ComparisonResult Compare<T>(T baseline, XbimModel revisedModel) where T : Ifc2x3.Kernel.IfcRoot
        {
            if (!_possibleTypes.Contains(typeof(T))) 
                return null;
            var val = baseline.GetType().GetProperty(_attrName).GetValue(baseline, null);
            if (val == null)
                return null;

            var result = new ComparisonResult(baseline, this);
            var hashed = new AttributeHasedRoot(baseline, (IfcSimpleValue)val);
            foreach (var item in _cache.Where(r => r.GetHashCode() == hashed.GetHashCode()))
            {
                result.Candidates.Add(item.Root);
                _processed.Add(item.Root);
            }
            return result;
        }

        public ComparisonResult GetResidualsFromRevision<T>(XbimModel revisedModel) where T : Ifc2x3.Kernel.IfcRoot
        {
            var result = new ComparisonResult(null, this);
            var isNotProcessed = new Func<IfcRoot, bool>(r => { return !_processed.Contains(r); });
            var isInCache = new Func<IfcRoot, bool>(r => { return _cache.Where(c => c.Root == r).FirstOrDefault() != null; });
            result.Candidates.AddRange(revisedModel.Instances.Where<T>(r => isNotProcessed(r) && isInCache(r)));
            return result;
        }

        public IEnumerable<ComparisonResult> Compare<T>(XbimModel baseline, XbimModel revised) where T : Ifc2x3.Kernel.IfcRoot
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

        private class AttributeHasedRoot
        {
            private IfcSimpleValue _val;
            private IfcRoot _root;
            public IfcRoot Root { get { return _root; } }
            int _hash;
            public AttributeHasedRoot(IfcRoot root, IfcSimpleValue value)
            {
                _root = root;
                _val = value;
                _hash = value.ToString().GetHashCode();
            }

            public override int GetHashCode()
            {
                return _hash;
            }
        }
    }
}
