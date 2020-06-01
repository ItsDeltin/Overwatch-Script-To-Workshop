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
        // nodesOut and segmentsOut must equal the ID override in Modules/PathfindEditor.del:
        // line 312: define globalvar nodesOut    [3];
        // line 313: define globalvar segmentsOut [4];
        private const int nodesOut = 3;
        private const int segmentsOut = 4;

        public static PathMap ImportFromCSVFile(string file, IPathmapErrorHandler errorHandler) => ImportFromCSV(File.ReadAllText(file).Trim(), errorHandler);
        public static PathMap ImportFromCSV(string text, IPathmapErrorHandler errorHandler)
        {
            CsvFrame frame; 
            try {
                frame = CsvFrame.ParseOne(text);
            }
            catch (Exception) {
                errorHandler.Error("Incorrect CSV format.");
                return null;
            }

            if (frame.VariableSetOwner != "Global")
            {
                errorHandler.Error("Need the global variable set, got the '" + frame.VariableSetOwner + "' variable set instead.");
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
                    (int)Math.Round((segmentVector.Value.X % 1) * 100),
                    (int)Math.Round((segmentVector.Value.Y % 1) * 100)
                ));
            }
            
            return new PathMap(vectors.ToArray(), segments.ToArray());
        }

        public static PathMap ImportFromXMLFile(string file)
        {
            using (var reader = XmlReader.Create(file))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(PathMap));
                return (PathMap)serializer.Deserialize(reader);
            }
        }

        public static PathMap ImportFromXML(string xml)
        {
            using (var reader = XmlReader.Create(new StringReader(xml)))
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

        public Element NodesAsWorkshopData() => Element.CreateArray(
            Nodes.Select(node => node.ToVector()).ToArray()
        );
        public Element SegmentsAsWorkshopData() => Element.CreateArray(
            Segments.Select(segment => segment.AsWorkshopData()).ToArray()
        );
    }

    public class Segment
    {
        [XmlAttribute]
        public int Node1 { get; set; }
        [XmlAttribute]
        public int Node2 { get; set; }
        [XmlAttribute]
        public int Node1Attribute { get; set; }
        [XmlAttribute]
        public int Node2Attribute { get; set; }

        public Segment(int node1, int node2, int node1Attribute, int node2Attribute)
        {
            Node1 = node1;
            Node2 = node2;
            Node1Attribute = node1Attribute;
            Node2Attribute = node2Attribute;
        }

        private Segment() {}

        public bool ShouldSerializeNode1Attribute() => Node1Attribute != 0;
        public bool ShouldSerializeNode2Attribute() => Node2Attribute != 0;

        public V_Vector AsWorkshopData()
        {
            return new V_Vector((double)Node1 + (((double)Node1Attribute) / 100), (double)Node2 + (((double)Node2Attribute) / 100), 0);
        }
    }

    public interface IPathmapErrorHandler
    {
        void Error(string error);
    }
    
    class ConsolePathmapErrorHandler : IPathmapErrorHandler
    {
        private readonly Log log;

        public ConsolePathmapErrorHandler(Log log)
        {
            this.log = log;
        }

        public void Error(string error) => log.Write(LogLevel.Normal, new ColorMod("Error: " + error, ConsoleColor.Red));
    }
}