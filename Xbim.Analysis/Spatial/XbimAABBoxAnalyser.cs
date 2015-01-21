using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.Kernel;
using Xbim.Common.Geometry;
using Xbim.IO;
using System.IO;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Analysis.Spatial
{
    /// <summary>
    /// Spatial relations analyser of axis aligned bounding boxes of the products
    /// </summary>
    public class XbimAABBoxAnalyser : ISpatialRelations
    {
        private XbimModel _model;
        private Dictionary<IfcProduct, XbimRect3D> _prodBBs = new Dictionary<IfcProduct, XbimRect3D>();

        public XbimModel Model { get { return _model; } }

        /// <summary>
        /// Constructor of spatial relations analyser of axis aligned bounding boxes of the products.
        /// If you already have a dictionary of the AABBoxes and products you should use the other constructor
        /// </summary>
        /// <param name="model">Building model</param>
        public XbimAABBoxAnalyser(XbimModel model)
        {
            _model = model;
            Xbim3DModelContext context = new Xbim3DModelContext(model);
            if (model.GeometriesCount == 0)
            {       
                context.CreateContext();
            }


            //create cached BBoxes
            foreach (var prod in model.IfcProducts.Cast<IfcProduct>())
            {
                XbimRect3D prodBox = XbimRect3D.Empty;
                foreach (var shp in context.ShapeInstancesOf(prod))
                {
                    //bounding boxes are lightweight and are produced when geometry is created at first place

                    //get or cast to BBox
                    var bb = shp.BoundingBox;
                    bb = XbimRect3D.TransformBy(bb, shp.Transformation);
                    if (prodBox.IsEmpty) prodBox = bb; else prodBox.Union(bb);
                    //add every BBox to the world to get the size and position of the world
                }
                _prodBBs.Add(prod, prodBox);
            }
            
        }

        /// <summary>
        /// Constructor of spatial relations analyser of axis aligned bounding boxes of the products
        /// </summary>
        /// <param name="model">Building model</param>
        /// <param name="prodBBs">Axis aligned bounding boxes of the products</param>
        public XbimAABBoxAnalyser(XbimModel model, Dictionary<IfcProduct, XbimRect3D> prodBBs)
        {
            _model = model;
            _prodBBs = prodBBs;
        }

        public bool Equals(IfcProduct first, IfcProduct second)
        {
            //check if it is not identical obect
            if (first == second) return true;

            //BB approximation
            XbimRect3D firstBB, secondBB;
            if (!_prodBBs.TryGetValue(first, out firstBB)) return false; //no geometry, nothing to analyse
            if (!_prodBBs.TryGetValue(second, out secondBB)) return false; //no geometry, nothing to analyse
            
            return AlmostEqual(firstBB, secondBB, Tolerance);
        }

        public bool Disjoint(IfcProduct first, IfcProduct second)
        {
            //check if it is not identical obect
            if (first == second) return false;

            //BB approximation
            XbimRect3D firstBB, secondBB;
            if (!_prodBBs.TryGetValue(first, out firstBB)) return false; //no geometry, nothing to analyse
            if (!_prodBBs.TryGetValue(second, out secondBB)) return false; //no geometry, nothing to analyse
            
            return Disjoint(firstBB, secondBB, Tolerance);
        }

        public bool Intersects(IfcProduct first, IfcProduct second)
        {
            //check if it is not identical obect
            if (first == second) return false;

            //BB approximation
            XbimRect3D firstBB, secondBB;
            if (!_prodBBs.TryGetValue(first, out firstBB)) return false; //no geometry, nothing to analyse
            if (!_prodBBs.TryGetValue(second, out secondBB)) return false; //no geometry, nothing to analyse

            return Intersects(firstBB, secondBB, Tolerance);
        }

        public bool Touches(IfcProduct first, IfcProduct second)
        {
            //check if it is not identical obect
            if (first == second) return false;

            //BB approximation
            XbimRect3D firstBB, secondBB;
            if (!_prodBBs.TryGetValue(first, out firstBB)) return false; //no geometry, nothing to analyse
            if (!_prodBBs.TryGetValue(second, out secondBB)) return false; //no geometry, nothing to analyse
            
            return Touches(firstBB, secondBB, Tolerance);
        }

        public bool Within(IfcProduct first, IfcProduct second)
        {
            return Contains(second, first);
        }

        public bool Contains(IfcProduct first, IfcProduct second)
        {
            //check if it is not identical obect
            if (first == second) return false;

            //BB approximation
            XbimRect3D firstBB, secondBB;
            if (!_prodBBs.TryGetValue(first, out firstBB)) return false; //no geometry, nothing to analyse
            if (!_prodBBs.TryGetValue(second, out secondBB)) return false; //no geometry, nothing to analyse

            return Contains(firstBB, secondBB, Tolerance);

        }

        public bool Relate(IfcProduct first, IfcProduct second)
        {
            return !Disjoint(first, second);
        }


        #region Enumerable functions
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

        #region Helpers

        private double Tolerance { get { return _model.ModelFactors.OneMilliMetre * 10f; } }

        public static bool AlmostEqual(XbimRect3D firstBB, XbimRect3D secondBB, double tolerance)
        {
            //compare position of BBs
            var dMin = (firstBB.Min - secondBB.Min).Length;
            if (dMin > tolerance) return false;

            var dMax = (firstBB.Max - secondBB.Max).Length;
            if (dMax > tolerance) return false;

            //if all previous tests were OK BBs are supposed to be almost equal
            return true;
        }

        public static bool AlmostEqual(double a, double b, double tolerance)
        {
            return Math.Abs(a - b) < tolerance;
        }

        public static bool Disjoint(XbimRect3D a, XbimRect3D b, double tolerance)
        {
            return (
                a.Max.X + tolerance < b.Min.X ||
                a.Max.Y + tolerance < b.Min.Y ||
                a.Max.Z + tolerance < b.Min.Z
                ) || (
                a.Min.X - tolerance > b.Max.X ||
                a.Min.Y - tolerance > b.Max.Y ||
                a.Min.Z - tolerance > b.Max.Z
                );
        }

        public static bool Contains(XbimRect3D a, XbimRect3D b, double tolerance)
        {
            return
                a.Min.X - tolerance < b.Min.X &&
                a.Min.Y - tolerance < b.Min.Y &&
                a.Min.Z - tolerance < b.Min.Z &&
                a.Max.X + tolerance > b.Max.X &&
                a.Max.Y + tolerance > b.Max.Y &&
                a.Max.Z + tolerance > b.Max.Z &&
                //avoid identical geometries to be supposed to be contained in another one
                !AlmostEqual(a, b, tolerance)
                ;
        }

        public static bool Touches(XbimRect3D a, XbimRect3D b, double tolerance)
        {
            XbimRect3D envelope = XbimRect3D.Empty;
            envelope.Union(a);
            envelope.Union(b);

            var xSize = a.SizeX + b.SizeX;
            var ySize = a.SizeY + b.SizeY;
            var zSize = a.SizeZ + b.SizeZ;

            return
                (xSize >= envelope.SizeX && ySize >= envelope.SizeY && AlmostEqual(zSize, envelope.SizeZ, tolerance))
                ||
                (xSize >= envelope.SizeX && AlmostEqual(ySize, envelope.SizeY, tolerance) && zSize >= envelope.SizeZ)
                ||
                (AlmostEqual(xSize, envelope.SizeX, tolerance) && ySize >= envelope.SizeY && zSize >= envelope.SizeZ)
                ;
        }

        public static bool Intersects(XbimRect3D a, XbimRect3D b, double tolerance)
        {
            XbimRect3D envelope = XbimRect3D.Empty;
            envelope.Union(a);
            envelope.Union(b);

            var xSize = a.SizeX + b.SizeX;
            var ySize = a.SizeY + b.SizeY;
            var zSize = a.SizeZ + b.SizeZ;

            return
                (
                xSize > envelope.SizeX && 
                ySize > envelope.SizeY && 
                zSize > envelope.SizeZ
                ) && !( 
                //avoid identical geometries to be supposed to be intersection
                AlmostEqual(xSize / 2f, envelope.SizeX, tolerance) &&
                AlmostEqual(ySize / 2f, envelope.SizeY, tolerance) &&
                AlmostEqual(zSize / 2f, envelope.SizeZ, tolerance)
                )
                ;
        }

        #endregion
    }
}
