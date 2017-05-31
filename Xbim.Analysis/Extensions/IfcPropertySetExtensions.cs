using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.PropertyResource;

namespace Xbim.Analysis.Extensions
{
    public static class IfcPropertySetExtensions
    {
        public static int GetPropertyHash(this IIfcProperty property)
        {
            if (property == null) return 0;

            //check actual value
            switch (property.GetType().Name)
            {
                case "IfcPropertySingleValue":
                    var baseVal = ((IfcPropertySingleValue)(property)).NominalValue;
                    var baseStr = baseVal == null ? "" : baseVal.ToString();
                    return "IfcPropertySingleValue".GetHashCode() + baseStr.GetHashCode();
                case "IfcPropertyEnumeratedValue":
                    var baseEnum = property as IfcPropertyEnumeratedValue;
                    int enumHash = "IfcPropertyEnumeratedValue".GetHashCode();
                    foreach (var e in baseEnum.EnumerationValues)
                        enumHash += e.ToString().GetHashCode();
                    return enumHash;
                case "IfcPropertyBoundedValue":
                    var baseBounded = property as IfcPropertyBoundedValue;
                    return "IfcPropertyBoundedValue".GetHashCode() + baseBounded.UpperBoundValue.ToString().GetHashCode()
                        + baseBounded.LowerBoundValue.ToString().GetHashCode();
                case "IfcPropertyTableValue":
                    var baseTable = property as IfcPropertyTableValue;
                    //check all table items
                    int tableHash = "IfcPropertyTableValue".GetHashCode();
                    for (int i = 0; i < baseTable.DefiningValues.Count; i++)
                    {
                        tableHash +=
                            baseTable.DefiningValues[i].ToString().GetHashCode() +
                            baseTable.DefinedValues[i].ToString().GetHashCode();
                    }
                    return tableHash;
                case "IfcPropertyReferenceValue":
                    var baseRef = property as IfcPropertyReferenceValue;
                    var refHash = "IfcPropertyReferenceValue".GetHashCode();
                    refHash += baseRef.UsageName.ToString().GetHashCode();
                    refHash += baseRef.PropertyReference.GetType().GetHashCode();
                    //should go deeper but it would be too complicated for now
                    return refHash;
                case "IfcPropertyListValue":
                    var baseList = property as IfcPropertyListValue;
                    var listHash = "IfcPropertyListValue".GetHashCode();
                    foreach (var item in baseList.ListValues)
                        listHash += item.ToString().GetHashCode();
                    return listHash;
                default:
                    break;
            }
            throw new NotImplementedException();
        }

        public static int GetPSetHash(this IIfcPropertySet pSet)
        {
            if (pSet == null) return 0;
            var result = pSet.GetType().GetHashCode();
            foreach (var item in pSet.HasProperties)
                result += GetPropertyHash(item);
            return result;
        }
    }
}
