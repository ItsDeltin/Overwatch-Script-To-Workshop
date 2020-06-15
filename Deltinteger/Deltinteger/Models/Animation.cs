using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Models.Import;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.CustomMethods;

namespace Deltin.Deltinteger.Models
{
    public class Animation
    {
        private AnimatedLine[] AnimatedLines { get; }
        private AnimatedVertex[] Vertices { get; }
        public int Frames { get; }
        public double FPS { get; private set; }
        private A_Wait Wait { get; set; }
        public bool WasBuilt { get; private set; }

        public Animation(AnimatedLine[] animatedLines, AnimatedVertex[] vertices)
        {
            AnimatedLines = animatedLines;
            Vertices = vertices;
            Frames = Vertices[0].FramePoints.Length;
        }

        public void SetFPS(double fps, double skip)
        {
            FPS = fps;

            double time = 1.0 / FPS;
            if (skip > 0) time *= 1.0 + 1.0 / skip;
            else if (skip < 0) time *= -skip + 1;
            
            Wait = Element.Part<A_Wait>(new V_Number(time));
        }

        public AnimationBuild Create(ActionSet actionSet, Element visibleTo, Element location, IWorkshopTree reevaluation, EnumMember type, EnumMember color)
        {
            IndexReference currentFrame = actionSet.VarCollection.Assign("currentFrame", actionSet.IsGlobal, false);

            Element[] vertexElements = new Element[Vertices.Length];
            IndexReference[] vertexVariables = new IndexReference[Vertices.Length];

            TranslateRule current = null;

            for (int i = 0; i < vertexElements.Length; i++)
                if (!Vertices[i].Changes)
                {
                    // The vertex does not change location so a constant location is set.
                    vertexElements[i] = Vertices[i].Initial().ToVector();
                    vertexVariables[i] = null;
                }
                else
                {
                    if (i%5==0 || current == null)
                    {
                        if (current != null)
                            actionSet.Translate.DeltinScript.WorkshopRules.Add(current.GetRule());
                        
                        current = new TranslateRule(actionSet.Translate.DeltinScript, "init_animation_" + i + "-" + (i + 5), RuleEvent.OngoingGlobal);
                    }

                    // The vertex changes location in the animation, so set its location to a variable.
                    IndexReference indexReference = actionSet.VarCollection.Assign("Vertex", true, false);
                    
                    // Set the variable to the initial position
                    current.ActionSet.AddAction(indexReference.SetVariable(VertexArrayForVertex(i)));

                    vertexElements[i] = ((Element)indexReference.GetVariable())[(Element)currentFrame.GetVariable()];
                    vertexVariables[i] = indexReference;
                }
            
            actionSet.Translate.DeltinScript.WorkshopRules.Add(current.GetRule());
            
            IndexReference effects = actionSet.VarCollection.Assign("hi_andy", actionSet.IsGlobal, false);
            actionSet.AddAction(effects.SetVariable(new V_EmptyArray()));

            foreach (AnimatedLine line in AnimatedLines)
            {
                int vertex1 = Array.IndexOf(Vertices, line.Vertex1);
                int vertex2 = Array.IndexOf(Vertices, line.Vertex2);

                if (vertex1 == -1 || vertex2 == -1) throw new Exception();

                actionSet.AddAction(Element.Part<A_CreateBeamEffect>(
                    visibleTo,
                    type,
                    Element.Part<V_Add>(location, vertexElements[vertex1]),
                    Element.Part<V_Add>(location, vertexElements[vertex2]),
                    color,
                    reevaluation
                ));
                actionSet.AddAction(effects.ModifyVariable(Operation.AppendToArray, new V_LastCreatedEntity()));
                actionSet.AddAction(A_Wait.MinimumWait);
            }

            WasBuilt = true;

            return new AnimationBuild(vertexVariables, (Element)effects.GetVariable(), currentFrame);
        }

        private Element VertexArrayForVertex(int vertex)
        {
            Element[] positions = new Element[Frames];
            Element lastNotNull = null;
            for (int f = 0; f < Frames; f++)
            {
                positions[f] = Vertices[vertex].FramePoints[f]?.ToVector();
                if (positions[f] == null) positions[f] = lastNotNull;
                else lastNotNull = positions[f];
            }
            return Element.CreateArray(positions);
        }

