using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Deltin.Deltinteger.Assets;

namespace Deltin.Deltinteger.Pathfinder
{
    [XmlRoot("PathMap")]
    public class LegacyPathmap
    {
        public Vertex[] Nodes { get; set; }
        [XmlArray]
        [XmlArrayItem(ElementName = "Segment", Type = typeof(LegacySegment))]
        public LegacySegment[] Segments { get; set; }
        public MapAttribute[] Attributes { get; set; }

        public LegacyPathmap() { }

        public static bool TryLoad(string text, out Pathmap pathmap)
        {
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

        Pathmap AsPathmap()
        {
            List<MapAttribute> attributes = new List<MapAttribute>(Attributes ?? new MapAttribute[0]);

            if (Segments != null)
                foreach (var segment in Segments)
                {
                    if (segment.Node1Attribute != 0) attributes.Add(new MapAttribute(segment.Node1, segment.Node2, segment.Node1Attribute));
                    if (segment.Node2Attribute != 0) attributes.Add(new MapAttribute(segment.Node2, segment.Node1, segment.Node2Attribute));
                }

            return new Pathmap((Nodes ?? new Vertex[0]), (Segments ?? new LegacySegment[0]).Select(s => new Segment(s.Node1, s.Node2)).ToArray(), (attributes ?? new List<MapAttribute>()).ToArray());
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

        public LegacySegment() { }
    }
}