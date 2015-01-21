using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common.Geometry;
using Xbim.Ifc2x3.Kernel;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Analysis.Comparitors
{
    public class GeometryComparer : IModelComparer
    {
        public Dictionary<IfcRoot, ChangeType> Compare(IEnumerable<IfcRoot> Baseline, IEnumerable<IfcRoot> Delta)
        {
            throw new NotImplementedException(); //need to do this
        }

        private Dictionary<Int32, Int32> map = new Dictionary<Int32, Int32>();
        public Dictionary<Int32, Int32> GetMap() { return map; }

        public Dictionary<IfcRoot, ChangeType> Compare(Xbim3DModelContext baseContext, Xbim3DModelContext revisionContext, double oneMillimetre)
        {
            Dictionary<IfcRoot, ChangeType> changes = new Dictionary<IfcRoot, ChangeType>();
            //Martin I have messed this function comletely
            //List<XbimShapeInstance> baseShapes = baseContext.ShapeInstances().ToList();
            //List<XbimShapeInstance> revisedShapes = revisionContext.ShapeInstances().ToList();
            ////Create our dictionary for return
            //Dictionary<IfcRoot, ChangeType> changes = new Dictionary<IfcRoot, ChangeType>();

            ////check intersections of boounding boxes
            ////below code is mock and will be replaced by octree

            //foreach (var shape in baseShapes)
            //{
            //    //Martin this is wrong as I am working on parts of a product sha[pe rather then the whole product shape
            //    IEnumerable<XbimShapeInstance> hits = revisedShapes.Where(s => XbimRect3D.TransformBy(s.BoundingBox, s.Transformation).IsSimilar(XbimRect3D.TransformBy(shape.BoundingBox, s.Transformation), oneMillimetre * 10));
            //    int hitCount = hits.Count();
            //    //Martin the code below is random just to see what is happening and to show you how Lloyds frameowrk works
            //    //I am working through this and I am finding complexities
            //    //what if one object has two more matches? I have put in unknown state just to see results, but really this might be a clash
            //    //unless as in this case it is a window in an opening
            //    if (hitCount == 1)
            //    {
            //        //changes[(IfcProduct)model.Instances[shape.IfcProductLabel] = ChangeType.Matched;
            //        //map.Add(Math.Abs(shape.Product.EntityLabel), Math.Abs(hits.First().Product.EntityLabel));
            //    }
            //    if (hitCount > 1) //check the shapes of this product to try and match
            //    {
            //        XbimShapeGroup baseShapeGroup = shape.Shapes; //get the basic geometries that make up this one
            //        IEnumerable<int> baseShapeHashes = baseShapeGroup.ShapeHashCodes();
            //        int baseCount = baseShapeHashes.Count();
            //        foreach (var productShape in hits)
            //        {
            //            XbimShapeGroup shapeGroup = productShape.Shapes;
            //            IEnumerable<int> revShapeHashes = productShape.Shapes.ShapeHashCodes();
            //            if (baseCount == revShapeHashes.Count() && baseShapeHashes.Union(revShapeHashes).Count() == baseCount) //we have a match
            //            {
            //                changes.Add(productShape.Product, ChangeType.Matched);
            //                map.Add(Math.Abs(shape.Product.EntityLabel), Math.Abs(productShape.Product.EntityLabel));
            //            }
            //            else
            //                changes.Add(productShape.Product, ChangeType.Unknown);
            //        }
                    
            //    }
            //}
            return changes;
        }
    }
}
