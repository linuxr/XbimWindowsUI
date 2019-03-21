using System.Collections.Generic;
using System.Linq;
using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.Ifc4.Interfaces;

namespace XbinConverter.Export
{
    public class MetaObject
    {
        public UnityProperty Prop { get; set; }

        public MetaObject(IPersistEntity entity)
        {
            Prop = new UnityProperty
            {
                EntityLabel = entity.EntityLabel,
                Items = new List<MetaItem>
                {
                    new MetaItem {Name = "Ifc Label", Value = "#" + entity.EntityLabel, PropertySetName = "General"}
                }
            };


            var ifcType = entity.ExpressType;
            Prop.Items.Add(new MetaItem { Name = "Type", Value = ifcType.Type.Name, PropertySetName = "General" });

            var ifcObj = entity as IIfcObject;
            var typeEntity = ifcObj?.IsTypedBy.FirstOrDefault()?.RelatingType;
            if (typeEntity != null)
            {
                Prop.Items.Add(
                    new MetaItem
                    {
                        Name = "Defining Type",
                        Value = typeEntity.Name,
                        PropertySetName = "General",
                        IfcLabel = typeEntity.EntityLabel
                    }
                );
            }

            var props = ifcType.Properties.Values;
            foreach (var prop in props)
            {
                ReportProp(entity, prop, false);
            }
            var invs = ifcType.Inverses;

            foreach (var inverse in invs)
            {
                ReportProp(entity, inverse, false);
            }
        }
        
        private void ReportProp(IPersistEntity entity, ExpressMetaProperty prop, bool verbose)
        {
            var propVal = prop.PropertyInfo.GetValue(entity, null);
            if (propVal == null)
            {
                if (!verbose)
                    return;
                propVal = "<null>";
            }

            if (prop.EntityAttribute.IsEnumerable)
            {
                var propCollection = propVal as System.Collections.IEnumerable;

                if (propCollection != null)
                {
                    var propVals = propCollection.Cast<object>().ToArray();
                    switch (propVals.Length)
                    {
                        case 0:
                            if (!verbose)
                                return;
                            Prop.Items.Add(new MetaItem { Name = prop.PropertyInfo.Name, Value = "<empty>", PropertySetName = "General" });
                            break;
                        case 1:
                            var tmpSingle = GetMetaItem(propVals[0]);
                            tmpSingle.Name = prop.PropertyInfo.Name + "[0]";
                            tmpSingle.PropertySetName = "General";
                            Prop.Items.Add(tmpSingle);
                            break;
                        default:
                            int i = 0;
                            foreach (var item in propVals)
                            {
                                var tmpLoop = GetMetaItem(item);
                                tmpLoop.Name = $"{prop.PropertyInfo.Name}[{i++}]";
                                tmpLoop.PropertySetName = prop.PropertyInfo.Name;
                                Prop.Items.Add(tmpLoop);
                            }
                            break;
                    }
                }
                else
                {
                    if (!verbose)
                        return;
                    Prop.Items.Add(new MetaItem { Name = prop.PropertyInfo.Name, Value = "<not an enumerable>" });
                }
            }
            else
            {
                var tmp = GetMetaItem(propVal);
                tmp.Name = prop.PropertyInfo.Name;
                tmp.PropertySetName = "General";
                Prop.Items.Add(tmp);
            }
        }

        private MetaItem GetMetaItem(object propVal)
        {
            var retItem = new MetaItem();

            var pe = propVal as IPersistEntity;
            var propLabel = 0;
            if (pe != null)
            {
                propLabel = pe.EntityLabel;
            }
            var ret = propVal.ToString();
            if (ret == propVal.GetType().FullName)
            {
                ret = propVal.GetType().Name;
            }

            if (pe is IIfcRepresentation)
            {
                var t = (IIfcRepresentation)pe;
                ret += $" ('{t.RepresentationIdentifier}' '{t.RepresentationType}')";
            }
            else if (pe is IIfcRelDefinesByProperties)
            {
                var t = (IIfcRelDefinesByProperties)pe;
                var stringValues = new List<string>();
                var name = t.RelatingPropertyDefinition?.PropertySetDefinitions.FirstOrDefault()?.Name;
                if (!string.IsNullOrEmpty(name))
                    stringValues.Add($"'{name}'");
                if (stringValues.Any())
                {
                    ret += $" ({string.Join(" ", stringValues.ToArray())})";
                }
            }
            else if (pe is IIfcRoot)
            {
                var t = (IIfcRoot)pe;
                var stringValues = new List<string>();
                if (!string.IsNullOrEmpty(t.Name))
                    stringValues.Add($"'{t.Name}'");
                if (!string.IsNullOrEmpty(t.GlobalId))
                    stringValues.Add($"'{t.GlobalId}'");
                if (stringValues.Any())
                {
                    ret += $" ({string.Join(" ", stringValues.ToArray())})";
                }
            }
            else if (pe is IIfcOwnerHistory)
            {
                var t = (IIfcOwnerHistory)pe;
                var stringValues = new List<string>();
                if (!string.IsNullOrEmpty(t.OwningUser?.EntityLabel.ToString()))
                    stringValues.Add($"#{t.OwningUser?.EntityLabel.ToString()}");
                if (!string.IsNullOrEmpty(t.OwningApplication?.ApplicationIdentifier))
                    stringValues.Add($"'{t.OwningApplication?.ApplicationIdentifier}'");
                if (stringValues.Any())
                {
                    ret += $" ({string.Join(" using ", stringValues.ToArray())})";
                }
            }
            retItem.Value = ret;
            retItem.IfcLabel = propLabel;
            return retItem;
        }
    }
}
