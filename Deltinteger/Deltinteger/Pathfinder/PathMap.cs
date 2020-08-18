using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Serialization;
using Deltin.Deltinteger.Csv;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Models;
using Newtonsoft.Json;

namespace Deltin.Deltinteger.Pathfinder
{
    public class Pathmap
    {
        // nodesOut and segmentsOut must equal the ID override in Modules/PathfindEditor.del:
        // line 312: define globalvar nodesOut    [3];
        // line 313: define globalvar segmentsOut [4];
        private const int nodesOut = 0;
        private const int segmentsOut = 1;
        private const int attributesOut = 2;

        public static Pathmap ImportFromCSVFile(string file, IPathmapErrorHandler errorHandler) => ImportFromCSV(File.ReadAllText(file).Trim(), errorHandler);
        public static Pathmap ImportFromCSV(string text, IPathmapErrorHandler errorHandler)
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

            // Get nodes
            CsvArray nodeArray = frame.VariableValues[nodesOut] as CsvArray;

            if (nodeArray == null)
            {
                errorHandler.Error("Incorrect format, 'nodesOut' is not an array. Did you compile your pathmap?");
                return null;
            }

            Vertex[] vectors = new Vertex[nodeArray.Values.Length];
            for (int i = 0; i < nodeArray.Values.Length; i++)
            {
                CsvVector nodeVector = (CsvVector)nodeArray.Values[i];
                vectors[i] = nodeVector.Value;
            }
            
            // Get segments
            CsvArray segmentArray = frame.VariableValues[segmentsOut] as CsvArray;

            if (segmentArray == null)
            {
                errorHandler.Error("Incorrect format, 'segmentsOut' is not an array.");
                return null;
            }

            Segment[] segments = new Segment[segmentArray.Values.Length];
            for (int i = 0; i < segmentArray.Values.Length; i++)
            {
                CsvVector segmentVector = (CsvVector)segmentArray.Values[i];
                segments[i] = new Segment(
                    (int)segmentVector.Value.X,
                    (int)segmentVector.Value.Y
                );
            }
            
            // Get attributes
            CsvArray attributeArray = frame.VariableValues[attributesOut] as CsvArray;

            if (attributeArray == null)
            {
                errorHandler.Error("Incorrect format, 'attributesOut' is not an array.");
                return null;
            }

            MapAttribute[] attributes = new MapAttribute[attributeArray.Values.Length];
            for (int i = 0; i < attributeArray.Values.Length; i++)
            {
                CsvVector attributeVector = (CsvVector)attributeArray.Values[i];
                attributes[i] = new MapAttribute(
                    (int)attributeVector.Value.X,
                    (int)attributeVector.Value.Y,
                    (int)attributeVector.Value.Z
                );
            }
            
            return new Pathmap(vectors.ToArray(), segments.ToArray(), attributes);
        }

        public static Pathmap ImportFromText(string text)
        {
            if (LegacyPathmap.TryLoad(text, out Pathmap legacyMap)) return legacyMap;
            return Deserialize(text);
        }
        public static Pathmap ImportFromFile(string file)
        {
            if (LegacyPathmap.TryLoadFile(file, out Pathmap legacyMap)) return legacyMap;
            return Deserialize(System.IO.File.ReadAllText(file));
        }
        private static Pathmap Deserialize(string text) => JsonConvert.DeserializeObject<Pathmap>(text, new JsonSerializerSettings() {
            Formatting = Formatting.Indented
        });

        public Vertex[] Nodes { get; set; }
        public Segment[] Segments { get; set; }
        public MapAttribute[] Attributes { get; set; }

        public Pathmap(Vertex[] nodes, Segment[] segments, MapAttribute[] attributes)
        {
            Nodes = nodes;
            Segments = segments;
            Attributes = attributes;
        }

        private Pathmap() {}

        public string ExportAsJSON() => JsonConvert.SerializeObject(this);

        public Element NodesAsWorkshopData() => Element.CreateArray(
            Nodes.Select(node => node.ToVector()).ToArray()
        );
        public Element SegmentsAsWorkshopData() => Element.CreateArray(
            Segments.Select(segment => segment.AsWorkshopData()).ToArray()
        );
        public Element AttributesAsWorkshopData() => Element.CreateArray(
            Attributes.Select(attribute => attribute.AsWorkshopData()).ToArray()
        );
    }

    public class Segment
    {
        public int Node1 { get; set; }
        public int Node2 { get; set; }

        public Segment(int node1, int node2)
        {
            Node1 = node1;
            Node2 = node2;
        }

        private Segment() {}

        public V_Vector AsWorkshopData() => new V_Vector((double)Node1, (double)Node2, 0);
    }

    public class MapAttribute
    {
        [XmlAttribute]
        public int Node1 { get; set; }
        [XmlAttribute]
        public int Node2 { get; set; }
        [XmlAttribute]
        public int Attribute { get; set; }

        public MapAttribute(int node1, int node2, int attribute)
        {
            Node1 = node1;
            Node2 = node2;
            Attribute = attribute;
        }
        public MapAttribute() {}

        public Element AsWorkshopData() => new V_Vector(Node1, Node2, Attribute);
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