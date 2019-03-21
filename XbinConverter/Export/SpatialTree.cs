using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xbim.Ifc;
using Xbim.Ifc.ViewModels;
using Xbim.Ifc4.Interfaces;

namespace XbinConverter.Export
{
    public class SpatialTree
    {
        private SpatialModel _root;
        private List<MetaObject> _metaObjects;
        private List<MetaType> _metaTypes;
        private List<MetaProperty> _metaProperties;
        private List<MetaQuantity> _metaQuantities;
        private List<MetaMaterial> _metaMaterials;

        // export folder
        // private string _folderName;
        // model file name
        // private string _modelFileName;

        public SpatialTree(IfcStore store)
        {
            var project = store.Instances.OfType<IIfcProject>().FirstOrDefault();
            XbimModelViewModel sv = null;
            if (project != null)
            {
                sv = new XbimModelViewModel(project, null);
            }

            if (sv == null)
            {
                return;
            }

            // spatial
            _root = new SpatialModel
            {
                Name = sv.Name,
                EntityLabel = sv.EntityLabel,
                ParentEntityLabel = 0,
                Children = new List<SpatialModel>()
            };

            // meta objects
            _metaObjects = new List<MetaObject>();
            var mo = new MetaObject(sv.Entity);
            _metaObjects.Add(mo);

            // types
            _metaTypes = new List<MetaType>();
            var mt = new MetaType(sv.Entity);
            _metaTypes.Add(mt);

            // Properties
            _metaProperties = new List<MetaProperty>();
            var mp = new MetaProperty(sv.Entity);
            _metaProperties.Add(mp);

            // TODO Quantities
            _metaQuantities = new List<MetaQuantity>();
            var mq = new MetaQuantity(sv.Entity);
            _metaQuantities.Add(mq);

            // Materials
            _metaMaterials = new List<MetaMaterial>();
            var mm = new MetaMaterial(sv.Entity);
            _metaMaterials.Add(mm);

            InitModels(sv, _root);
        }

