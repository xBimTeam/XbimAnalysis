using System.Collections.Generic;
using System.Linq;
using Xbim.Common.Geometry;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.PropertyResource;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Analysis
{

    public struct PropertySetNameComparer : IComparer<IIfcPropertySet>
    {

        public int Compare(IIfcPropertySet x, IIfcPropertySet y)
        {
            return string.Compare(x.Name, y.Name);
        }
    }
    public struct PropertySingleValueNameComparer : IComparer<IfcPropertySingleValue>
    {

        public int Compare(IfcPropertySingleValue x, IfcPropertySingleValue y)
        {
            return string.Compare(x.Name, y.Name);
        }
    }

    public struct PropertySingleValueValueComparer : IComparer<IfcPropertySingleValue>
    {

        public int Compare(IfcPropertySingleValue x, IfcPropertySingleValue y)
        {
            if (x.NominalValue == null) return -1;
            if (y.NominalValue == null) return 1;
            return System.String.CompareOrdinal(x.NominalValue.Value.ToString(), y.NominalValue.Value.ToString());
        }
    }

    /// <summary>
    /// A signature for comparing IfcElements
    /// </summary>
    public class IfcElementSignature
    {
        public int ModelID;
        public string SchemaType;
        public string DefinedTypeId;
        public string GlobalId;
        public string OwningUser;
        public string Name;
        public string Description;
        public int HasAssignmentsKey;
        public int IsDecomposedByKey;
        public int DecomposesKey;
        public int HasAssociationsKey;
        public string ObjectType;
        public int PropertyCount;
        public string MaterialName;
        public int PropertySetNamesKey;
        public int PropertyNamesKey;
        public int PropertyValuesKey;
        public double CentroidX;
        public double CentroidY;
        public double CentroidZ;
        public double BoundingSphereRadius;
        public int ShapeId;
        public int ReferencedByKey;
        public string Tag;
        public int HasStructuralMemberKey;
        public int FillsVoidsKey;
        public int ConnectedToKey;
        public int HasCoveringsKey;
        public int HasProjectionsKey;
        public int ReferencedInStructuresKey;
        public int HasPortsKey;
        public int HasOpeningsKey;
        public int IsConnectionRealizationKey;
        public int ProvidesBoundariesKey;
        public int ConnectedFromKey;
        public int ContainedInStructureKey;

        public IfcElementSignature(IIfcElement elem, IGeometryStoreReader geometryContext)
        {


            XbimMatrix3D m3D = XbimMatrix3D.Identity;
            if(elem.ObjectPlacement !=null) m3D = elem.ObjectPlacement.ToMatrix3D();
            //var geomManager = elem.Model.GeometryManager;
            
            ShapeId = 0;
            //get the 3D shape
            var shapes = geometryContext.ShapeInstancesOfEntity(elem);
            if (shapes.Any())
            {
                XbimRect3D r3D = XbimRect3D.Empty;
                foreach (var shape in shapes)
                {
                    if (r3D.IsEmpty)
                        r3D = shape.BoundingBox;
                    else
                        r3D.Union(shape.BoundingBox);

                }
                XbimPoint3D p3D = r3D.Centroid();
                p3D = m3D.Transform(p3D);
                BoundingSphereRadius = r3D.Length() / 2;
                CentroidX = p3D.X;
                CentroidY = p3D.Y;
                CentroidZ = p3D.Z;
            }
            //get the defining type
            IIfcTypeObject ot = elem.GetDefiningType();
            IIfcMaterialSelect material = (elem is IIfcObjectDefinition) ? (elem as IIfcObjectDefinition).Material : null;
            //sort out property definitions
            List<IIfcPropertySet> psets = elem.GetAllPropertySets();
            PropertyCount = psets.SelectMany(p => p.HasProperties).Count();
            psets.Sort(new PropertySetNameComparer());
            foreach (var pset in psets)
            {
                PropertySetNamesKey ^= pset.Name.GetHashCode();
            }
            List<IfcPropertySingleValue> props = psets.SelectMany(p => p.HasProperties).OfType<IfcPropertySingleValue>().ToList();
            props.Sort(new PropertySingleValueNameComparer());
            foreach (var prop in props)
            {
                PropertyNamesKey ^= prop.Name.GetHashCode();
            }
            props.Sort(new PropertySingleValueValueComparer());
            foreach (var prop in props)
            {
                PropertyValuesKey ^= prop.NominalValue.GetHashCode();
            }
            ModelID =elem.EntityLabel;
            SchemaType = elem.GetType().Name;
            DefinedTypeId = (ot == null ? "" : (string)ot.GlobalId);
            GlobalId = elem.GlobalId;
            OwningUser = elem.OwnerHistory.LastModifyingUser != null ? elem.OwnerHistory.LastModifyingUser.ToString() : elem.OwnerHistory.OwningUser.ToString();
            Name = elem.Name ?? "";
            Description = elem.Description ?? "";
            HasAssignmentsKey = elem.HasAssignments.Count();
            IsDecomposedByKey = elem.IsDecomposedBy.Count();
            DecomposesKey = elem.Decomposes.Count();
            HasAssociationsKey = elem.HasAssociations.Count();
            ObjectType = elem.ObjectType ?? "";
            MaterialName = material == null ? "" : material.ToString();
            ReferencedByKey = elem.ReferencedBy.Count();
            Tag = elem.Tag ?? "";
            HasStructuralMemberKey = 0; // elem.HasStructuralMember.Count();
            FillsVoidsKey = elem.FillsVoids.Count();
            ConnectedToKey = elem.ConnectedTo.Count();
            HasCoveringsKey = elem.HasCoverings.Count();
            HasProjectionsKey = elem.HasProjections.Count();
            ReferencedInStructuresKey = elem.ReferencedInStructures.Count();
            HasPortsKey = (elem is IIfcDistributionElement) ? (elem as IIfcDistributionElement).HasPorts.Count() : 0;
            HasOpeningsKey = elem.HasOpenings.Count();
            IsConnectionRealizationKey = elem.IsConnectionRealization.Count();
            ProvidesBoundariesKey = elem.ProvidesBoundaries.Count();
            ConnectedFromKey = elem.ConnectedFrom.Count();
            ContainedInStructureKey = elem.ContainedInStructure.Count();
        }

        static public string CSVheader()
        {
            return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30},{31},{32},{33},{34},{35}",
                "Model ID",
                "SchemaType",
                "DefinedTypeId",
                "GUID",
                "Owner",
                "Name",
                "Description",
                "HasAssignments",
                "IsDecomposedBy",
                "Decomposes",
                "HasAssociations",
                "ObjectType",
                "PropertyCount",
                "PropertySetNamesKey",
                "PropertyNamesKey",
                "PropertyValuesKey",
                "MaterialId",
                "CentroidX",
                "CentroidY",
                "CentroidZ",
                "RadiusBoundingSphere",
                "ShapeId",
                "ReferencedBy",
                "Tag",
                "HasStructuralMember",
                "FillsVoids",
                "ConnectedTo",
                "HasCoverings",
                "HasProjections",
                "ReferencedInStructures",
                "HasPorts",
                "HasOpenings",
                "IsConnectionRealization",
                "ProvidesBoundaries",
                "ConnectedFrom",
                "ContainedInStructure");

        }

        public string ToCSV()
        {
            return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30},{31},{32},{33},{34},{35}",
                ModelID,
                SchemaType,
                DefinedTypeId,
                GlobalId,
                OwningUser,
                Name,
                Description,
                HasAssignmentsKey,
                IsDecomposedByKey,
                DecomposesKey,
                HasAssociationsKey,
                ObjectType,
                PropertyCount,
                PropertySetNamesKey,
                PropertyNamesKey,
                PropertyValuesKey,
                MaterialName,
                CentroidX,
                CentroidY,
                CentroidZ,
                BoundingSphereRadius,
                ShapeId,
                ReferencedByKey,
                Tag,
                HasStructuralMemberKey,
                FillsVoidsKey,
                ConnectedToKey,
                HasCoveringsKey,
                HasProjectionsKey,
                ReferencedInStructuresKey,
                HasPortsKey,
                HasOpeningsKey,
                IsConnectionRealizationKey,
                ProvidesBoundariesKey,
                ConnectedFromKey,
                ContainedInStructureKey
                );
        }
    }
}
