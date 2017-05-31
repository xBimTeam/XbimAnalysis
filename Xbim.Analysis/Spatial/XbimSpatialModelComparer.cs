using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common.Geometry;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions.Interfaces;
namespace Xbim.Analysis.Spatial
{
    public class XbimSpatialModelComparer
    {
        private XbimOctree<IfcProduct> _tree;
        public XbimOctree<IfcProduct> Octree { get { return _tree; } }

        private Dictionary<IfcProduct, XbimRect3D> _prodBBsA = new Dictionary<IfcProduct, XbimRect3D>();
        private Dictionary<IfcProduct, XbimRect3D> _prodBBsB = new Dictionary<IfcProduct, XbimRect3D>();

        private XbimProductVersionComparison _comparison = new XbimProductVersionComparison();
        private HashSet<IfcProduct> _processedFromB = new HashSet<IfcProduct>();
        
        private IModel _modelA;
        private IModel _modelB;

        private double _precision;
        
//        public XbimSpatialModelComparer(IModel modelA, IModel modelB)
//        {
//            if (modelA == null || modelB == null) 
//                throw new ArgumentNullException();

//            _modelA = modelA;
//            _modelB = modelB;

//            //check if all the model use the same units
//            var meter = modelA.ModelFactors.OneMetre;
//            _precision = modelA.ModelFactors.Precision;
//            if (Math.Abs(modelB.ModelFactors.OneMetre - meter) > 1e-9)
//                throw new ArgumentException("All models have to use the same length units.");
//            if (Math.Abs(modelB.ModelFactors.Precision - _precision) > 1e-9)
//                throw new ArgumentException("All models have to use the same precision.");

//            //get axis aligned BBoxes and overall world size
//            XbimRect3D worldBB = XbimRect3D.Empty;
//            foreach (var model in new[] { modelA, modelB })
//            //Parallel.ForEach<IModel>(new[] { modelA, modelB }, model =>
//            {
//                //generate geometry if there is no in the model
//                if (model.GeometriesCount == 0)
//                {
//                    var context = new Xbim3DModelContext(model);
//                    context.CreateContext();
//                }
//                foreach (var prod in model.IfcProducts.Cast<IfcProduct>())
//                {
//                    XbimRect3D prodBox = XbimRect3D.Empty;
//                    foreach (var shp in context.ShapeInstancesOf(prod))
//                    {

//                        //we need to preprocess all the products first to get the world size. Will keep results to avoid repetition.

//                        //bounding boxes are lightweight and are produced when geometry is created at first place
//                        //var geom = prod.Geometry3D();
//                        //var trans = prod.Transform();

//                        //  if (geom != null && geom.FirstOrDefault() != null)
//                        //  {
//                        //var mesh = geom.Mesh(model.ModelFactors.DeflectionTolerance);
//                        //var bb = mesh.Bounds;

//                        //Axis aligned BBox
//                        var bb = shp.BoundingBox;//.GetAxisAlignedBoundingBox();
//                        bb = XbimRect3D.TransformBy(bb, shp.Transformation);
//                        if (prodBox.IsEmpty) prodBox = bb; else prodBox.Union(bb);
//                        //bb = bb.Transform(trans);
//                        // bb = new XbimRect3D(trans.Transform(bb.Min), trans.Transform(bb.Max));


//#if DEBUG
//                        //System.Diagnostics.Debug.WriteLine("{0,-45} {1,10:F5} {2,10:F5} {3,10:F5} {4,10:F5} {5,10:F5} {6,10:F5}",
//                        //                                    prod.Name, bb.X, bb.Y, bb.Z, bb.SizeX, bb.SizeY, bb.SizeZ);
//#endif

                      
//                        // }

//                    }
//                    if (model == modelA)
//                        _prodBBsA.Add(prod, prodBox);
//                    else
//                        _prodBBsB.Add(prod, prodBox);

//                    //add every BBox to the world to get the size and position of the world
//                    //if it contains valid BBox
//                    if (!double.IsNaN(prodBox.SizeX))
//                        worldBB.Union(prodBox);
//                }
//            }
//            //);

//            //create octree
//            //target size must depend on the units of the model
//            //size inflated with 0.5 meter so that there are not that many products on the boundary of the world
//            var size = Math.Max(Math.Max(worldBB.SizeX, worldBB.SizeY), worldBB.SizeZ) + (float)meter / 2f;
//            var shift = (float)meter / 4f;
//            var position = worldBB.Location + new XbimVector3D() { X = size / 2f - shift, Y = size / 2f - shift, Z = size / 2f - shift };
//            _tree = new XbimOctree<IfcProduct>(size, (float)meter, 1f, position);

//            //add every product and its AABBox to octree
//            foreach (var item in _prodBBsA)
//                _tree.Add(item.Key, item.Value);
//            foreach (var item in _prodBBsB)
//                _tree.Add(item.Key, item.Value);

//            //process candidates for a closer evaluation
//            ProcessCandidates();

//            //precise evaluation should follow here
//        }

        public int CountProductsFromA { get { return _prodBBsA.Count; } }
        public int CountProductsFromB { get { return _prodBBsB.Count; } }

        public XbimProductVersionComparison Comparison { get { return _comparison; } }

