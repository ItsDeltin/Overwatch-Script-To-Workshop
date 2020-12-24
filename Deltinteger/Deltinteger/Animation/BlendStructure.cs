using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vertex = Deltin.Deltinteger.Models.Vertex;

namespace Deltin.Deltinteger.Animation
{
    /// <summary>The root of a blend file.</summary>
    public class BlendFile
    {
        /// <summary>The objects in the .blend file.</summary>
        [JsonProperty("objects")]
        public BlendObject[] Objects { get; set; }

        /// <summary>Gets a BlendFile from a json string.</summary>
        public static BlendFile FromJson(string json) => JsonConvert.DeserializeObject<BlendFile>(json, new BlendObjectConverter());
    }

    /// <summary>An abstract class that is implemented by either a mesh or an armature.</summary>
    public abstract class BlendObject
    {
        /// <summary>The name of the mesh or armature.</summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>The animation data of the mesh or armature.</summary>
        [JsonProperty("animation_data")]
        public AnimationAction[] AnimationData { get; set; }

        /// <summary>The children of the BlendObject. The values are the indices of the BlendFile's objects.</summary>
        [JsonProperty("children")]
        public int[] Children { get; set; }

        /// <summary>The parent of the BlendObject. The value is the index of the BlendFile's objects.</summary>
        [JsonProperty("parent")]
        public int Parent { get; set; }
    }

    /// <summary>A mesh containing model data.</summary>
    public class BlendMesh : BlendObject
    {
        /// <summary>The vertex groups of the mesh.</summary>
        [JsonProperty("vertex_groups")]
        public VertexGroup[] VertexGroups { get; set; }
        /// <summary>The vertices of the mesh.</summary>
        [JsonProperty("vertices")]
        public BlendVertex[] Vertices { get; set; }
        /// <summary>The edges of the mesh.</summary>
        [JsonProperty("edges")]
        public Edge[] Edges { get; set; }
    }

    // This is currently unused, this will be used when multiple AnimationActions are supported via nla tracks.
    /// <summary>Contains animation data about a mesh or armature.</summary>
    public class AnimationData {}

    /// <summary>Contains the animation data of an animated action.</summary>
    public class AnimationAction
    {
        /// <summary>The name of the action.</summary>
        [JsonProperty("name")]
        public string Name { get; set; }
        /// <summary>The individual F-Curves that make up the action.</summary>
        [JsonProperty("fcurves")]
        public FCurve[] FCurves { get; set; }
        /// <summary>The final frame range of all F-Curves within this action.</summary>
        [JsonProperty("frame_range")]
        public Vertex FrameRange { get; set; }
    }

    /// <summary>F-Curve defining values of a period of time.</summary>
    public class FCurve
    {
        /// <summary>The type of the f-curve.</summary>
        [JsonProperty("fcurve_type")]
        public FCurveType FCurveType { get; set; }
        /// <summary>An arbritrary string that determines what this f-curve is interacting with.
        /// If FCurveType == Location, this will be null.
        /// If FCurveType == BoneRotation, this will equal the name of the bone whose rotation is being affected.</summary>
        [JsonProperty("target")]
        public string Target { get; set; }
        /// <summary>The keyframes of the F-Curve.</summary>
        [JsonProperty("keyframes")]
        public Keyframe[] Keyframes { get; set; }
        /// <summary>The range of the curve. X is the starting keyframe, Y is the ending keyframe.</summary>
        [JsonProperty("range")]
        public Vertex Range { get; set; }
    }

    /// <summary>The type of an F-Curve.</summary>
    public enum FCurveType
    {
        /// <summary>Changes the location of the mesh or armature. The value of the f-curve's keyframes will be of type Vertex (quaternion).</summary>
        [JsonProperty("location")]
        Location = 0,
        /// <summary>Changes the rotation of a bone. The value of the f-curve's keyframes will be of type Vertex (quaternion).</summary>
        [JsonProperty("bone_rotation")]
        BoneRotation = 1,
        /// <summary>Changes the location of a bone. The value of the f-curve's keyframes will be of type Vertex.</summary>
        [JsonProperty("bone_location")]
        BoneLocation = 2
    }

    /// <summary>A point in an f-curve.</summary>
    public class Keyframe
    {
        /// <summary>The frame of this keyframe.</summary>
        [JsonProperty("start")]
        public int Start { get; set; }
        /// <summary>The value of the keyframe. If the FCurveType == Location, this will be a vector.
        /// If the FCurveType == BoneRotation, this will be a quaternion.</summary>
        [JsonProperty("value")]
        public JObject Value { get; set; }
    }

