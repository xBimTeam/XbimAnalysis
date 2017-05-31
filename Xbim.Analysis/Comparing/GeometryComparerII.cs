using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common.Geometry;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Analysis.Comparing
{
    public class GeometryComparerII : IModelComparerII
    {
        private XbimOctree<IfcProduct> _tree;
        public XbimOctree<IfcProduct> Octree { get { return _tree; } }

        private Dictionary<IfcProduct, XbimRect3D> _prodBBsA = new Dictionary<IfcProduct, XbimRect3D>();
        private Dictionary<IfcProduct, XbimRect3D> _prodBBsB = new Dictionary<IfcProduct, XbimRect3D>();

        private XbimProductVersionComparison _comparison = new XbimProductVersionComparison();
        private HashSet<IfcProduct> _processedFromB = new HashSet<IfcProduct>();

        private IModel _baselineModel;
        private IModel _revisedModel;

        private Xbim3DModelContext _baselineContext;
        private Xbim3DModelContext _revisedContext;

        private double _precision;
        private double _meter;

        public GeometryComparerII(IModel baselineModel, IModel revisedModel)
        {
            //Martin needs to be re-engineered for new Geometry

            if (baselineModel == null || revisedModel == null)
                throw new ArgumentNullException();

            _baselineModel = baselineModel;
            _revisedModel = revisedModel;

            //check if all the model use the same units
            _meter = baselineModel.ModelFactors.OneMetre;
            _precision = baselineModel.ModelFactors.Precision;
            if (Math.Abs(revisedModel.ModelFactors.OneMetre - _meter) > 1e-9)
                throw new ArgumentException("All models have to use the same length units.");
            //if (Math.Abs(revisedModel.ModelFactors.Precision - _precision) > 1e-9)
            //    throw new ArgumentException("All models have to use the same precision.");

            if (revisedModel.ModelFactors.Precision > _precision)
                _precision = revisedModel.ModelFactors.Precision;

            //create geometry context
            _baselineContext = new Xbim3DModelContext(_baselineModel);
            _revisedContext = new Xbim3DModelContext(_revisedModel);

            //get axis aligned BBoxes and overall world size
            XbimRect3D worldBB = XbimRect3D.Empty;

            foreach (var context in new []{_revisedContext, _baselineContext})
            {
                if (!context.CreateContext())
                    throw new Exception("Geometry context not created.");
                foreach (var shpInst in context.ShapeInstances())
                {
                    var bBox = shpInst.BoundingBox;
                    var product = context.Model.Instances.Where<IfcProduct>(p => p.EntityLabel == shpInst.IfcProductLabel).FirstOrDefault();
                    if (product == null)
                        throw new Exception("Product not defined.");
                    if (context == _baselineContext)
                        _prodBBsA.Add(product, bBox);
                    else
                        _prodBBsB.Add(product, bBox);

                    //add every BBox to the world to get the size and position of the world
                    //if it contains valid BBox
                    if (!double.IsNaN(bBox.SizeX))
                        worldBB.Union(bBox);
                }
            }

            ////foreach (var model in new[] { baselineModel, revisedModel })
            //Parallel.ForEach<IModel>(new[] { baselineModel, revisedModel }, model =>
            //{
            //    //load geometry engine using local path
            //    if (model.GeometriesCount == 0)
            //    {
            //        //load geometry engine if it is not loaded yet
            //        string basePath = Path.GetDirectoryName(GetType().Assembly.Location);
            //        AssemblyResolver.GetModelGeometryAssembly(basePath);
            //    }

            //    //initialize octree with all the objects
            //    Xbim3DModelContext context = new Xbim3DModelContext(model);
            //    context.CreateContext();
            //    var prodShapes = context.ProductShapes;

            //    if (model == baselineModel)
            //        _baselineContext = context;
            //    else
            //        _revisedContext = context;

            //    //we need to preprocess all the products first to get the world size. Will keep results to avoid repetition.
            //    foreach (var shp in prodShapes)
            //    {
            //        //bounding boxes are lightweight and are produced when geometry is created at first place
            //        var bb = shp.BoundingBox;

            //        if (shp.Product.ModelOf == baselineModel)
            //            _prodBBsA.Add(shp.Product, bb);
            //        else
            //            _prodBBsB.Add(shp.Product, bb);

            //        //add every BBox to the world to get the size and position of the world
            //        //if it contains valid BBox
            //        if (!float.IsNaN(bb.SizeX))
            //            worldBB.Union(bb);
            //    }
            //}
            //);

            //create octree
            //target size must depend on the units of the model
            //size inflated with 0.5 meter so that there are not that many products on the boundary of the world
            var size = Math.Max(Math.Max(worldBB.SizeX, worldBB.SizeY), worldBB.SizeZ) + _meter / 2.0;
            var shift = (float)_meter / 4f;
            var position = worldBB.Location + new XbimVector3D() { X = size / 2f - shift, Y = size / 2f - shift, Z = size / 2f - shift };
            _tree = new XbimOctree<IfcProduct>(size, (float)_meter, 1f, position);

            //add every product and its AABBox to octree
            foreach (var item in _prodBBsA)
                _tree.Add(item.Key, item.Value);
            foreach (var item in _prodBBsB)
                _tree.Add(item.Key, item.Value);
        }


        #region Comparer interface implementation
        public string Name
        {
            get { return "Xbim Geometry Comparer"; }
        }

        public string Description
        {
            get { return "Compares objects based on their geometry. It uses three levels of refinement including bounding boxes, geometry hash and precise geometry checking"; }
        }

        public ComparisonType ComparisonType
        {
            get { return ComparisonType.GEOMETRY; }
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


        public ComparisonResult Compare<T>(T baseline, IModel revisedModel) where T : IIfcRoot
        {
            var product = baseline as IfcProduct;
            
            //it doesn't make a sense to inspect geometry of anything that isn't product
            if (product == null) return null;

            //find octree node of the product
            var node = _tree.Find(product);

            //product doesn't have a geometry if it was not found in the octree
            if (node == null) return null;
#if DEBUG
            var candidates = GetCandidatesFromNode(product, node).ToList();
#else
                var candidates = GetCandidatesFromNode(product, node);
#endif

            var result = new ComparisonResult(baseline, this);
            foreach (var candidate in candidates)
            {
                //compare hash of the geometry
                if (CompareHashes(product, candidate))
                {
                    //precise geometry check should go here

                    result.Candidates.Add(candidate);
                    _processedFromB.Add(candidate);
                }
            }
            return result;
        }

        public ComparisonResult GetResidualsFromRevision<T>(IModel revisedModel) where T : IIfcRoot
        {
            var result = new ComparisonResult(null, this);
            var prods = _prodBBsB.Keys.Where(p => !_processedFromB.Contains(p) && typeof(T).IsAssignableFrom(p.GetType()));
            result.Candidates.AddRange(prods);
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
        private IEnumerable<IfcProduct> GetCandidatesFromNode(IfcProduct original, XbimOctree<IfcProduct> node)
        {
            if (original != null && node != null)
            {
                //content which if from other models
                var nodeContent = node.Content().Where(nc => (nc.ModelOf != original.ModelOf));
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

        private bool CompareHashes(IfcProduct baseline, IfcProduct revision)
        {
            //Martin messed up by srls new geometry
            //var shape = _baselineContext.ProductShapes.Where(ps => ps.Product == baseline).FirstOrDefault();

            //XbimShapeGroup baseShapeGroup = shape.Shapes; //get the basic geometries that make up this one
            //IEnumerable<int> baseShapeHashes = baseShapeGroup.ShapeHashCodes();
            //int baseCount = baseShapeHashes.Count();

            //IEnumerable<XbimProductShape> revisionShapes = _revisedContext.ProductShapes.Where(ps => ps.Product == revision);

            //foreach (var rs in revisionShapes)
            //{
            //    XbimShapeGroup shapeGroup = rs.Shapes;
            //    IEnumerable<int> revShapeHashes = rs.Shapes.ShapeHashCodes();
            //    if (baseCount == revShapeHashes.Count() && baseShapeHashes.Union(revShapeHashes).Count() == baseCount) //we have a match
            //    {
            //        return true;
            //    }
            //}
            return false;
        }

        #endregion
    }
}
