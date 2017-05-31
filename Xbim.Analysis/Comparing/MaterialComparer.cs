using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Analysis.Extensions;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;

namespace Xbim.Analysis.Comparing
{
    public class MaterialComparer : IModelComparerII
    {
        private IModel _model;
        private List<MaterialHash> _cache = new List<MaterialHash>();

        public MaterialComparer(IModel revisedModel)
        {
            _model = revisedModel;
            var roots = revisedModel.Instances.OfType<IfcObjectDefinition>();
            foreach (var root in roots)
            {
                var material = root.Material;
                if (material != null)
                _cache.Add(new MaterialHash(root, material));
            }
        }

        #region Comparer implementation
        public string Name
        {
            get { return "Xbim Material Comparer"; }
        }

        public string Description
        {
            get { return "This comparer compares objects based on their material composition."; }
        }

        public ComparisonType ComparisonType
        {
            get { return Comparing.ComparisonType.MATERIAL; }
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
            if (!(baseline is IfcObjectDefinition))
                return null;

            var baseObj = baseline as IfcObjectDefinition;
            var matSel = baseObj.Material;
            if (matSel == null)
                return null;

            var result = new ComparisonResult(baseline, this);
            var matHashed = new MaterialHash(baseline, matSel);
            var hashes = _cache.Where(m => m.GetHashCode() == matHashed.GetHashCode());
            foreach (var h in hashes)
            {
                if ((h.Root is IfcObject && baseline is IfcObject) || (h.Root is IIfcTypeObject && baseline is IIfcTypeObject))
                {
                    result.Candidates.Add(h.Root);
                    _processed.Add(h.Root);
                }
            }
            return result;
        }

        public ComparisonResult GetResidualsFromRevision<T>(IModel revisedModel) where T : IIfcRoot
        {
            var result = new ComparisonResult(null, this);
            var isInCache = new Func<IIfcRoot, bool>(r => { return _cache.Where(c => c.Root == r).FirstOrDefault() != null; });
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

        private class MaterialHash
        {
            private int _hash;
            private IIfcMaterialSelect _material;
            private IIfcRoot _root;

            public IIfcMaterialSelect Material { get { return _material; } }
            public IIfcRoot Root { get { return _root; } }

            public MaterialHash(IIfcRoot root, IIfcMaterialSelect material)
            {
                _root = root;
                _material = material;
                _hash = _material.CreateHashCode();
            }

            public override int GetHashCode()
            {
                return _hash;
            } 
        }
    }
}
