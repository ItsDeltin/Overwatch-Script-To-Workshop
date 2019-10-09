using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Deltin.Deltinteger.Csv;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Assets.Models;

namespace Deltin.Deltinteger.Pathfinder
{
    public class PathMap
    {
        private static readonly Log Log = new Log("PathMap");

        private static readonly Variable nodesOut = Variable.D;
        private static readonly Variable segmentsOut = Variable.E;

        public static PathMap ImportFromCSV(string file)
        {
            CsvFrame frame = CsvFrame.ParseOne(File.ReadAllText(file).Trim());

            if (frame.VariableSetOwner != "Global")
            {
                Log.Write(LogLevel.Normal, new ColorMod("Error: need the global variable set, got " + frame.VariableSetOwner + " instead.", ConsoleColor.Red));
                return null;
            }

            List<Vertex> vectors = new List<Vertex>();
            CsvArray nodeArray = frame.VariableValues[nodesOut] as CsvArray;

            for (int i = 0; i < nodeArray.Values.Length; i++)
            {
                CsvVector nodeVector = (CsvVector)nodeArray.Values[i];
                vectors.Add(nodeVector.Value);
            }
            
            List<Segment> segments = new List<Segment>();
            CsvArray segmentArray = frame.VariableValues[segmentsOut] as CsvArray;

            for (int i = 0; i < segmentArray.Values.Length; i++)
            {
                CsvVector segmentVector = (CsvVector)segmentArray.Values[i];

                segments.Add(new Segment(
                    (int)segmentVector.Value.X,
                    (int)segmentVector.Value.Y,
                    (int)segmentVector.Value.Z
                ));
            }
            
            return new PathMap(vectors.ToArray(), segments.ToArray());
        }

        public static PathMap ImportFromXML(string file)
        {
            using (var reader = XmlReader.Create(file))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(PathMap));
                return (PathMap)serializer.Deserialize(reader);
            }
        }

        public Vertex[] Nodes { get; set; }
        public Segment[] Segments { get; set; }

        public PathMap(Vertex[] nodes, Segment[] segments)
        {
            Nodes = nodes;
            Segments = segments;
        }

        private PathMap() {}

        public string ExportAsXML()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(PathMap));
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("","");

            string result;
            using (StringWriter stringWriter = new StringWriter())
            {
                serializer.Serialize(stringWriter, this, ns);
                result = stringWriter.ToString();
            }
            return result;
        }

        public Element NodesAsWorkshopData()
        {
            return Element.CreateArray(
                Nodes.Select(node => node.ToVector()).ToArray()
            );
        }
        public Element SegmentsAsWorkshopData()
        {
            return Element.CreateArray(
                Segments.Select(segment => segment.AsWorkshopData()).ToArray()
            );
        }
    }

    public class Segment
    {
        [XmlAttribute]
        public int Node1 { get; set; }
        [XmlAttribute]
        public int Node2 { get; set; }
        [XmlAttribute]
        public int Attribute { get; set; }

        public Segment(int node1, int node2, int attribute)
        {
            Node1 = node1;
            Node2 = node2;
            Attribute = attribute;
        }

        private Segment() {}

        public bool ShouldSerializeAttribute()
        {
            return Attribute != 0;
        }

        public V_Vector AsWorkshopData()
        {
            return new V_Vector((double)Node1, (double)Node2, (double)Attribute);
        }
    }
}