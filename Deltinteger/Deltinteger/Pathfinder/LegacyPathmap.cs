using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Deltin.Deltinteger.Models;

namespace Deltin.Deltinteger.Pathfinder
{
    [XmlRoot("PathMap")]
    public class LegacyPathmap
    {
        public Vertex[] Nodes { get; set; }
        [XmlArray]
        [XmlArrayItem(ElementName="Segment", Type = typeof(LegacySegment))]
        public LegacySegment[] Segments { get; set; }
        public MapAttribute[] Attributes { get; set; }

        public LegacyPathmap() {}

        public static bool TryLoad(string text, out Pathmap pathmap)
        {
            test();
            try
            {
                using (var reader = XmlReader.Create(new StringReader(text)))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(LegacyPathmap));
                    LegacyPathmap legacy = (LegacyPathmap)serializer.Deserialize(reader);

                    if ((legacy.Nodes != null && legacy.Nodes.Length > 0) || (legacy.Segments != null && legacy.Segments.Length > 0))
                    {
                        pathmap = legacy.AsPathmap();
                        return true;
                    }
                    pathmap = null;
                    return false;
                }
            }
            catch
            {
                pathmap = null;
                return false;
            }
        }

        public static bool TryLoadFile(string file, out Pathmap pathmap)
        {
            test();
            try
            {
                using (var reader = XmlReader.Create(file))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(LegacyPathmap));
                    LegacyPathmap legacy = (LegacyPathmap)serializer.Deserialize(reader);

                    if ((legacy.Nodes != null && legacy.Nodes.Length > 0) || (legacy.Segments != null && legacy.Segments.Length > 0))
                    {
                        pathmap = legacy.AsPathmap();
                        return true;
                    }
                    pathmap = null;
                    return false;
                }
            }
            catch
            {
                pathmap = null;
                return false;
            }
        }

        private static void test()
        {
            var ihate = new LegacyPathmap();
            ihate.Segments = new LegacySegment[] { new LegacySegment() { Node1 = 1, Node2 = 2, Node1Attribute = 3, Node2Attribute = 4 } };
            ihate.Nodes = new Vertex[] { new Vertex(1,2,3) };

            var stream = File.Create(@"C:\Users\Deltin\Desktop\delete later\testing_stuff.txt");

            XmlSerializer ser = new XmlSerializer(typeof(LegacyPathmap));
            ser.Serialize(stream, ihate);
        }

        Pathmap AsPathmap()
        {
            List<MapAttribute> attributes = new List<MapAttribute>(Attributes);
            foreach (var segment in Segments)
            {
                if (segment.Node1Attribute != 0) attributes.Add(new MapAttribute(segment.Node1, segment.Node2, segment.Node1Attribute));
                if (segment.Node2Attribute != 0) attributes.Add(new MapAttribute(segment.Node2, segment.Node1, segment.Node2Attribute));
            }

            return new Pathmap(Nodes, Segments.Select(s => new Segment(s.Node1, s.Node2)).ToArray(), attributes.ToArray());
        }
    }

    [XmlRoot(ElementName = "Segment")]
    public class LegacySegment
    {
        [XmlAttribute]
        public int Node1 { get; set; }
        [XmlAttribute]
        public int Node2 { get; set; }
        [XmlAttribute]
        public int Node1Attribute { get; set; }
        [XmlAttribute]
        public int Node2Attribute { get; set; }

        public LegacySegment() {}
    }
}