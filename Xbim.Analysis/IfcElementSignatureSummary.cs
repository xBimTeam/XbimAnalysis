using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.Analysis
{
    public class IfcElementSignatureSummary
    {
        public string FileName;
        public string OriginatingSystem;
        public string PreprocessorVersion;
        public string IfcVersion;
        public int NameCount;
        public int DescriptionCount;
        public int ProductCount;
        public int HasAssignmentsCount;
        public int IsDecomposedByCount;
        public int DecomposesCount;
        public int HasAssociationsCount;
        public int ObjectTypeCount;
        public int PropertyCount;
        public int MaterialNameCount;
        public int PropertySetNameCount;
        public int PropertyNamesCount;
        public int PropertyValuesCount;
        public int HasGeometryCount;
        public int ReferencedByCount;
        public int TagCount;
        public int HasStructuralMemberCount;
        public int FillsVoidsCount;
        public int ConnectedToCount;
        public int HasCoveringsCount;
        public int HasProjectionsCount;
        public int ReferencedInStructuresCount;
        public int HasPortsCount;
        public int HasOpeningsCount;
        public int IsConnectionRealizationCount;
        public int ProvidesBoundariesCount;
        public int ConnectedFromCount;
        public int ContainedInStructureCount;

        public void Add(IfcElementSignature sig)
        {
           if(!string.IsNullOrWhiteSpace(sig.Name)) NameCount++;
           if (!string.IsNullOrWhiteSpace(sig.Description)) DescriptionCount++;
           ProductCount++;
           if (sig.HasAssignmentsKey > 0) HasAssignmentsCount++;
           if (sig.IsDecomposedByKey > 0) IsDecomposedByCount++;
           if (sig.DecomposesKey > 0) DecomposesCount++;
           if (sig.HasAssociationsKey > 0) HasAssociationsCount++;
           if (!string.IsNullOrWhiteSpace(sig.ObjectType)) ObjectTypeCount++;
           if (sig.PropertyCount > 0) PropertyCount++;
           if (!string.IsNullOrWhiteSpace(sig.MaterialName)) MaterialNameCount++;
           if (sig.PropertySetNamesKey > 0) PropertySetNameCount++;
           if (sig.PropertyNamesKey > 0) PropertyNamesCount++;
           if (sig.PropertyValuesKey > 0) PropertyValuesCount++;
           if (sig.ShapeId > 0) HasGeometryCount++;
           if (!string.IsNullOrWhiteSpace(sig.Tag)) TagCount++;
           if (sig.HasStructuralMemberKey > 0) HasStructuralMemberCount++;
           if (sig.FillsVoidsKey > 0) FillsVoidsCount++;
           if (sig.ConnectedToKey > 0) ConnectedToCount++;
           if (sig.HasCoveringsKey > 0) HasCoveringsCount++;
           if (sig.HasProjectionsKey > 0) HasProjectionsCount++;
           if (sig.ReferencedInStructuresKey > 0) ReferencedInStructuresCount++;
           if (sig.HasPortsKey > 0) HasPortsCount++;
           if (sig.HasOpeningsKey > 0) HasOpeningsCount++;
           if (sig.IsConnectionRealizationKey > 0) IsConnectionRealizationCount++;
           if (sig.ProvidesBoundariesKey > 0) ProvidesBoundariesCount++;
           if (sig.ConnectedFromKey > 0) ConnectedFromCount++;
           if (sig.HasOpeningsKey > 0) HasOpeningsCount++;
           if (sig.ContainedInStructureKey > 0) ContainedInStructureCount++;
        }

        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30},{31}",
        FileName,
        OriginatingSystem,
        PreprocessorVersion,
        IfcVersion,
        NameCount,
        DescriptionCount,
        ProductCount,
        HasAssignmentsCount,
        IsDecomposedByCount,
        DecomposesCount,
        HasAssociationsCount,
        ObjectTypeCount,
        PropertyCount,
        MaterialNameCount,
        PropertySetNameCount,
        PropertyNamesCount,
        PropertyValuesCount,
        HasGeometryCount,
        ReferencedByCount,
        TagCount,
        HasStructuralMemberCount,
        FillsVoidsCount,
        ConnectedToCount,
        HasCoveringsCount,
        HasProjectionsCount,
        ReferencedInStructuresCount,
        HasPortsCount,
        HasOpeningsCount,
        IsConnectionRealizationCount,
        ProvidesBoundariesCount,
        ConnectedFromCount,
        ContainedInStructureCount
               );
        }

        static public string CSVheader()
        {
            return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30},{31}",
                 "FileName",
        "OriginatingSystem",
        "PreprocessorVersion",
        "IfcVersion",
        "NameCount",
        "DescriptionCount",
        "ProductCount",
        "HasAssignmentsCount",
        "IsDecomposedByCount",
        "DecomposesCount",
        "HasAssociationsCount",
        "ObjectTypeCount",
        "PropertyCount",
        "MaterialNameCount",
        "PropertySetNameCount",
        "PropertyNamesCount",
        "PropertyValuesCount",
        "HasGeometryCount",
        "ReferencedByCount",
        "TagCount",
        "HasStructuralMemberCount",
        "FillsVoidsCount",
        "ConnectedToCount",
        "HasCoveringsCount",
        "HasProjectionsCount",
        "ReferencedInStructuresCount",
        "HasPortsCount",
        "HasOpeningsCount",
        "IsConnectionRealizationCount",
        "ProvidesBoundariesCount",
        "ConnectedFromCount",
        "ContainedInStructureCount"
        );

        }
    }
}
