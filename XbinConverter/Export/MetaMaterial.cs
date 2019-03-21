using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;

namespace XbinConverter.Export
{
    public class MetaMaterial
    {
        public UnityProperty Prop { get; set; }

        public MetaMaterial(IPersistEntity entity)
        {
            Prop = new UnityProperty
            {
                EntityLabel = entity.EntityLabel,
                Items = new List<MetaItem>()
            };

            if (entity is IIfcObject)
            {
                var ifcObj = entity as IIfcObject;
                var matRels = ifcObj.HasAssociations.OfType<IIfcRelAssociatesMaterial>();
                foreach (var matRel in matRels)
                {
                    AddMaterialData(matRel.RelatingMaterial, "");
                }
            }
            else if (entity is IIfcTypeObject)
            {
                var ifcObj = entity as IIfcTypeObject;
                var matRels = ifcObj.HasAssociations.OfType<IIfcRelAssociatesMaterial>();
                foreach (var matRel in matRels)
                {
                    AddMaterialData(matRel.RelatingMaterial, "");
                }
            }
        }

        private void AddMaterialData(IIfcMaterialSelect matSel, string setName)
        {
            if (matSel is IIfcMaterial) //simplest just add it
                Prop.Items.Add(new MetaItem
                {
                    Name = $"{((IIfcMaterial)matSel).Name} [#{matSel.EntityLabel}]",
                    PropertySetName = setName,
                    Value = ""
                });
            else if (matSel is IIfcMaterialLayer)
                Prop.Items.Add(new MetaItem
                {
                    Name = $"{((IIfcMaterialLayer)matSel).Material.Name} [#{matSel.EntityLabel}]",
                    Value = ((IIfcMaterialLayer)matSel).LayerThickness.Value.ToString(),
                    PropertySetName = setName
                });
            else if (matSel is IIfcMaterialList)
            {
                foreach (var mat in ((IIfcMaterialList)matSel).Materials)
                {
                    Prop.Items.Add(new MetaItem
                    {
                        Name = $"{mat.Name} [#{mat.EntityLabel}]",
                        PropertySetName = setName,
                        Value = ""
                    });
                }
            }
            else if (matSel is IIfcMaterialLayerSet)
            {
                foreach (var item in ((IIfcMaterialLayerSet)matSel).MaterialLayers) //recursive call to add materials
                {
                    AddMaterialData(item, ((IIfcMaterialLayerSet)matSel).LayerSetName);
                }
            }
            else if (matSel is IIfcMaterialLayerSetUsage)
            {
                //recursive call to add materials
                foreach (var item in ((IIfcMaterialLayerSetUsage)matSel).ForLayerSet.MaterialLayers)
                {
                    AddMaterialData(item, ((IIfcMaterialLayerSetUsage)matSel).ForLayerSet.LayerSetName);
                }
            }
        }
    }
}
