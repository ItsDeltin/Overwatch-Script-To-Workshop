using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using Vertex = Deltin.Deltinteger.Models.Vertex;

namespace Deltin.Deltinteger.Animation
{
    public static class BlendStructureHelper
    {
        /// <summary>Converts an array of BlendVertex-es into an Element array where every i element is a vertex of the local location and every i + 1 element is an array of groups.</summary>
        public static Element VerticesToElementWithGroups(BlendVertex[] vertices)
        {
            var array = new Element[vertices.Length * 2];
            for (int i = 0; i < array.Length; i++)
            {
                array[i * 2] = vertices[i].Vertex.ToVector();
                
                // Get the groups.
                var groupArray = new Element[vertices[i].Groups.Length * 2];
                for (int g = 0; g < groupArray.Length; g++)
                {
                    groupArray[g * 2] = vertices[i].Groups[g].Group;
                    groupArray[(g * 2) + 1] = vertices[i].Groups[g].Weight;
                }

                // Add the group array.
                array[(i * 2) + 1] = Element.CreateArray(groupArray);
            }

            return Element.CreateArray(array);
        }

        /// <summary>Converts an array of BlendVertex-es to a Element vector array.</summary>
        public static Element VerticesToElement(BlendVertex[] vertices)
        {
            var array = new Element[vertices.Length];
            for (int i = 0; i < array.Length; i++)
                array[i] = vertices[i].Vertex.ToVector();
            return Element.CreateArray(array);
        }

        public static Element GetActionNames(AnimationAction[] actions) => Element.CreateArray(actions.Select(a => new V_CustomString(a.Name)).ToArray());
        public static Element GetActions(BlendObject blendObject) => Element.CreateArray(GetFcurveArray(blendObject, blendObject.AnimationData));

        public static Element GetFcurveArray(BlendObject blendObject, AnimationAction action)
        {
            var fcurves = new Element[action.FCurves.Length];
            for (int i = 0; i < fcurves.Length; i ++)
                fcurves[i] = GetKeyframeData(blendObject, action.FCurves[i]);
            return Element.CreateArray(fcurves);
        }

        /// <summary>Gets the keyframe data for an FCurve for the workshop.
        /// The array structure is [type, target, k0, k1, k2...]. k0, etc. are
        /// the keyframes structured [start, values...].</summary>
        public static Element GetKeyframeData(BlendObject blendObject, FCurve curve)
        {
            // fcurve: [type, target, k0, k1, k2...]
            var fcurveData = new List<Element>();

            // Add the type.
            fcurveData.Add((int)curve.FCurveType);

            // Add the target.
            switch (curve.FCurveType)
            {
                // Default: add empty.
                default:
                    fcurveData.Add(new V_False());
                    break;
                
                // For bone rotation, add the index of the bone.
                case FCurveType.BoneRotation:
                    fcurveData.Add(Array.FindIndex(((BlendArmature)blendObject).Bones, b => b.Name == curve.Target));
                    break;
            }

            // Add keyframes.
            foreach (var keyframe in curve.Keyframes)
            {
                // [start, values...]
                var keyframeData = new List<Element>();

                // Add the keyframe number.
                keyframeData.Add(keyframe.Start);

                // Add the value.
                switch (curve.FCurveType)
                {
                    // Location
                    case FCurveType.Location:
                        keyframeData.Add(keyframe.Value.ToObject<Vertex>().ToVector());
                        break;
                    
                    // Bone rotation
                    case FCurveType.BoneRotation:
                        var vertex = keyframe.Value.ToObject<Vertex>();
                        keyframeData.Add(vertex.ToVector());
                        keyframeData.Add(vertex.W);
                        break;
                }

                fcurveData.Add(Element.CreateArray(keyframeData.ToArray()));
            }
            return Element.CreateArray(fcurveData.ToArray());
        }
    }

    class BoneStructure
    {
        readonly BlendFile _file;
        readonly BlendArmature _armature;
        public readonly List<BonePoint> _pointData = new List<BonePoint>();
        public readonly List<BoneData> _boneData = new List<BoneData>();

        public BoneStructure(BlendFile file, BlendArmature armature)
        {
            _file = file;
            _armature = armature;
            LinkRootBones();
        }

        /// <summary>Creates a 3d array where the first dimension is the bone, the second dimension is the child mesh, and the
        /// third dimension is the vertex the bone interacts with.</summary>
        public Element GetBoneVertexData()
        {
            // Iterate through each bone.
            var boneArray = new Element[_armature.Bones.Length];
            for (int b = 0; b < boneArray.Length; b++)
            {
                // Iterate through each child.
                var meshArray = new Element[_armature.Children.Length];
                for (int m = 0; m < meshArray.Length; m++)
                {
                    var child = _file.Objects[_armature.Children[m]];
                    meshArray[m] = GetMeshData(_armature.Bones[b].Name, child);
                }
                boneArray[b] = Element.CreateArray(meshArray);
            }
            return Element.CreateArray(boneArray);
        }

        /// <summary>Called by 'GetBoneVertexData' to get an individual mesh's vertex data.</summary>
        Element GetMeshData(string boneName, BlendObject blendObject)
        {
            if (blendObject is BlendArmature)
                return new V_EmptyArray();
            
            var mesh = (BlendMesh)blendObject; // Get the mesh.

            // This array stores data in groups of 2, alternating between target vertex and weight.
            // [x] or [even] is the index of the vertex.
            // [x + 1] or [odd] is the weight of the vertex for the bone. 
            var boneInfoArray = new List<Element>();

            // Iterate through each vertex.
            for (int i = 0; i < mesh.Vertices.Length; i++)
                // Get a group with a matching name.
                foreach (var group in mesh.Vertices[i].Groups)
                    // Matching vertex group.
                    if (mesh.VertexGroups[group.Group].Name == boneName)
                    {
                        boneInfoArray.Add(i); // Add the index of the vertex.
                        boneInfoArray.Add(group.Weight); // Add the weight.
                        break; // Break out of the foreach.
                    }
            
            // Done.
            return Element.CreateArray(boneInfoArray.ToArray());
        }

