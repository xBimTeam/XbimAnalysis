using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.Analysis.Spatial
{
    /// <summary>
    /// Some of the spatial relations can be established from the semantic relations.
    /// This applies even for the object with no geometry.
    /// If the relation can't be established null is returned as the result is unknown but not necessarilly negative.
    /// </summary>
    public class XbimSemanticAnalyser: ISpatialRelations
    {
        private IModel _model;

        public XbimSemanticAnalyser(IModel model)
        {
            _model = model;
        }

        public bool Equals(IfcProduct first, IfcProduct second)
        {
            if (first == second) return true;

            //can't tell from semantics
            throw new NotImplementedException();
        }

        public bool Disjoint(IfcProduct first, IfcProduct second)
        {
            if (first == second) return false;

            //can't tell from semantics
            throw new NotImplementedException();
        }
        
        public bool Intersects(IfcProduct first, IfcProduct second)
        {
            throw new NotImplementedException();
        }

        public bool Touches(IfcProduct first, IfcProduct second)
        {
            //connects elements
            IEnumerable<IfcRelConnectsElements> connElemRels = _model.Instances.Where<IfcRelConnectsElements>
                (r => (r.RelatedElement == first && r.RelatingElement == second) || (r.RelatedElement == second && r.RelatingElement == first));
            if (connElemRels.FirstOrDefault() != null) return true;

            //Connects Path Elements
            IEnumerable<IfcRelConnectsPathElements> connPathElemRels = _model.Instances.Where<IfcRelConnectsPathElements>
                           (r => (r.RelatedElement == first && r.RelatingElement == second) || (r.RelatedElement == second && r.RelatingElement == first));
            if (connPathElemRels.FirstOrDefault() != null) return true;

            //Connects Port To Element
            IEnumerable<IfcRelConnectsPortToElement> connPortElemRels = _model.Instances.Where<IfcRelConnectsPortToElement>
            (r => (r.RelatedElement == first && r.RelatingPort == second) || (r.RelatedElement == second && r.RelatingPort == first));
            if (connPortElemRels.FirstOrDefault() != null) return true;
            
            //two elements might be connected via ports
            if (first is IIfcElement && second is IIfcElement)
            {
                IIfcElement fElement = first as IIfcElement;
                IIfcElement sElement = second as IIfcElement;
                IEnumerable<IfcRelConnectsPortToElement> connPortToElemRels = _model.Instances.Where<IfcRelConnectsPortToElement>
                    (r => (r.RelatingPort.ConnectedTo == fElement || r.RelatingPort.ConnectedTo == sElement));
                //get all ports
                List<IfcPort> ports = new List<IfcPort>();
                foreach (var relConPort in connPortToElemRels)
                {
                    ports.Add(relConPort.RelatingPort);
                }

                //find relations containing at least one of the ports
                IEnumerable<IfcRelConnectsPorts> connectPorts = _model.Instances.Where<IfcRelConnectsPorts>
                    (r => ports.Contains(r.RelatedPort) || ports.Contains(r.RelatingPort));

                //if there is such a connection than two elements are connected via port (like a pipe and the basin)
                if (connectPorts.FirstOrDefault() != null) return true;
            }
                
            //Connects Ports
            IEnumerable<IfcRelConnectsPorts> connPortsRels = _model.Instances.Where<IfcRelConnectsPorts>
                (r => (r.RelatedPort == first && r.RelatingPort == second) || (r.RelatedPort == second && r.RelatingPort == first));
            if (connPortsRels.FirstOrDefault() != null) return true;

            //Connects Structural Element
            IEnumerable<IfcRelConnectsStructuralElement> connStructRels = _model.Instances.Where<IfcRelConnectsStructuralElement>
                (r => (r.RelatedStructuralMember == first && r.RelatingElement == second) || (r.RelatedStructuralMember == second && r.RelatingElement == first));
            if (connStructRels.FirstOrDefault() != null) return true;

            //Connects Structural Member
            IEnumerable<IfcRelConnectsStructuralMember> connStructMemRels = _model.Instances.Where<IfcRelConnectsStructuralMember>
                (r => (r.RelatedStructuralConnection == first && r.RelatingStructuralMember == second) || (r.RelatedStructuralConnection == second && r.RelatingStructuralMember == first));
            if (connStructMemRels.FirstOrDefault() != null) return true;
            
            //Covers Bldg Elements
            IEnumerable<IfcRelCoversBldgElements> coversRels = _model.Instances.Where<IfcRelCoversBldgElements>
                (r => (r.RelatedCoverings.Contains(first) && r.RelatingBuildingElement == second) || (r.RelatedCoverings.Contains(second) && r.RelatingBuildingElement == first));
            if (coversRels.FirstOrDefault() != null) return true;

            //Covers Spaces
            IEnumerable<IfcRelCoversSpaces> coversSpacesRels = _model.Instances.Where<IfcRelCoversSpaces>
                (r => (r.RelatedCoverings.Contains(first) && r.RelatedSpace == second) || (r.RelatedCoverings.Contains(second) && r.RelatedSpace == first));
            if (coversSpacesRels.FirstOrDefault() != null) return true;
            
            //Space Boundary
            IEnumerable<IfcRelSpaceBoundary> spaceBoundRels = _model.Instances.Where<IfcRelSpaceBoundary>
                (r => (r.RelatedBuildingElement == first && r.RelatingSpace == second) || (r.RelatedBuildingElement == second && r.RelatingSpace == first));
            if (spaceBoundRels.FirstOrDefault() != null) return true;

            //Window or door filling the wall
            var firstFillingProds = GetFillingProducts(first);
            var secondFillingProds = GetFillingProducts(second);
            if (firstFillingProds.Contains(second)) return true;
            if (secondFillingProds.Contains(first)) return true;

            return false;
        }

        public bool Crosses(IfcProduct first, IfcProduct second)
        {
            throw new NotImplementedException();
        }

        public bool Within(IfcProduct first, IfcProduct second)
        {
            return Contains(second, first);
        }

        public bool Contains(IfcProduct first, IfcProduct second)
        {
            //this type of relation is always recursive
            if (first == second) return true;

            //check the case of spatial strucure element (specific relations)
            IfcSpatialStructureElement spatStruct = first as IfcSpatialStructureElement;
            IEnumerable<IfcProduct> prods = null;
            if (spatStruct != null)
            {
                prods = GetProductsInSpatStruct(spatStruct);
                foreach (var prod in prods)
                    if (Contains(prod, second)) return true;
            }
            prods = GetProductsInProduct(first);
            foreach (var prod in prods)
                if (Contains(prod, second)) return true;

            //if we don't know
            return false;
        }


        /// <summary>
        /// This cannot be established from the semantic relations
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public bool Overlaps(IfcProduct first, IfcProduct second)
        {
            throw new NotImplementedException();
        }

        public bool Relate(IfcProduct first, IfcProduct second)
        {
            if (Touches(first, second)) return true;
            if (Contains(first, second)) return true;
            if (Within(first, second)) return true;

            return false;
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
            foreach (var item in GetProductsInProduct(prod))
            {
                yield return item;
            }

            var spatialElement = prod as IfcSpatialStructureElement;
            if (spatialElement != null)
            {
                foreach (var item in GetProductsInSpatStruct(spatialElement))
                {
                    yield return item;
                }
            }
        }

        public IEnumerable<IfcProduct> GetRelatingProducts(IfcProduct prod)
        {
            throw new NotImplementedException();
        }
        #endregion


        #region Helper functions
        private IEnumerable<IfcProduct> GetProductsInSpatStruct(IfcSpatialStructureElement spatialStruct)
        {
            //contained in spatial structure
            IEnumerable<IfcRelContainedInSpatialStructure> prodRels = 
                _model.Instances.Where<IfcRelContainedInSpatialStructure>(r => r.RelatingStructure == spatialStruct);
            foreach (var rel in prodRels)
            {
                foreach (var prod in rel.RelatedElements)
                {
                    yield return prod;
                }
            }

            //referenced in spatial structure
            IEnumerable<IfcRelReferencedInSpatialStructure> prodRefs = 
                _model.Instances.Where<IfcRelReferencedInSpatialStructure>(r => r.RelatingStructure == spatialStruct);
            foreach (var rel in prodRefs)
            {
                foreach (var prod in rel.RelatedElements)
                {
                    yield return prod;
                }
            }
        }

        private IEnumerable<IfcProduct> GetProductsInProduct(IfcProduct prod)
        {
            //decomposes is a supertype of IfcRelAggregates, IfcRelNests
            IEnumerable<IfcRelDecomposes> decompRels = _model.Instances.Where<IfcRelDecomposes>(r => r.RelatingObject == prod);
            foreach (var item in decompRels)
            {
                foreach (var p in item.RelatedObjects)
                {
                    IfcProduct product = p as IfcProduct;
                    yield return product;
                }
            }

            //fills
            IfcOpeningElement opening = prod as IfcOpeningElement;
            if (opening != null)
            {
                IEnumerable<IfcRelFillsElement> fillsRels = _model.Instances.Where<IfcRelFillsElement>(r => r.RelatingOpeningElement == opening);
                foreach (var rel in fillsRels)
                {
                    yield return rel.RelatedBuildingElement;
                }
            }

            //voids
            IIfcElement element = prod as IIfcElement;
            if (element != null)
            {
                IEnumerable<IfcRelVoidsElement> voidsRels = _model.Instances.Where<IfcRelVoidsElement>(r => r.RelatingBuildingElement == element);
                foreach (var rel in voidsRels)
                {
                    yield return rel.RelatedOpeningElement;

                    //go one level deeper to the elements filling the opening because these are spatialy in the original product as well
                    foreach (var e in GetFillingProducts(rel.RelatedOpeningElement))
                    {
                        yield return e;
                    }
                }
            }
        }

        private IEnumerable<IfcProduct> GetFillingProducts(IfcProduct prod)
        {
            //voids
            var element = prod as IIfcElement;
            if (element != null)
            {
                var voidsRels = _model.Instances.Where<IfcRelVoidsElement>(r => r.RelatingBuildingElement == element);
                foreach (var rel in voidsRels)
                {
                    foreach (var item in GetFillingProducts(rel.RelatedOpeningElement))
                    {
                        yield return item;
                    }
                }
            }

            //fills
            var opening = prod as IfcOpeningElement;
            if (opening != null)
            {
                var fillsRels = _model.Instances.Where<IfcRelFillsElement>(r => r.RelatingOpeningElement == opening);
                foreach (var rel in fillsRels)
                {
                    yield return rel.RelatedBuildingElement;
                }
            }
        }
        #endregion



       
    }
}
