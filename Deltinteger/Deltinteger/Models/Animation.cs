using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Models.Import;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.Models
{
    public class Animation
    {
        private AnimatedLine[] AnimatedLines { get; }
        private AnimatedVertex[] Vertices { get; }
        public int Frames { get; }
        public int FPS { get; }
        private A_Wait Wait { get; }
        public bool WasBuilt { get; private set; }

        public Animation(AnimatedLine[] animatedLines, AnimatedVertex[] vertices)
        {
            AnimatedLines = animatedLines;
            Vertices = vertices;
            Frames = Vertices[0].FramePoints.Length;
            FPS = 3;
            Wait = Element.Part<A_Wait>(new V_Number(1.0/(double)FPS));
        }

        public AnimationBuild Create(TranslateRule context, ScopeGroup scope, Element visibleTo, Element location, IWorkshopTree reevaluation)
        {
            List<Element> actions = new List<Element>();

            Element[] vertexElements = new Element[Vertices.Length];
            IndexedVar[] vertexVariables = new IndexedVar[Vertices.Length];
            for (int i = 0; i < vertexElements.Length; i++)
                if (!Vertices[i].Changes)
                {
                    // The vertex does not change location so a constant location is set.
                    vertexElements[i] = Vertices[i].Initial().ToVector();
                    vertexVariables[i] = null;
                }
                else
                {
                    // The vertex changes location in the animation, so set its location to a variable.
                    IndexedVar indexedVar = context.VarCollection.AssignVar(scope, "Vertex", context.IsGlobal, null);
                    
                    // Set the variable to the initial position
                    actions.AddRange(indexedVar.SetVariable(Vertices[i].Initial().ToVector()));

                    vertexElements[i] = indexedVar.GetVariable();
                    vertexVariables[i] = indexedVar;
                }

            foreach (AnimatedLine line in AnimatedLines)
            {
                int vertex1 = Array.IndexOf(Vertices, line.Vertex1);
                int vertex2 = Array.IndexOf(Vertices, line.Vertex2);

                if (vertex1 == -1 || vertex2 == -1) throw new Exception();

                actions.Add(
                    Element.Part<A_CreateBeamEffect>(
                        visibleTo,
                        EnumData.GetEnumValue(BeamType.GrappleBeam),
                        Element.Part<V_Add>(location, vertexElements[vertex1]),
                        Element.Part<V_Add>(location, vertexElements[vertex2]),
                        EnumData.GetEnumValue(Elements.Color.Red),
                        reevaluation
                    )
                );
            }

            WasBuilt = true;

            return new AnimationBuild(actions.ToArray(), vertexVariables);
        }

        public Element[] Animate(AnimationBuild builtAnimation)
        {
            List<Element> actions = new List<Element>();

            for (int f = 0; f < Frames; f++)
            {
                for (int v = 0; v < Vertices.Length; v++)
                    if (Vertices[v].FramePoints[f] != null
                        && builtAnimation.VertexVariables[v] != null)
                    {
                        actions.AddRange(builtAnimation.VertexVariables[v].SetVariable(Vertices[v].FramePoints[f].ToVector()));
                    }

                actions.Add(Wait);
            }

            return actions.ToArray();
        }

        public static Animation ImportObjSequence(string folder)
        {
            string[] files = GetFiles(folder);

            if (files.Length == 0) throw new Exception();

            ObjModel[] keys = new ObjModel[files.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                string content = File.ReadAllText(files[i]);
                keys[i] = ObjModel.Import(content);
            }

            // Throw exception if the number of vertexes is not the same in every file.
            int vertexCount = keys[0].Vertices.Count;
            if (keys.Any(key => key.Vertices.Count != vertexCount)) throw new Exception();

            /*
            // Track which vertexes change locations.
            var changes = new bool[vertexCount];
            foreach (ObjModel key in keys)
                for (int i = 0; i < key.Vertices.Count; i++)
                    if (!keys[0].Vertices[i].EqualTo(key.Vertices[i]))
                        changes[i] = true;
            */
            
            AnimatedVertex[] animatedVertices = new AnimatedVertex[keys[0].Vertices.Count];
            for (int i = 0; i < animatedVertices.Length; i++)
            {
                Vertex[] vertices = new Vertex[keys.Length];
                for (int v = 0; v < vertices.Length; v++)
                {
                    bool updated = true;
                    if (v > 0)
                        updated = !keys[v].Vertices[i].EqualTo(keys[v - 1].Vertices[i]);
                    
                    if (updated)
                        vertices[v] = keys[v].Vertices[i];
                    else
                        vertices[v] = null;
                }
                animatedVertices[i] = new AnimatedVertex(vertices);
            }

            Line[] baseLines = keys[0].GetLines();
            var animatedLines = new AnimatedLine[baseLines.Length];
            for (int l = 0; l < animatedLines.Length; l++)
                animatedLines[l] = new AnimatedLine(
                    animatedVertices[baseLines[l].Vertex1Reference],
                    animatedVertices[baseLines[l].Vertex2Reference]
                );
            
            return new Animation(animatedLines, animatedVertices);
        }

        private static string[] GetFiles(string folder)
        {
            List<string> frames = new List<string>();
            string[] files = Directory.GetFiles(folder);
            foreach (string file in files)
            {
                if (Path.GetExtension(file) != ".obj") continue;
                string[] split = Path.GetFileNameWithoutExtension(file).Split('_');
                if (split.Length < 2) continue;
                if (!int.TryParse(split.Last(), out int f)) continue;
                frames.Add(file);
            }
            return frames.OrderBy(file => int.Parse(Path.GetFileNameWithoutExtension(file).Split('_').Last())).ToArray();
        }
    }

    public class AnimationBuild
    {
        public Element[] Actions { get; }
        /// Any element can potentially be null.
        public IndexedVar[] VertexVariables { get; }

        public AnimationBuild(Element[] actions, IndexedVar[] vertexVariables)
        {
            Actions = actions;
            VertexVariables = vertexVariables;
        }
    }

    public class AnimatedLine
    {
        public AnimatedVertex Vertex1 { get; }
        public AnimatedVertex Vertex2 { get; }

        public AnimatedLine(AnimatedVertex vertex1, AnimatedVertex vertex2)
        {
            Vertex1 = vertex1;
            Vertex2 = vertex2;
        }
    }

    public class AnimatedVertex
    {
        public Vertex[] FramePoints { get; }

        public bool Changes { get; } = false;

        public AnimatedVertex(Vertex[] framePoints)
        {
            if (framePoints == null) throw new ArgumentNullException(nameof(framePoints));
            if (framePoints.Length == 0) throw new ArgumentException("framePoints is empty.", nameof(framePoints));
            if (framePoints[0] == null) throw new ArgumentException("Initial frame cannot be null.", nameof(framePoints));

            FramePoints = framePoints;

            for (int i = 1; i < framePoints.Length; i++)
                if (framePoints[i] != null)
                {
                    Changes = true;
                    break;
                }
        }

        public Vertex Initial()
        {
            return FramePoints[0];
        }
    }

    [CustomMethod("CreateAnimation", CustomMethodType.Action)]
    [VarRefParameter("Animation")]
    [Parameter("Visible To", Elements.ValueType.Player, null)]
    [Parameter("Location", Elements.ValueType.Vector, null)]
    [EnumParameter("Reevaluation", typeof(EffectRev))]
    class CreateAnimation : ModelCreator
    {
        override protected MethodResult Get()
        {
            if (((VarRef)Parameters[0]).Var is AnimationVar == false)
                throw SyntaxErrorException.InvalidVarRefType(((VarRef)Parameters[0]).Var.Name, VarType.Model, ParameterLocations[0]);
            
            AnimationVar animation = (AnimationVar)((VarRef)Parameters[0]).Var;
            Element visibleTo           = (Element)Parameters[1];
            Element location            = (Element)Parameters[2];
            EnumMember effectRev     = (EnumMember)Parameters[3];

            List<Element> actions = new List<Element>();
            AnimationBuild build = animation.Animation.Create(TranslateContext, Scope, visibleTo, location, effectRev);
            actions.AddRange(build.Actions);
            actions.AddRange(animation.Animation.Animate(build));

            return new MethodResult(actions.ToArray(), null);
        }

        override public CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "zoo wee mama this is bad."
            );
        }
    }
}