        public void Animate(ActionSet actionSet, AnimationBuild builtAnimation, IWorkshopTree doLoop)
        {
            if (doLoop != null) actionSet.AddAction(Element.Part<A_While>(doLoop));

            actionSet.AddAction(builtAnimation.Current.SetVariable(new V_Number(0)));

            if (builtAnimation.Current.WorkshopVariable.IsGlobal)
                actionSet.AddAction(Element.Part<A_ForGlobalVariable>(
                    builtAnimation.Current.WorkshopVariable,
                    new V_Number(0), new V_Number(Frames - 1), new V_Number(1)
                ));
            // Player
            else
                actionSet.AddAction(Element.Part<A_ForPlayerVariable>(
                    new V_EventPlayer(),
                    builtAnimation.Current.WorkshopVariable,
                    new V_Number(0), new V_Number(Frames - 1), new V_Number(1)
                ));
            
            actionSet.AddAction(Wait);
            actionSet.AddAction(new A_End());

            if (doLoop != null)
            {
                actionSet.AddAction(Wait);
                actionSet.AddAction(new A_End());
            }
        }

        public static Animation ImportObjSequence(string folder, int skip)
        {
            string[] files = GetFiles(folder, skip);

            if (files.Length == 0) throw new Exception("No obj files found at " + folder);

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

        private static string[] GetFiles(string folder, int skip)
        {
            List<string> frames = new List<string>();
            string[] files = Directory.GetFiles(folder);
            int num = 0;
            foreach (string file in files)
            {
                if (Path.GetExtension(file) != ".obj") continue;
                string[] split = Path.GetFileNameWithoutExtension(file).Split('_');
                if (split.Length < 2) continue;
                if (!int.TryParse(split.Last(), out int f)) continue;

                if (skip == 0 || (skip > 0 ? (num + 1) % (skip + 1) != 0 : (num + 1) % (-skip + 1) == 0)) frames.Add(file);
                num++;
            }
            return frames.OrderBy(file => int.Parse(Path.GetFileNameWithoutExtension(file).Split('_').Last())).ToArray();
        }
    }

    public class AnimationBuild
    {
        /// Any element can potentially be null.
        public IndexReference[] VertexVariables { get; }
        public Element EffectArray { get; }
        public IndexReference Current { get; }

        public AnimationBuild(IndexReference[] vertexVariables, Element effectArray, IndexReference current)
        {
            VertexVariables = vertexVariables;
            EffectArray = effectArray;
            Current = current;
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

    [CustomMethod("CreateAnimation", "Makes an animation, I guess", CustomMethodType.MultiAction_Value)]
    class CreateAnimation : ModelCreator
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new FileParameter("animationFile", "The folder of the animation."),
            new CodeParameter("visibleTo", "The players the animation is visible to."),
            new CodeParameter("position", "The position to display the animation."),
            new CodeParameter("reevaluation", "The reevaluation of the created animation. Position needs to be reevaluated for the position to play.", ValueGroupType.GetEnumType<EffectRev>()),
            new CodeParameter("beamType", "The type of beam.", ValueGroupType.GetEnumType<BeamType>()),
            new CodeParameter("beamColor", "The color of the beam.", ValueGroupType.GetEnumType<Color>()),
            new CodeParameter("loopWhile", "Loops the animation while the specified condition is true.", defaultValue:null),
            new ConstNumberParameter("fps", "The frames per second of the animation. Must be a constant number value."),
            new ConstNumberParameter("frameSkip", "0 will skip no frames, 1 will skip ever other frame, 2 will skip every 3rd frame, 3 will skip every 4th frame, etc. Defaults to 0.", 0)
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues, object[] additional)
        {
            double fps = (double)additional[7];
            double frameSkip = (double)additional[8];

            Animation animation = Animation.ImportObjSequence(Path.GetDirectoryName((string)additional[0]), (int)frameSkip);

            animation.SetFPS(fps, frameSkip);

            Element visibleTo           = (Element)parameterValues[1];
            Element location            = (Element)parameterValues[2];
            EnumMember effectRev     = (EnumMember)parameterValues[3];
            EnumMember effectType     = (EnumMember)parameterValues[4];
            EnumMember effectColor = (EnumMember)parameterValues[5];

            List<Element> actions = new List<Element>();
            AnimationBuild build = animation.Create(actionSet, visibleTo, location, effectRev, effectType, effectColor);
            animation.Animate(actionSet, build, parameterValues[6]);

            return build.EffectArray;
        }
    }
}