        private void ProcessCandidates()
        {
            //get products from the model A as a base set
            foreach (var product in _prodBBsA.Keys)
            {
                var node = _tree.Find(product);
#if DEBUG
                var candidates = GetCandidatesFromNode(product, node).ToList();
#else
                var candidates = GetCandidatesFromNode(product, node);
#endif
                var version = new XbimProductVersion() { Old = product};
                foreach (var candidate in candidates)
                    version.New.Add(candidate);
                
                _comparison.Add(version);
            }

            //add to comparison all products from B which are not candidates for anything in A
            foreach (var product in _prodBBsB.Keys)
            {
                if (!_processedFromB.Contains(product))
                {
                    var version = new XbimProductVersion() {};
                    version.New.Add(product);
                    _comparison.Add(version);
                }
            }
        }

        private IEnumerable<IfcProduct> GetCandidatesFromNode(IfcProduct original, XbimOctree<IfcProduct> node)
        {
            if (original != null && node != null)
            {
                //content which if from other models
                var nodeContent = node.Content().Where(nc => (nc.ModelOf != original.ModelOf) && (nc.GetType() == original.GetType()));
                var prodBBox = XbimRect3D.Empty;

                foreach (var candidate in nodeContent)
                {
                    //check BBoxes for equality
                    var contBBox = _prodBBsB[candidate];
                    prodBBox = _prodBBsA[original];
                    if (XbimAABBoxAnalyser.AlmostEqual(contBBox, prodBBox, _precision))
                    {
                        if (!_processedFromB.Contains(candidate))
                            _processedFromB.Add(candidate);
                        yield return candidate;
                    }

                    //cope with the situation when product's bbox is on the border and 
                    //it's equivalent from the second model might have end up on the higher level
                    var borderDirections = BBoxBorderDirections(node.Bounds, prodBBox);
                    var parents = GetParentsInDirections(borderDirections, node);
                    foreach (var parent in parents)
                        foreach (var item in GetCandidatesFromNode(original, parent))//recursion
                        {
                            if (!_processedFromB.Contains(candidate))
                                _processedFromB.Add(candidate);
                            yield return candidate;
                        }
                }
            }
        }

        private HashSet<XbimOctree<IfcProduct>> GetParentsInDirections(IEnumerable<XbimDirectionEnum> directions, XbimOctree<IfcProduct> node)
        {
            HashSet<XbimOctree<IfcProduct>> result = new HashSet<XbimOctree<IfcProduct>>();
            foreach (var direction in directions)
            {
                var parent = node.GetCommonParentInDirection(direction, false);
                if (parent != null && !result.Contains(parent))
                    result.Add(parent);
            }
            return result;
        }

        private IEnumerable<XbimDirectionEnum> BBoxBorderDirections(XbimRect3D outer, XbimRect3D inner)
        {
            if ((inner.Min.X - outer.Min.X) < _precision) yield return XbimDirectionEnum.WEST;
            if ((inner.Min.Y - outer.Min.Y) < _precision) yield return XbimDirectionEnum.SOUTH;
            if ((inner.Min.Z - outer.Min.Z) < _precision) yield return XbimDirectionEnum.DOWN;
            if ((outer.Max.X - inner.Max.X) < _precision) yield return XbimDirectionEnum.EAST;
            if ((outer.Max.Y - inner.Max.Y) < _precision) yield return XbimDirectionEnum.NORTH;
            if ((outer.Max.Z - inner.Max.Z) < _precision) yield return XbimDirectionEnum.UP;
        }
    }

    public class XbimProductVersion
    {
        private IfcProduct _old;
        private List<IfcProduct> _new = new List<IfcProduct>();

        public IfcProduct Old { get { return _old; } set { _old = value; } }
        public IList<IfcProduct> New { get { return _new; } }

        public bool HasOldVersion { get { return _old != null; } }
        public bool HasNewVersion { get { return _new.Count > 0; } }
        public bool HasMoreNewVersions { get { return _new.Count > 1; } }
        public bool IsOneToOne { get { return HasOldVersion && _new.Count == 1; } }
    }

    public class XbimProductVersionComparison : List<XbimProductVersion>
    {
        public IEnumerable<IfcProduct> OnlyInOld
        {
            get
            {
                foreach (var item in this.Where(i => !i.HasNewVersion))
                {
                    yield return item.Old;
                }
            }
        }

        public IEnumerable<IfcProduct> OnlyInNew
        {
            get
            {
                HashSet<IfcProduct> unique = new HashSet<IfcProduct>();
                foreach (var item in this.Where(i => !i.HasOldVersion))
                {
                    foreach (var p in item.New)
                    {
                        if (!unique.Contains(p))
                        {
                            yield return p;
                            unique.Add(p);
                        }
                    }
                }
            }
        }

        public IDictionary<IfcProduct, IfcProduct> MatchOneToOne
        {
            get 
            {
                var result = new Dictionary<IfcProduct, IfcProduct>();
                foreach (var item in this.Where(i => i.IsOneToOne))
                {
                    result.Add(item.Old, item.New.First());
                }
                return result;
            }
        }

        public IEnumerable<XbimProductVersion> HasMoreNewVersions
        {
            get 
            {
                foreach (var item in this.Where(i => i.HasMoreNewVersions))
                {
                    yield return item;
                }
            }
        }

        public bool IsModelConsistent()
        {
            IModel oldModel = null;
            IModel newModel = null ;
            foreach (var item in this)
            {
                if (oldModel == null && item.Old != null)
                    oldModel = item.Old.ModelOf;
                if (item.Old != null)
                    if (oldModel != item.Old.ModelOf)
                        return false;
                foreach (var p in item.New)
                {
                    if (p == null) continue;
                    if (newModel == null)
                        newModel = p.ModelOf;
                    if (p.ModelOf != newModel)
                        return false;
                }
            }

            if (newModel == oldModel) 
                return false;

            return true;
        }
    }
}
