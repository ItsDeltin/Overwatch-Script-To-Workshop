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
        private static readonly Variable Connect1 = Variable.H;
        private static readonly Variable Connect2 = Variable.I;

        public static PathMap ImportFromCSV(string file)
        {
            CsvFrame[] frames = CsvFrame.ParseSet(File.ReadAllLines(file));

            for (int i = 0; i < frames.Length; i++)
                if (frames[i].VariableSetOwner != "Global")
                {
                    Log.Write(LogLevel.Normal, new ColorMod("Error: need the global variable set, got " + frames[i].VariableSetOwner + " instead on line " + i + ".", ConsoleColor.Red));
                    Console.ReadLine();
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
                if (frames[i].VariableValues[ConnectIndex].Changed && frames[i].VariableValues[Connect1] is CsvNumber && frames[i].VariableValues[Connect2] is CsvNumber)
                {
                    segments.Add(new Segment(
                        (int)((CsvNumber)frames[i].VariableValues[Connect1]).Value,
                        (int)((CsvNumber)frames[i].VariableValues[Connect2]).Value
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
    }

    public class Segment
    {
        [XmlAttribute]
        public int Node1 { get; set; }
        [XmlAttribute]
        public int Node2 { get; set; }

        public Segment(int node1, int node2)
        {
            Node1 = node1;
            Node2 = node2;
        }

        private Segment() {}
    }
}