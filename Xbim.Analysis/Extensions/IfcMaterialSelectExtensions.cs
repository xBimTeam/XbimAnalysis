using System;
using Xbim.Ifc2x3.MaterialResource;

namespace Xbim.Analysis.Extensions
{
    public static class IfcMaterialSelectExtensions
    {
        public static int CreateHashCode(this IfcMaterialSelect materialSelect)
        {
            var material = materialSelect as IfcMaterial;
            if (material != null)
                return "IfcMaterial".GetHashCode() + material.Name.GetHashCode();
            
            var materialList = materialSelect as IfcMaterialList;
            if (materialList != null)
            {
                int result = "IfcMaterialList".GetHashCode();
                foreach (var item in materialList.Materials)
                {
                    result += item.CreateHashCode();   
                }
                return result;
            }

            var usage = materialSelect as IfcMaterialLayerSetUsage;
            if (usage != null)
            {
                int result = "IfcMaterialLayerSetUsage".GetHashCode();
                var lSet = usage.ForLayerSet;
                return result += lSet.CreateHashCode();
            }

            var layerSet = materialSelect as IfcMaterialLayerSet;
            if (layerSet != null)
            {
                int result = "IfcMaterialLayerSet".GetHashCode();
                foreach (var l in layerSet.MaterialLayers)
                {
                    result += l.CreateHashCode();
                }
                return result;
            }

            var layer = materialSelect as IfcMaterialLayer;
            if (layer != null)
            {
                int result = "IfcMaterialLayer".GetHashCode();
                result += layer.Material.CreateHashCode();
                result += layer.LayerThickness.ToString().GetHashCode();
                return result;
            }

            throw new NotImplementedException();
        }
    }
}
