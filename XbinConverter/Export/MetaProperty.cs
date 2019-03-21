using System.Collections.Generic;
using System.Linq;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;

namespace XbinConverter.Export
{
    public class MetaProperty
    {
        public UnityProperty Prop { get; set; }

        public MetaProperty(IPersistEntity entity)
        {
            Prop = new UnityProperty
            {
                EntityLabel = entity.EntityLabel,
                Items = new List<MetaItem>()
            };

            if (entity is IIfcObject)
            {
                var asIfcObject = (IIfcObject)entity;
                foreach (
                    var pSet in
                        asIfcObject.IsDefinedBy.Select(
                            relDef => relDef.RelatingPropertyDefinition as IIfcPropertySet)
                    )
                    AddPropertySet(pSet);
            }
            else if (entity is IIfcTypeObject)
            {
                var asIfcTypeObject = entity as IIfcTypeObject;
                if (asIfcTypeObject.HasPropertySets == null)
                    return;
                foreach (var pSet in asIfcTypeObject.HasPropertySets.OfType<IIfcPropertySet>())
                {
                    AddPropertySet(pSet);
                }
            }
        }


        private void AddPropertySet(IIfcPropertySet pSet)
        {
            if (pSet == null)
                return;
            foreach (var item in pSet.HasProperties.OfType<IIfcPropertySingleValue>()) //handle IfcPropertySingleValue
            {
                AddProperty(item, pSet.Name);
            }
            foreach (var item in pSet.HasProperties.OfType<IIfcComplexProperty>()) // handle IfcComplexProperty
            {
                // by invoking the undrlying addproperty function with a longer path
                foreach (var composingProperty in item.HasProperties.OfType<IIfcPropertySingleValue>())
                {
                    AddProperty(composingProperty, pSet.Name + " / " + item.Name);
                }
            }
            foreach (var item in pSet.HasProperties.OfType<IIfcPropertyEnumeratedValue>()) // handle IfcComplexProperty
            {
                AddProperty(item, pSet.Name);
            }
        }

        private void AddProperty(IIfcPropertyEnumeratedValue item, string groupName)
        {
            var val = "";
            var nomVals = item.EnumerationValues;
            foreach (var nomVal in nomVals)
            {
                if (nomVal != null)
                    val = nomVal.ToString();
                Prop.Items.Add(new MetaItem
                {
                    IfcLabel = item.EntityLabel,
                    PropertySetName = groupName,
                    Name = item.Name,
                    Value = val
                });
            }
        }

        private void AddProperty(IIfcPropertySingleValue item, string groupName)
        {
            var val = "";
            var nomVal = item.NominalValue;
            if (nomVal != null)
                val = nomVal.ToString();
            Prop.Items.Add(new MetaItem
            {
                IfcLabel = item.EntityLabel,
                PropertySetName = groupName,
                Name = item.Name,
                Value = val
            });
        }
    }
}