    /// <summary>Contains data about a vertex group.</summary>
    public class VertexGroup
    {
        /// <summary>The index of the vertex group.
        /// The vertex of a model will have an array of indexes equal to the relative VertexGroup.</summary>
        [JsonProperty("index")]
        public int Index { get; set; }
        /// <summary>The name of the vertex group. Any bones with an identicle name will interact with this group.</summary>
        [JsonProperty("name")]
        public string Name { get; set; }
    }

    /// <summary>Contains data about a group a vertex is assigned to.</summary>
    public class VertexGroupElement
    {
        /// <summary>The index of the group.</summary>
        [JsonProperty("group")]
        public int Group { get; set; }
        /// <summary>The weight of the group.</summary>
        [JsonProperty("weight")]
        public double Weight { get; set; }
    }

    /// <summary>A vertex in a mesh.</summary>
    public class BlendVertex
    {
        /// <summary>The actual local location of the vertex.</summary>
        [JsonProperty("co")]
        public Vertex Vertex { get; set; }
        /// <summary>The groups that the vertex is assigned to.</summary>
        [JsonProperty("groups")]
        public VertexGroupElement[] Groups { get; set; }
    }

    /// <summary>Contains the indices of 2 vertices to make up an edge..</summary>
    public class Edge
    {
        /// <summary>The first vertex.</summary>
        [JsonProperty("v1")]
        public int Vertex1 { get; set; }
        /// <summary>The second vertex.</summary>
        [JsonProperty("v2")]
        public int Vertex2 { get; set; }
    }

    /// <summary>An armature containing bone data.</summary>
    public class BlendArmature : BlendObject
    {
        /// <summary>The bones in the armature.</summary>
        [JsonProperty("bones")]
        public Bone[] Bones { get; set; }
        /// <summary>The empties in the armature.</summary>
        [JsonProperty("empties")]
        public BoneEmpty[] Empties { get; set; }
    }

    /// <summary>A bone in the armature.</summary>
    public class Bone
    {
        /// <summary>The name of the bone.
        /// Any direct child meshes who have vertex groups sharing the same name will have their vertexes linked with the bone.</summary>
        [JsonProperty("name")]
        public string Name { get; set; }
        /// <summary>The location of the head of the bone relative to its parent.</summary>
        [JsonProperty("head")]
        public Vertex Head { get; set; }
        /// <summary>The location of the tail of the bone relative to its parent.</summary>
        [JsonProperty("tail")]
        public Vertex Tail { get; set; }
        /// <summary>The location of the head of the bone relative to the armature.</summary>
        [JsonProperty("head_local")]
        public Vertex HeadLocal { get; set; }
        /// <summary>The location of the tail of the bone relative to the armature.</summary>
        [JsonProperty("tail_local")]
        public Vertex TailLocal { get; set; }
        /// <summary>The length of the bone.</summary>
        [JsonProperty("length")]
        public double Length { get; set; }
        /// <summary>The matrix of the bone.</summary>
        [JsonProperty("matrix")]
        public double[] Matrix { get; set; }
        /// <summary>The children of the Bone. The values are the indices of the Armature's bones.</summary>
        [JsonProperty("children")]
        public int[] Children { get; set; }
        /// <summary>The parent of the Bone. The value is the index of the Armature's bones.</summary>
        [JsonProperty("parent")]
        public int Parent { get; set; }
        /// <summary>When the bone has a parent, the bone's head is stuck to the parent's tail.</summary>
        [JsonProperty("use_connect")]
        public bool IsConnected { get; set; }

        public Vertex TailRelative => TailLocal - HeadLocal;
    }

    /// <summary>A point linked to a bone in an armature.</summary>
    public class BoneEmpty
    {
        /// <summary>The name of the empty.</summary>
        [JsonProperty("name")]
        public string Name { get; set; }
        /// <summary>The location of the empty relative to the bone it is connected to.</summary>
        [JsonProperty("location")]
        public Vertex Location { get; set; }
        /// <summary>The location of the empty relative to the armature.</summary>
        [JsonProperty("location_local")]
        public Vertex LocalLocation { get; set; }
        /// <summary>The name of the bone that the empty is connected to.</summary>
        [JsonProperty("parent_bone")]
        public string ParentBone { get; set; }
    }

    /// <summary>Deserializes BlendObjects.</summary>
    class BlendObjectConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(BlendObject);
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new NotImplementedException();

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);

            // If the 'bones' property exists, then this is an armature. Otherwise, it is a mesh.
            BlendObject blendObject = jsonObject.ContainsKey("bones") ? (BlendObject)new BlendArmature() : new BlendMesh();
            
            serializer.Populate(jsonObject.CreateReader(), blendObject);
            return blendObject;
        }
    }
}