using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Deltin.Deltinteger.Csv;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Models;

namespace Deltin.Deltinteger.Pathfinder
{
    public class PathMap
    {
        private static readonly Log Log = new Log("PathMap");

        private static readonly Variable IsBuilding = Variable.D;
        private static readonly Variable BuildingNodeIndex = Variable.E;
        private static readonly Variable BuildingNode = Variable.F;
        private static readonly Variable ConnectIndex = Variable.G;
        private static readonly Variable SegmentOut = Variable.H;

        public static PathMap ImportFromCSV(string file)
        {
            CsvFrame[] frames = CsvFrame.ParseSet(File.ReadAllLines(file));

            for (int i = 0; i < frames.Length; i++)
                if (frames[i].VariableSetOwner != "Global")
                {
                    Log.Write(LogLevel.Normal, new ColorMod("Error: need the global variable set, got " + frames[i].VariableSetOwner + " instead on line " + i + ".", ConsoleColor.Red));
                    Console.ReadLine();
                    return null;
                }

            bool started = false;
            int startAt = -1;
            for (int i = frames.Length - 1; i >= 0; i--)
            {
                if (frames[i].VariableValues[IsBuilding] is CsvBoolean && ((CsvBoolean)frames[i].VariableValues[IsBuilding]).Value)
                {
                    started = true;
                }
                else if (started)
                {
                    startAt = i + 1;
                    break;
                }
            }

            if (startAt == -1)
            {
                Log.Write(LogLevel.Normal, new ColorMod("Error: build not sure. Press Voice Line Up to compile the pathmap.", ConsoleColor.Red));
                Console.ReadLine();
            }

            int last = -1;

            List<Vertex> vectors = new List<Vertex>();
            for (int i = startAt; i < frames.Length; i++)
                if (frames[i].VariableValues[BuildingNodeIndex].Changed && frames[i].VariableValues[BuildingNode] is CsvVector)
                {
                    vectors.Add(((CsvVector)frames[i].VariableValues[BuildingNode]).Value);
                    last = i + 1;
                }
            
            List<Segment> segments = new List<Segment>();
            for (int i = last; i < frames.Length; i++)
                if (frames[i].VariableValues[ConnectIndex].Changed && frames[i].VariableValues[SegmentOut] is CsvVector)
                {
                    Vertex vector = ((CsvVector)frames[i].VariableValues[SegmentOut]).Value;

                    segments.Add(new Segment(
                        (int)vector.X,
                        (int)vector.Y,
                        (int)vector.Z
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