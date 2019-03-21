using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using XbinConverter.Export;
using Xbim.Common.Exceptions;
using Xbim.Common.Geometry;
using Xbim.Common.Step21;
using Xbim.Common.XbimExtensions;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.ModelGeometry.Scene;

namespace XbinConverter
{
    public class ConverterGLB
    {
        private const int _magic = 9527;
        private const byte _version = 1;
        private int _instanceId = 1;
        private double _oneMeter = 1;

        // 编码方式: magic -> version -> color... END_OF_COLOR -> geometry... END_OF_GEOMETRY -> instance ... END_OF_INSTANCE -> property json

        public void SetOneMeter(double m)
        {
            _oneMeter = m;
        }

        public int GetIndexType(int count)
        {
            if (count <= byte.MaxValue)
                return 5121; //(UNSIGNED_BYTE)

            if (count <= ushort.MaxValue)
                return 5123; // (UNSIGNED_SHORT)

            return 5125; // (UNSIGNED_INT)
        }

        public float[] GetMatrix(XbimShapeInstance shape)
        {
            var transformation =
                shape.Transformation * new XbimMatrix3D(1, 0, 0, 0, 0, 0, -1, 0, 0, 1, 0, 0, 0, 0, 0, 1);


            return new[]
            {
                (float) transformation.M11,
                (float) transformation.M12,
                (float) transformation.M13,
                (float) transformation.M14,
                (float) transformation.M21,
                (float) transformation.M22,
                (float) transformation.M23,
                (float) transformation.M24,
                (float) transformation.M31,
                (float) transformation.M32,
                (float) transformation.M33,
                (float) transformation.M34,
                (float) ToMeter(transformation.OffsetX),
                (float) ToMeter(transformation.OffsetY),
                (float) ToMeter(transformation.OffsetZ),
                (float) transformation.M44
            };
        }

        public void Convert(string ifcPath)
        {
            using (var store = IfcStore.Open(ifcPath))
            {
                this.SetOneMeter(store.ModelFactors.OneMeter);
                this.WriteGeometries(store, ifcPath);

                var spaitialTree = new SpatialTree(store);
                spaitialTree.ExportJsonToBin(ifcPath, false);
            }
        }

        private double ToMeter(double d)
        {
            return d / _oneMeter;
        }


        private void WriteGeometry(BinaryWriter writer, XbimShapeGeometry geometry)
        {
            var ms = new MemoryStream(((IXbimShapeGeometryData) geometry).ShapeData);
            var br = new BinaryReader(ms);
            var tr = br.ReadShapeTriangulation();

            var typ = GetIndexType(tr.Vertices.Count);
            var indicesCount = tr.Faces.Sum(f => f.Indices.Count);

            // write geometry id
            writer.Write(geometry.ShapeLabel);
            // write vertices number
            writer.Write(tr.Vertices.Count);
            // write index type
            writer.Write(typ);
            // write indices count
            writer.Write(indicesCount);

            // write vertices
            foreach (var vertex in tr.Vertices)
            {
                writer.Write((float) ToMeter(vertex.X));
                writer.Write((float) ToMeter(vertex.Y));
                writer.Write((float) ToMeter(vertex.Z));
            }

            // write indices
            foreach (var face in tr.Faces)
            foreach (var index in face.Indices)
                if (typ == 5121)
                    writer.Write((byte) index);
                else if (typ == 5123)
                    writer.Write((ushort) index);
                else
                    writer.Write((uint) index);
        }

        public void WriteGeometries(IfcStore store, string fileName)
        {
            var c = new Xbim3DModelContext(store);
            c.CreateContext();
            if (store.GeometryStore == null)
                throw new XbimException("Geometry store has not been initialised");

            using (var binaryWriter = new BinaryWriter(new FileStream(fileName + ".bin", FileMode.Create)))
            using (var geomRead = store.GeometryStore.BeginRead())
            {
                WriteHeader(binaryWriter);
                WriteColors(binaryWriter, store, geomRead);
                var lookup = geomRead.ShapeGeometries;

                var prodIds = new HashSet<int>();
                foreach (var product in store.Instances.OfType<IIfcProduct>())
                {
                    if (product is IIfcFeatureElement)
                        continue;
                    prodIds.Add(product.EntityLabel);
                }

                var toIgnore = new short[4];
                toIgnore[0] = store.Metadata.ExpressTypeId("IFCOPENINGELEMENT");
                toIgnore[1] = store.Metadata.ExpressTypeId("IFCPROJECTIONELEMENT");

                if (store.SchemaVersion == XbimSchemaVersion.Ifc4)
                {
                    toIgnore[2] = store.Metadata.ExpressTypeId("IFCVOIDINGFEATURE");
                    toIgnore[3] = store.Metadata.ExpressTypeId("IFCSURFACEFEATURE");
                }
         
                var instancess = new List<List<XbimShapeInstance>>();

                foreach (var geometry in lookup)
                {
                    if (geometry == null || geometry.ShapeData.Length <= 0)
                        continue;

                    var xbimShapeInstances = geomRead.ShapeInstancesOfGeometry(geometry.ShapeLabel).Where(si =>
                        !toIgnore.Contains(si.IfcTypeId) &&
                        si.RepresentationType ==
                        XbimGeometryRepresentationType
                            .OpeningsAndAdditionsIncluded &&
                        prodIds.Contains(si.IfcProductLabel)).ToList();

                    if (!xbimShapeInstances.Any())
                        continue;

                    WriteGeometry(binaryWriter, geometry);

                    instancess.Add(xbimShapeInstances);
                }

                WriteTerm(binaryWriter, "END_OF_GEOMETRY");

                foreach (var instances in instancess)
                foreach (var instance in instances)
                {
                    var styleId = instance.StyleLabel > 0 ? instance.StyleLabel : -instance.IfcTypeId;
                    binaryWriter.Write(_instanceId++);
                    binaryWriter.Write(instance.IfcProductLabel);
                    binaryWriter.Write(styleId);
                    binaryWriter.Write(instance.ShapeGeometryLabel);
                    foreach (var f in GetMatrix(instance)) binaryWriter.Write(f);
                }

                WriteTerm(binaryWriter, "END_OF_INSTANCE");
            }
        }

        private void WriteColors(BinaryWriter writer, IfcStore store, IGeometryStoreReader geomRead)
        {
            var styles = geomRead.StyleIds;
            var allStyles = geomRead.ShapeInstances.Select(i => -i.IfcTypeId).Distinct().Concat(styles).ToList();

            // collect colors
            var colourMap = new XbimColourMap();
            foreach (var styleId in allStyles)
            {
                XbimColour color = null;
                if (styleId > 0)
                {
                    var ss = (IIfcSurfaceStyle) store.Instances[styleId];
                    var texture = XbimTexture.Create(ss);
                    color = texture.ColourMap.FirstOrDefault();
                }
                else
                {
                    var theType = store.Metadata.GetType((short) Math.Abs(styleId));
                    color = colourMap[theType.Name];
                }

                if (color == null) color = XbimColour.DefaultColour;

                writer.Write(styleId);
                writer.Write(color.Red);
                writer.Write(color.Green);
                writer.Write(color.Blue);
                writer.Write(color.Alpha);
            }

            WriteTerm(writer, "END_OF_COLOR");
        }

        private void WriteTerm(BinaryWriter writer, string label)
        {
            writer.Write(Encoding.ASCII.GetBytes(label));
        }

        private void WriteHeader(BinaryWriter writer)
        {
            writer.Write(_magic);
            writer.Write(_version);
        }
    }
}