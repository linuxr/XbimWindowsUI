using System.Collections.Generic;
using System.Linq;
using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.Ifc4.Interfaces;

namespace XbinConverter.Export
{
    public class MetaType
    {
        public UnityProperty Prop { get; set; }

        public MetaType(IPersistEntity entity)
        {
            Prop = new UnityProperty
            {
                EntityLabel = entity.EntityLabel,
                Items = new List<MetaItem>()
            };

            var ifcObj = entity as IIfcObject;
            var typeEntity = ifcObj?.IsTypedBy.FirstOrDefault()?.RelatingType;
            if (typeEntity == null)
                return;
            var ifcType = typeEntity?.ExpressType;

            Prop.Items.Add(new MetaItem { Name = "Type", Value = ifcType.Type.Name });
            Prop.Items.Add(new MetaItem { Name = "Ifc Label", Value = "#" + typeEntity.EntityLabel });

            Prop.Items.Add(new MetaItem { Name = "Name", Value = typeEntity.Name });
            Prop.Items.Add(new MetaItem { Name = "Description", Value = typeEntity.Description });
            Prop.Items.Add(new MetaItem { Name = "GUID", Value = typeEntity.GlobalId });
            if (typeEntity.OwnerHistory != null)
            {
                Prop.Items.Add(new MetaItem
                {
                    Name = "Ownership",
                    Value =
                       typeEntity.OwnerHistory.OwningUser + " using " +
                       typeEntity.OwnerHistory.OwningApplication.ApplicationIdentifier
                });
            }
            //now do properties in further specialisations that are text labels
            foreach (var pInfo in ifcType.Properties.Where
                (p => p.Value.EntityAttribute.Order > 4
                      && p.Value.EntityAttribute.State != EntityAttributeState.DerivedOverride)
                ) //skip the first for of root, and derived and things that are objects
            {
                var val = pInfo.Value.PropertyInfo.GetValue(typeEntity, null);
                if (!(val is ExpressType))
                    continue;
                var pi = new MetaItem { Name = pInfo.Value.PropertyInfo.Name, Value = ((ExpressType)val).ToString() };
                Prop.Items.Add(pi);
            }
        }
    }
}
