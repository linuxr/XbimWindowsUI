using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace XbinConverter.Export
{
    public class MetaItem
    {
        public string Units { get; set; }

        public string Name { get; set; }
        public string Value { get; set; }
        public string PropertySetName { get; set; }

        public int IfcLabel { get; set; }
    }

    public class UnityProperty
    {
        public int EntityLabel { get; set; }
        public List<MetaItem> Items { get; set; }
    }

    public class SpatialModel
    {
        // spatial name
        public string Name { get; set; }

        // entity label
        public int EntityLabel { get; set; }

        // parent entity label
        public int ParentEntityLabel { get; set; }

        // children
        public List<SpatialModel> Children { get; set; }
    }
    
    public class TreeAndProperties
    {
        // tree
        public SpatialModel ComponentTree { get; set; }

        // properties
        public List<UnityProperty> Properties { get; set; }
    }
}
