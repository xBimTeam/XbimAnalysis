using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.Kernel;
using Xbim.XbimExtensions.Interfaces;
using Xbim.IO;
using Xbim.XbimExtensions;
using Xbim.Common.Geometry;
using System.IO;
using Xbim.ModelGeometry.Scene;
using System.Diagnostics;
using Xbim.Ifc2x3.Extensions;
namespace Xbim.Analysis.Spatial
{
    public class XbimSpatialAnalyser : ISpatialAnalyser
    {
        private XbimOctree<IfcProduct> _tree;
        private Dictionary<IfcProduct, XbimRect3D> _prodBBs = new Dictionary<IfcProduct, XbimRect3D>();
        public XbimOctree<IfcProduct> Octree { get { return _tree; } }

        private XbimModel _model;
        public XbimModel Model { get { return _model; } }

        private XbimAABBoxAnalyser _bboxAnalyser;
        private XbimSemanticAnalyser _semanticAnalyser;

        /// <summary>
        /// Octree is created in the constructor. This method of spatial
        /// indexing should speed up spatial queries by doing simple 
        /// computations on indexed geometries.
        /// </summary>
        /// <param name="model">Building model</param>
        public XbimSpatialAnalyser(XbimModel model)
        {
            _model = model;
            _bboxAnalyser = new XbimAABBoxAnalyser(model, _prodBBs);
            _semanticAnalyser = new XbimSemanticAnalyser(model);

            //generate geometry if there is no in the model
            if (model.GeometriesCount == 0)
            {
                //create geometry
                 var m3D = new Xbim3DModelContext(model);
                 m3D.CreateContext(true);
            }
            
            //initialize octree with all the objects
            var prods = model.Instances.OfType<IfcProduct>();
            //Stopwatch sw = new Stopwatch();
            //var report = new StringWriter();
            //report.WriteLine("{0,-15}, {1,-40}, {2,5}, {3,5}", "Type", "Product name", "Geometry", "BBox");
            
            //we need to preprocess all the products first to get world size. Will keep results to avoid repetition.
            XbimRect3D worldBB = XbimRect3D.Empty;
            foreach (var prod in prods)
            {

                //bounding boxes are lightweight and are produced when geometry is created at first place
                //sw.Start();
                var geom = prod.Geometry3D();
                var trans = prod.ObjectPlacement.ToMatrix3D();
                //sw.Stop();
                //var geomGeneration = sw.ElapsedMilliseconds;

                if (geom != null && geom.FirstOrDefault() != null)
                {
                    //get or cast to BBox
                    //sw.Restart();
                    var bb = geom.GetAxisAlignedBoundingBox();
                    bb = new XbimRect3D(trans.Transform(bb.Min), trans.Transform(bb.Max));
                    //sw.Stop();
                    //var gettingBbox = sw.ElapsedMilliseconds;
                    //report.WriteLine("{0,-15}, {1,-40}, {2,5}, {3,5}", prod.GetType().Name, prod.Name, geomGeneration, gettingBbox);

                    //add every BBox to the world to get the size and position of the world
                    _prodBBs.Add(prod, bb);
                    if (!double.IsNaN(bb.SizeX))
                        worldBB.Union(bb);

                    //Debug.WriteLine("{0,-45} {1,10:F5} {2,10:F5} {3,10:F5} {4,10:F5} {5,10:F5} {6,10:F5}", 
                    //    prod.Name, bb.X, bb.Y, bb.Z, bb.SizeX, bb.SizeY, bb.SizeZ);
                }
            }
            //Debug.WriteLine("{0,-45} {1,10:F5} {2,10:F5} {3,10:F5} {4,10:F5} {5,10:F5} {6,10:F5}",
            //           "World", worldBB.X, worldBB.Y, worldBB.Z, worldBB.SizeX, worldBB.SizeY, worldBB.SizeZ);

            //create octree
            //target size must depend on the units of the model
            var meter = (float)model.ModelFactors.OneMetre;
            var size = Math.Max(Math.Max(worldBB.SizeX, worldBB.SizeY), worldBB.SizeZ) + meter/2f;
            var shift = meter / 4f;
            var position = worldBB.Location + new XbimVector3D() {X = size/2f-shift, Y = size/2f-shift, Z = size/2f-shift };
            _tree = new XbimOctree<IfcProduct>(size, meter, 1f, position);

            //sw.Restart();
            //add every product to the world
            foreach (var item in _prodBBs)
                _tree.Add(item.Key, item.Value);

            //sw.Stop();
            //report.WriteLine("Generation of octree containing {0} products {1}", prods.Count(), sw.ElapsedMilliseconds);
        }