        // export json to gltf bin
        public void ExportJsonToBin(string fileName, string exportFilePath, bool withIndent)
        {
            using (var binaryWriter = new StreamWriter(new FileStream(exportFilePath, FileMode.Append)))
            // using (var binaryWriter = new BinaryWriter(new FileStream(fileName + ".bin", FileMode.Append)))
            {
                var tp = new TreeAndProperties
                {
                    Properties = new List<UnityProperty>(),
                    ComponentTree = _root
                };

                // spatial json

                var propMap = new Dictionary<int, List<MetaItem>>();
                // objects json uniq objects
                _metaObjects = _metaObjects.Where((x, i) => _metaObjects.FindIndex(z => z.Prop.EntityLabel == x.Prop.EntityLabel) == i).ToList();
                foreach (var obj in _metaObjects)
                {
                    if (obj.Prop.Items == null || obj.Prop.Items.Count == 0)
                        continue;

                    if (!propMap.ContainsKey(obj.Prop.EntityLabel))
                    {
                        propMap[obj.Prop.EntityLabel] = new List<MetaItem>();
                        propMap[obj.Prop.EntityLabel] = obj.Prop.Items;
                    }
                    else
                        propMap[obj.Prop.EntityLabel].AddRange(obj.Prop.Items);

                }

                // types json uniq types
                _metaTypes = _metaTypes.Where((x, i) => _metaTypes.FindIndex(z => z.Prop.EntityLabel == x.Prop.EntityLabel) == i).ToList();
                foreach (var obj in _metaTypes)
                {
                    if (obj.Prop.Items == null || obj.Prop.Items.Count == 0)
                        continue;

                    if (!propMap.ContainsKey(obj.Prop.EntityLabel))
                    {
                        propMap[obj.Prop.EntityLabel] = new List<MetaItem>();
                        propMap[obj.Prop.EntityLabel] = obj.Prop.Items;
                    }
                    else
                        propMap[obj.Prop.EntityLabel].AddRange(obj.Prop.Items);

                }

                // Properties json uniq Properties
                _metaProperties = _metaProperties.Where((x, i) =>
                    _metaProperties.FindIndex(z => z.Prop.EntityLabel == x.Prop.EntityLabel) == i).ToList();
                foreach (var obj in _metaProperties)
                {
                    if (obj.Prop.Items == null || obj.Prop.Items.Count == 0)
                        continue;

                    if (!propMap.ContainsKey(obj.Prop.EntityLabel))
                    {
                        propMap[obj.Prop.EntityLabel] = new List<MetaItem>();
                        propMap[obj.Prop.EntityLabel] = obj.Prop.Items;
                    }
                    else
                        propMap[obj.Prop.EntityLabel].AddRange(obj.Prop.Items);

                }

                // Quantities json uniq Quantities
                _metaQuantities = _metaQuantities.Where((x, i) => _metaQuantities.FindIndex(z => z.Prop.EntityLabel == x.Prop.EntityLabel) == i).ToList();
                foreach (var obj in _metaQuantities)
                {
                    if (obj.Prop.Items == null || obj.Prop.Items.Count == 0)
                        continue;

                    if (!propMap.ContainsKey(obj.Prop.EntityLabel))
                    {
                        propMap[obj.Prop.EntityLabel] = new List<MetaItem>();
                        propMap[obj.Prop.EntityLabel] = obj.Prop.Items;
                    }
                    else
                        propMap[obj.Prop.EntityLabel].AddRange(obj.Prop.Items);

                }

                // Materials json uniq Materials
                _metaMaterials = _metaMaterials.Where((x, i) => _metaMaterials.FindIndex(z => z.Prop.EntityLabel == x.Prop.EntityLabel) == i).ToList();
                foreach (var obj in _metaMaterials)
                {
                    if (obj.Prop.Items == null || obj.Prop.Items.Count == 0)
                        continue;

                    if (!propMap.ContainsKey(obj.Prop.EntityLabel))
                    {
                        propMap[obj.Prop.EntityLabel] = new List<MetaItem>();
                        propMap[obj.Prop.EntityLabel] = obj.Prop.Items;
                    }
                    else
                        propMap[obj.Prop.EntityLabel].AddRange(obj.Prop.Items);
                }

                // properties
                foreach (var item in propMap)
                {
                    var prop = new UnityProperty
                    {
                        EntityLabel = item.Key,
                        Items = FilterMetaItems(item.Value)
                    };
                    tp.Properties.Add(prop);
                }

                // write to file
                var json = withIndent? JsonConvert.SerializeObject(tp, Formatting.Indented) : JsonConvert.SerializeObject(tp);
                binaryWriter.Write(json);
            }
        }

        private void InitModels(IXbimViewModel xvm, SpatialModel parent)
        {
            foreach (var child in xvm.Children)
            {
                // meta object
                var mo = new MetaObject(child.Entity);
                _metaObjects.Add(mo);

                // types
                var mt = new MetaType(child.Entity);
                _metaTypes.Add(mt);

                // Properties
                var mp = new MetaProperty(child.Entity);
                _metaProperties.Add(mp);

                // Quantities
                var mq = new MetaQuantity(child.Entity);
                _metaQuantities.Add(mq);

                // Materials
                var mm = new MetaMaterial(child.Entity);
                _metaMaterials.Add(mm);

                // spatial tree
                var sm = new SpatialModel
                {
                    Name = child.Name,
                    EntityLabel = child.EntityLabel,
                    ParentEntityLabel =  parent.EntityLabel,
                    Children = new List<SpatialModel>()
                };
                parent.Children.Add(sm);

                InitModels(child, sm);
            }
        }

        private List<MetaItem> FilterMetaItems(List<MetaItem> items)
        {
            var newItems = new List<MetaItem>();
            foreach (var item in items)
            {
                if (!String.IsNullOrEmpty(item.Value) && !String.IsNullOrEmpty(item.Name))
                    newItems.Add(item);
            }

            return newItems;
        }
    }
}
