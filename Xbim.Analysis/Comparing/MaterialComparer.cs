using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Analysis.Extensions;
using Xbim.Ifc2x3.IO;
using Xbim.Ifc2x3.Kernel;

namespace Xbim.Analysis.Comparing
{
    public class MaterialComparer : IModelComparerII
    {
        private XbimModel _model;
        private List<MaterialHash> _cache = new List<MaterialHash>();

        public MaterialComparer(XbimModel revisedModel)
        {
            _model = revisedModel;
            var roots = revisedModel.Instances.OfType<IfcRoot>();
            foreach (var root in roots)
            {
                var material = root.GetMaterial();
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

        private HashSet<IfcRoot> _processed = new HashSet<IfcRoot>();

        public ComparisonResult Compare<T>(T baseline, IO.XbimModel revisedModel) where T : Ifc2x3.Kernel.IfcRoot
        {
            var matSel = baseline.GetMaterial();
            if (matSel == null)
                return null;

            var result = new ComparisonResult(baseline, this);
            var matHashed = new MaterialHash(baseline, matSel);
            var hashes = _cache.Where(m => m.GetHashCode() == matHashed.GetHashCode());
            foreach (var h in hashes)
            {
                if ((h.Root is IfcObject && baseline is IfcObject) || (h.Root is IfcTypeObject && baseline is IfcTypeObject))
                {
                    result.Candidates.Add(h.Root);
                    _processed.Add(h.Root);
                }
            }
            return result;
        }

        public ComparisonResult GetResidualsFromRevision<T>(IO.XbimModel revisedModel) where T : Ifc2x3.Kernel.IfcRoot
        {
            var result = new ComparisonResult(null, this);
            var isInCache = new Func<IfcRoot, bool>(r => { return _cache.Where(c => c.Root == r).FirstOrDefault() != null; });
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

        private class MaterialHash
        {
            private int _hash;
            private IfcMaterialSelect _material;
            private IfcRoot _root;

            public IfcMaterialSelect Material { get { return _material; } }
            public IfcRoot Root { get { return _root; } }

            public MaterialHash(IfcRoot root, IfcMaterialSelect material)
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
