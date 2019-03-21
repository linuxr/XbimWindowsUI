using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;

namespace XbinConverter.Export
{
    public class MetaQuantity
    {
        public UnityProperty Prop { get; set; }

        public MetaQuantity(IPersistEntity entity)
        {
            Prop = new UnityProperty
            {
                EntityLabel = entity.EntityLabel,
                Items = new List<MetaItem>()
            };
            var o = entity as IIfcObject;
            if (o != null)
            {
                var ifcObj = o;
                var modelUnits = entity.Model.Instances.OfType<IIfcUnitAssignment>().FirstOrDefault();
                // not optional, should never return void in valid model

                foreach (
                    var relDef in
                        ifcObj.IsDefinedBy.Where(r => r.RelatingPropertyDefinition is IIfcElementQuantity))
                {
                    var pSet = relDef.RelatingPropertyDefinition as IIfcElementQuantity;
                    AddQuantityPSet(pSet, modelUnits);
                }
            }
            else if (entity is IIfcTypeObject)
            {
                var asIfcTypeObject = entity as IIfcTypeObject;
                var modelUnits = entity.Model.Instances.OfType<IIfcUnitAssignment>().FirstOrDefault();
                // not optional, should never return void in valid model

                if (asIfcTypeObject.HasPropertySets == null)
                    return;
                foreach (var pSet in asIfcTypeObject.HasPropertySets.OfType<IIfcElementQuantity>())
                {
                    AddQuantityPSet(pSet, modelUnits);
                }

                //foreach (var relDef in ifcObj. IsDefinedByProperties.Where(r => r.RelatingPropertyDefinition is IfcElementQuantity))
                //{
                //    var pSet = relDef.RelatingPropertyDefinition as IfcElementQuantity;
                //    AddQuantityPSet(pSet, modelUnits);
                //}
            }
        }

        private void AddQuantityPSet(IIfcElementQuantity pSet, IIfcUnitAssignment modelUnits)
        {
            if (pSet == null)
                return;
            if (modelUnits == null) throw new ArgumentNullException(nameof(modelUnits));
            foreach (var item in pSet.Quantities.OfType<IIfcPhysicalSimpleQuantity>())
            // currently only handles IfcPhysicalSimpleQuantity
            {
                Prop.Items.Add(new MetaItem
                {
                    PropertySetName = pSet.Name,
                    Name = item.Name,
                    Value = GetValueString(item, modelUnits)
                });
            }
        }

        private static string GetValueString(IIfcPhysicalSimpleQuantity quantity, IIfcUnitAssignment modelUnits)
        {
            if (quantity == null)
                return "";
            string value = null;
            var unitName = "";
            var u = quantity.Unit;
            if (quantity.Unit != null)
                unitName = quantity.Unit.FullName;

            var length = quantity as IIfcQuantityLength;
            if (length != null)
            {
                value = length.LengthValue.ToString();
                if (quantity.Unit == null)
                    unitName = GetUnit(modelUnits, IfcUnitEnum.LENGTHUNIT);
            }
            var area = quantity as IIfcQuantityArea;
            if (area != null)
            {
                value = area.AreaValue.ToString();
                if (quantity.Unit == null)
                    unitName = GetUnit(modelUnits, IfcUnitEnum.AREAUNIT);
            }
            var weight = quantity as IIfcQuantityWeight;
            if (weight != null)
            {
                value = weight.WeightValue.ToString();
                if (quantity.Unit == null)
                    unitName = GetUnit(modelUnits, IfcUnitEnum.MASSUNIT);
            }
            var time = quantity as IIfcQuantityTime;
            if (time != null)
            {
                value = time.TimeValue.ToString();
                if (quantity.Unit == null)
                    unitName = GetUnit(modelUnits, IfcUnitEnum.TIMEUNIT);
            }
            var volume = quantity as IIfcQuantityVolume;
            if (volume != null)
            {
                value = volume.VolumeValue.ToString();
                if (quantity.Unit == null)
                    unitName = GetUnit(modelUnits, IfcUnitEnum.VOLUMEUNIT);
            }
            var count = quantity as IIfcQuantityCount;
            if (count != null)
                value = count.CountValue.ToString();


            if (string.IsNullOrWhiteSpace(value))
                return "";

            return string.IsNullOrWhiteSpace(unitName) ?
                value :
                $"{value} {unitName}";
        }

        private static string GetUnit(IIfcUnitAssignment units, IfcUnitEnum type)
        {
            var unit = units?.Units.OfType<IIfcNamedUnit>().FirstOrDefault(u => u.UnitType == type);
            return unit?.FullName;
        }
    }
}