        #region Directions
        public bool NorthOf(IfcProduct first, IfcProduct second)
        {
            throw new NotImplementedException();
        }

        public bool SouthOf(IfcProduct first, IfcProduct second)
        {
            throw new NotImplementedException();
        }

        public bool WestOf(IfcProduct first, IfcProduct second)
        {
            throw new NotImplementedException();
        }

        public bool EastOf(IfcProduct first, IfcProduct second)
        {
            throw new NotImplementedException();
        }

        public bool Above(IfcProduct first, IfcProduct second)
        {
            throw new NotImplementedException();
        }

        public bool Below(IfcProduct first, IfcProduct second)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Spatial Functions
        public double Distance(IfcProduct first, IfcProduct second)
        {
            throw new NotImplementedException();
        }

        public IfcProduct Buffer(IfcProduct product)
        {
            throw new NotImplementedException();
        }

        public IfcProduct ConvexHull(IfcProduct product)
        {
            throw new NotImplementedException();
        }

        public IfcProduct Intersection(IfcProduct first, IfcProduct second)
        {
            throw new NotImplementedException();
        }

        public IfcProduct Union(IfcProduct first, IfcProduct second)
        {
            throw new NotImplementedException();
        }

        public IfcProduct Difference(IfcProduct first, IfcProduct second)
        {
            throw new NotImplementedException();
        }

        public IfcProduct SymDifference(IfcProduct first, IfcProduct second)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Relations
        public bool Equals(IfcProduct first, IfcProduct second)
        {
            //check if it is not identical obect
            if (first == second) return true;

            //if BBoxes are not equal it can't be equal
            if (!_bboxAnalyser.Equals(first, second)) return false;

            //exact calculation should be performed at this step
            throw new NotImplementedException();
        }

        public bool Disjoint(IfcProduct first, IfcProduct second)
        {
            //check if it is not identical obect
            if (first == second) return false;

            //If BBoxes are disjoint than products must be disjoint as well
            if (_bboxAnalyser.Disjoint(first, second)) return true;

            
            //exact calculation should be performed at this step
            throw new NotImplementedException();
        }

        public bool Intersects(IfcProduct first, IfcProduct second)
        {
            //check if it is not identical obect
            if (first == second) return false;

            //If BBox approximation doesn't intersect or is not spatialy equal there can't be an intersection
            if (!_bboxAnalyser.Intersects(first, second) && !_bboxAnalyser.Equals(first, second)) return false;

            //exact calculation should be performed at this step
            throw new NotImplementedException();
        }

        public bool Touches(IfcProduct first, IfcProduct second)
        {
            //check if it is not identical obect
            if (first == second) return false;

            //use sematic relations as a first criterium
            if (_semanticAnalyser.Touches(first, second)) return true;

            //BBox approximation
            if (!(
                _bboxAnalyser.Intersects(first, second) || 
                _bboxAnalyser.Equals(first, second) || 
                _bboxAnalyser.Touches(first, second)
                )) 
                return false;

            //exact calculation should be performed at this step
            throw new NotImplementedException();
        }

        public bool Within(IfcProduct first, IfcProduct second)
        {
            return Contains(second, first);
        }

        public bool Contains(IfcProduct first, IfcProduct second)
        {
            //check if it is not identical obect
            if (first == second) return false;

            //use sematic relations as a first criterium
            if (_semanticAnalyser.Contains(first, second)) return true;

            //BBox approximation
            if (!(
                _bboxAnalyser.Equals(first, second) ||
                _bboxAnalyser.Contains(first, second)
                ))
                return false;

            //exact calculation should be performed at this step
            throw new NotImplementedException();
        }

        public bool Relate(IfcProduct first, IfcProduct second)
        {
            if (Equals(first, second)) return true;
            if (Intersects(first, second)) return true;
            if (Touches(first, second)) return true;
            if (Contains(first, second)) return true;
            if (Within(first, second)) return true;
            return false;
        }

        #region Enumerations of relater products
        public IEnumerable<IfcProduct> GetEqualTo(IfcProduct prod)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IfcProduct> GetDisjointFrom(IfcProduct prod)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IfcProduct> GetIntersectingWith(IfcProduct prod)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IfcProduct> GetTouching(IfcProduct prod)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IfcProduct> GetContainedProducts(IfcProduct prod)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IfcProduct> GetRelatingProducts(IfcProduct prod)
        {
            throw new NotImplementedException();
        }

        #endregion

        #endregion

        #region Helpers
       

        #endregion
    }
}