        /// <summary>Obtains an array of names from the bones.</summary>
        public Element GetNameArray()
        {
            var names = new Element[_armature.Bones.Length];
            for (int i = 0; i < names.Length; i++)
                names[i] = new V_CustomString(_armature.Bones[i].Name);
            return Element.CreateArray(names);
        }

        /// <summary>Populates BoneData and PointData so connected bones don't need to have the same point recalculated twice.</summary>
        void LinkRootBones()
        {
            // We link the bones by choosing the bones without a parent then recursively
            // linking the children.
            // A bone without a parent will have their Parent value set to -1.
            var rootBones = _armature.Bones.Where(bone => bone.Parent == -1).ToArray();
            
            foreach (var bone in rootBones)
            {
                // Create a point for the head and tail of the bone.
                _pointData.Add(new BonePoint(bone, true));
                _pointData.Add(new BonePoint(bone, false));
                // _pointData.Add(bone.HeadLocal.ToVector());
                // _pointData.Add(bone.TailLocal.ToVector());

                // Create the bone link data then add it to the BoneData list.
                BoneData root = new BoneData(bone, _pointData.Count - 2, _pointData.Count - 1);
                _boneData.Add(root);

                // Link the children recursively.
                GetChildBoneData(root, bone);
            }
        }

        /// <summary>Recursively called by LinkRootBones to link the descendant bones.</summary>
        void GetChildBoneData(BoneData data, Bone bone)
        {
            // Iterate through each child.
            foreach(var childIndex in bone.Children)
            {
                var child = _armature.Bones[childIndex]; // Get the actual child from the index.
                var childData = new BoneData(child); // Create the bone link data.
                _boneData.Add(childData); // Add the newly-created bone line to the BoneData list.

                // If the child is connected, reuse the point at the parent's tail (data.Tail).
                if (child.IsConnected)
                    childData.Head = data.Tail;
                else // Otherwise, create a new point.
                {
                    _pointData.Add(new BonePoint(child, true));
                    // _pointData.Add(child.Head.ToVector());
                    childData.Head = _pointData.Count - 1;
                }

                // Create the tail point.
                _pointData.Add(new BonePoint(child, false));
                // _pointData.Add(child.Tail.ToVector());
                childData.Tail = _pointData.Count - 1;

                // Recursively get this bone's children.
                GetChildBoneData(childData, child);
            }
        }
    
        /// <summary>Creates a 2d array where the first dimension is the bone and the second dimension is an array of indices which is the bone's descendants.</summary>
        public Element GetBoneDescendents()
        {
            var boneDescendentArrays = new Element[_boneData.Count];

            // For each bone, create an array of indices which indicates which points will change then the bone translates.
            for (int i = 0; i < _boneData.Count; i++)
            {
                // The list of bone point indices.
                var childBonePoints = new List<Element>();
                childBonePoints.Add(_boneData[i].Tail);

                // Iterate through each bone.
                foreach (BoneData compare in _boneData)
                // Make sure the compare bone is a descendant of the current bone.
                if (_boneData[i] != compare && IsBoneDescendentOf(_boneData[i].Original, compare.Original))
                {
                    // If the head is not in the point list, add it.
                    if (!childBonePoints.Contains(compare.Head)) childBonePoints.Add(compare.Head);
                    // If the tail is not in the point list, add it.
                    if (!childBonePoints.Contains(compare.Tail)) childBonePoints.Add(compare.Tail);
                }

                boneDescendentArrays[i] = Element.CreateArray(childBonePoints.ToArray());
            }

            // Done.
            return Element.CreateArray(boneDescendentArrays);
        }

        /// <summary>Determines if a bone is a descendant of another bone.</summary>
        /// <returns>May return true if parent == descendant.</returns>
        bool IsBoneDescendentOf(Bone parent, Bone descendent)
        {
            while (true)
            {
                if (descendent == parent) return true;
                if (descendent.Parent == -1) return false;
                descendent = _armature.Bones[descendent.Parent];
            }
        }

        /// <summary>Gets an array of initial bone positions.</summary>
        public Element GetInitialBonePositions() => Element.CreateArray(_pointData.Select(p => p.Position.ToVector()).ToArray());
        public Element GetLocalArmaturePositions() => Element.CreateArray(_pointData.Select(p => p.LocalPosition.ToVector()).ToArray());
        // public Element GetParents() => Element.CreateArray(_armature.Bones.Select(b => new V_Number(b.Parent)).ToArray());
        public Element GetParents() => Element.CreateArray(_boneData.Select(b => new V_Number(b.Head)).ToArray());
    }

    class BonePoint
    {
        public Vertex Position { get; }
        public Vertex LocalPosition { get; }

        public BonePoint(Bone bone, bool head)
        {
            if (head)
            {
                Position = bone.Head;
                LocalPosition = bone.HeadLocal;
            }
            else
            {
                Position = bone.Tail;
                LocalPosition = bone.TailLocal;
            }
        }
    }

    class BoneData
    {
        public Bone Original { get; }
        public int Head { get; set; }
        public int Tail { get; set; }

        public BoneData(Bone original, int head, int tail)
        {
            Original = original;
            Head = head;
            Tail = tail;
        }

        public BoneData(Bone original)
        {
            Original = original;
        }
    }
}