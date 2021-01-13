using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Serialization;
using Deltin.Deltinteger.Csv;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Models;
using Deltin.Deltinteger.Decompiler.TextToElement;
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

        public static Pathmap ImportFromActionSetFile(string file, IPathmapErrorHandler errorHandler) => ImportFromActionSet(File.ReadAllText(file).Trim(), errorHandler);
        public static Pathmap ImportFromActionSet(string text, IPathmapErrorHandler errorHandler)
        {
            ConvertTextToElement tte = new ConvertTextToElement(text);
            Workshop workshop = tte.GetActionList();

            Vertex[] nodeArray = null;
            Vertex[] segmentArray = null;
            Vertex[] attributeArray = null;

            const string nodesOut = "nodesOut", segmentsOut = "segmentsOut", attributesOut = "attributesOut";
            
            // Get the variable values.
            foreach (var action in workshop.Actions)
                if (action is SetVariableAction setVariable)
                {
                    // Get the variable name.
                    switch (setVariable.Variable.Name)
                    {
                        // Nodes
                        case nodesOut:
                            nodeArray = ExtractVertexArray(nodesOut, errorHandler, setVariable.Value);
                            break;
                        // Segments
                        case segmentsOut:
                            segmentArray = ExtractVertexArray(segmentsOut, errorHandler, setVariable.Value);
                            break;
                        // Attributes
                        case attributesOut:
                            attributeArray = ExtractVertexArray(attributesOut, errorHandler, setVariable.Value);
                            break;
                    }
                }

            if (nodeArray == null)
            {
                errorHandler.Error($"Incorrect format, '{nodesOut}' does not exist. Did you compile your pathmap?");
                return null;
            }
            if (segmentArray == null)
            {
                errorHandler.Error($"Incorrect format, '{segmentsOut}' does not exist. Did you compile your pathmap?");
                return null;
            }
            if (attributeArray == null)
            {
                errorHandler.Error($"Incorrect format, '{attributesOut}' does not exist. Did you compile your pathmap?");
                return null;
            }

            Segment[] segments = new Segment[segmentArray.Length];
            for (int i = 0; i < segments.Length; i++)
                segments[i] = new Segment((int)segmentArray[i].X, (int)segmentArray[i].Y);

            MapAttribute[] attributes = new MapAttribute[attributeArray.Length];
            for (int i = 0; i < attributes.Length; i++)
                attributes[i] = new MapAttribute(
                    (int)attributeArray[i].X,
                    (int)attributeArray[i].Y,
                    (int)attributeArray[i].Z
                );

            return new Pathmap(nodeArray, segments, attributes);
        }

        private static Vertex[] ExtractVertexArray(string variableName, IPathmapErrorHandler errorHandler, ITTEExpression expression)
        {
            if (expression is FunctionExpression arrayFunction && arrayFunction.Function.Name == "Array")
            {
                Vertex[] vertices = new Vertex[arrayFunction.Values.Length];
                // Loop through each value.
                for (int i = 0; i < arrayFunction.Values.Length; i++)
                    if (arrayFunction.Values[i] is FunctionExpression vectorFunction && vectorFunction.Function.Name == "Vector")
                    {
                        double x = ExtractVertexComponent(errorHandler, 0, vectorFunction),
                            y = ExtractVertexComponent(errorHandler, 1, vectorFunction),
                            z = ExtractVertexComponent(errorHandler, 2, vectorFunction);
                        
                        vertices[i] = new Vertex(x, y, z);
                    }
                    else
                    {
                        // The element is not a vector.
                        errorHandler.Error("An element in the " + variableName + " array is not a vector.");
                        return new Vertex[0];
                    }
                
                return vertices;
            }
            else
            {
                // The element is not an array.
                errorHandler.Error(variableName + " is not a vector array.");
                return new Vertex[0];
            }
        }

        private static double ExtractVertexComponent(IPathmapErrorHandler errorHandler, int component, FunctionExpression vectorFunction)
        {
            var componentName = new[] { "x", "y", "z" }[component];

            if (vectorFunction.Values.Length <= component)
            {
                errorHandler.Error($"The '{componentName}' component is out of range.");
                return 0;
            }
            // Get the component's number.
            else if (vectorFunction.Values[component] is NumberExpression numberExpression)
            {
                return numberExpression.Value;
            }
            else
            {
                errorHandler.Error($"The '{componentName}' component is not a number.");
                return 0;
            }
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
        private static Pathmap Deserialize(string text) => JsonConvert.DeserializeObject<Pathmap>(text);

        public Vertex[] Nodes { get; set; }
        public Segment[] Segments { get; set; }
        public MapAttribute[] Attributes { get; set; }

        public Pathmap(Vertex[] nodes, Segment[] segments, MapAttribute[] attributes)
        {
            Nodes = nodes;
            Segments = segments;
            Attributes = attributes;
        }

        private Pathmap() { }

        public string ExportAsJSON() => JsonConvert.SerializeObject(this, Formatting.Indented);

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

        private Segment() { }

        public Element AsWorkshopData() => Element.Vector((double)Node1, (double)Node2, 0);
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
        public MapAttribute() { }

        public Element AsWorkshopData() => Element.Vector(Node1, Node2, Attribute);
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