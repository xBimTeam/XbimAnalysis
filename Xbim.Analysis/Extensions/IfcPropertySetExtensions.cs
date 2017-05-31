using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Common.Geometry;
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

        public static IEnumerable<IIfcPropertySet> GetAllPropertySets(this IIfcObject obj)
        {
            return obj.IsDefinedBy.SelectMany(r => r.RelatingPropertyDefinition.PropertySetDefinitions).OfType<IIfcPropertySet>();
        }

        public static IEnumerable<IIfcPropertySet> GetAllPropertySets(this IIfcTypeObject obj)
        {
            return obj.HasPropertySets.OfType<IIfcPropertySet>();
        }

        public static IIfcTypeObject GetDefiningType(this IIfcObject obj)
        {
            return obj.IsTypedBy.Select(r => r.RelatingType).FirstOrDefault();
        }

        public static XbimMatrix3D ToMatrix3D(this IIfcObjectPlacement objPlace)
        {
            var lp = objPlace as IIfcLocalPlacement;
            if (lp != null)
            {
                XbimMatrix3D local = lp.RelativePlacement.ToMatrix3D();
                if (lp.PlacementRelTo != null)
                    return local * lp.PlacementRelTo.ToMatrix3D();
                else
                    return local;
            }
            else
                throw new NotImplementedException(string.Format("Placement of type {0} is not implemented", objPlace.GetType().Name));
        }

        public static XbimMatrix3D ToMatrix3D(this IIfcAxis2Placement placement)
        {
            var ax3 = placement as IIfcAxis2Placement3D;
            var ax2 = placement as IIfcAxis2Placement2D;
            if (ax3 != null)
                return ax3.ToMatrix3D();
            return ax2 != null ? ax2.ToMatrix3D() : XbimMatrix3D.Identity;
        }

        public static XbimMatrix3D ToMatrix3D(this IIfcAxis2Placement2D placement)
        {
            object transform;
            if (placement.RefDirection != null)
            {
                XbimVector3D v = placement.RefDirection.XbimVector3D();
                v.Normalized();
                transform = new XbimMatrix3D(v.X, v.Y, 0, 0, v.Y, v.X, 0, 0, 0, 0, 1, 0, placement.Location.X, placement.Location.Y, 0, 1);
            }
            else
                transform = new XbimMatrix3D(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, placement.Location.X, placement.Location.Y,
                                    placement.Location.Z, 1);
            return (XbimMatrix3D)transform;
        }

        public static XbimMatrix3D ToMatrix3D(this IIfcAxis2Placement3D pl)
        {
            return pl.ConvertAxis3D();
        }

        private static XbimMatrix3D ConvertAxis3D(this IIfcAxis2Placement3D pl)
        {
            if (pl.RefDirection == null || pl.Axis == null)
                return new XbimMatrix3D(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, pl.Location.X, pl.Location.Y,
                    pl.Location.Z, 1);

            var za = pl.Axis.XbimVector3D();
            za.Normalized();
            var xa = pl.RefDirection.XbimVector3D();
            xa.Normalized();
            var ya = za.CrossProduct(xa);
            ya.Normalized();
            return new XbimMatrix3D(xa.X, xa.Y, xa.Z, 0, ya.X, ya.Y, ya.Z, 0, za.X, za.Y, za.Z, 0, pl.Location.X,
                pl.Location.Y, pl.Location.Z, 1);
        }

        public static XbimVector3D XbimVector3D(this IIfcDirection dir)
        {
            return new XbimVector3D(dir.X, dir.Y, double.IsNaN(dir.Z) ? 0 : dir.Z);
        }
    }
}
