using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;

namespace Xbim.Analysis.Comparing
{
    public class AttributeComparer : IModelComparerII
    {
        private readonly string _attrName;
        private IModel _revModel;
        private readonly IEnumerable<Type> _possibleTypes;
        private readonly HashSet<AttributeHasedRoot> _cache = new HashSet<AttributeHasedRoot>();

        public AttributeComparer(string attributeName, IModel revisedModel)
        {
            _attrName = attributeName;
            _revModel = revisedModel;

            //get possible types
            var rootType = IfcMetaData.IfcType(typeof(IIfcRoot));
            var rootSubTypes = rootType.NonAbstractSubTypes;
            _possibleTypes = rootSubTypes.Where(t => IsSimpleValueAttribute(t, attributeName));

            //get possible objects
            var possibleObjects = revisedModel.Instances.Where<IIfcRoot>(r => _possibleTypes.Contains(r.GetType()));
            foreach (var obj in possibleObjects)
            {
                var inf = obj.GetType().GetProperty(attributeName);
                var val = inf.GetValue(obj, null);
                if (val != null)
                    _cache.Add(new AttributeHasedRoot(obj, (IIfcSimpleValue)val));
            }
        }

        private bool IsSimpleValueAttribute(Type type, string attrName)
        {
            var propInf = type.GetProperty(attrName);
            if (propInf == null) return false;
            var propType = propInf.PropertyType;

            var simple = typeof(IIfcSimpleValue);
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

        private HashSet<IIfcRoot> _processed = new HashSet<IIfcRoot>();
        public ComparisonResult Compare<T>(T baseline, IModel revisedModel) where T : IIfcRoot
        {
            if (!_possibleTypes.Contains(typeof(T))) 
                return null;
            var val = baseline.GetType().GetProperty(_attrName).GetValue(baseline, null);
            if (val == null)
                return null;

            var result = new ComparisonResult(baseline, this);
            var hashed = new AttributeHasedRoot(baseline, (IIfcSimpleValue)val);
            foreach (var item in _cache.Where(r => r.GetHashCode() == hashed.GetHashCode()))
            {
                result.Candidates.Add(item.Root);
                _processed.Add(item.Root);
            }
            return result;
        }

        public ComparisonResult GetResidualsFromRevision<T>(IModel revisedModel) where T : IIfcRoot
        {
            var result = new ComparisonResult(null, this);
            var isNotProcessed = new Func<IIfcRoot, bool>(r => { return !_processed.Contains(r); });
            var isInCache = new Func<IIfcRoot, bool>(r => { return _cache.Where(c => c.Root == r).FirstOrDefault() != null; });
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

        private class AttributeHasedRoot
        {
            private IIfcSimpleValue _val;
            private IIfcRoot _root;
            public IIfcRoot Root { get { return _root; } }
            int _hash;
            public AttributeHasedRoot(IIfcRoot root, IIfcSimpleValue value)
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
