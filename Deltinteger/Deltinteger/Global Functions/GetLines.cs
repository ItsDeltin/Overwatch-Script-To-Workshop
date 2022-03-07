using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Cache;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Assets;

namespace Deltin.Deltinteger.GlobalFunctions
{
    partial class GlobalFunctions
    {
        public static FuncMethod GetLines(DeltinScript deltinScript)
        {
            ITypeSupplier types = deltinScript.Types;

            return new FuncMethodBuilder()
            {
                Name = "GetLines",
                Documentation = new MarkupBuilder()
                    .Add("Gets the lines of a 3D model. Returns an array of vectors where the number of lines is ")
                    .Code("lines / 2").Add(". Lines are located with ").Code("lines[i * 2]").Add(" -> ").Code("lines[i * 2 + 1]").Add("."),
                Parameters = new[] {
                    new ModelFileParameter("model", "File path of the model to use. Must be a `.obj` file.", types)
                },
                ReturnType = new ArrayType(types, types.Vector()),
                Action = (actionSet, methodCall) =>
                {
                    ObjModel objModel = (ObjModel)methodCall.AdditionalParameterData[0];

                    var result = new List<Element>();
                    foreach (var line in objModel.GetLines())
                    {
                        result.Add(line.Vertex1.ToVector());
                        result.Add(line.Vertex2.ToVector());
                    }

                    return Element.CreateArray(result.ToArray());
                }
            };
        }

        public static FuncMethod GetPoints(DeltinScript deltinScript)
        {
            ITypeSupplier types = deltinScript.Types;

            return new FuncMethodBuilder()
            {
                Name = "GetPoints",
                Documentation = "Gets the points of a 3D model.",
                Parameters = new[] {
                    new ModelFileParameter("model", "File path of the model. Must be a `.obj` file.", types)
                },
                ReturnType = new ArrayType(types, types.Vector()),
                Action = (actionSet, methodCall) =>
                {
                    ObjModel objModel = (ObjModel)methodCall.AdditionalParameterData[0];
                    return Element.CreateArray(objModel.Vertices.Select(v => v.ToVector()).ToArray());
                }
            };
        }

        class ModelFileParameter : FileParameter
        {
            public ModelFileParameter(string parameterName, string description, ITypeSupplier types) : base(parameterName, description, types, ".obj") { }

            public override object Validate(ParseInfo parseInfo, IExpression value, DocRange valueRange, object additionalData)
            {
                string filepath = base.Validate(parseInfo, value, valueRange, additionalData) as string;
                if (filepath == null) return null;
                return GetFile<ModelLoader>(parseInfo, filepath, uri => new ModelLoader(uri)).Model;
            }
        }

        class ModelLoader : LoadedFile
        {
            public ObjModel Model { get; private set; }

            public ModelLoader(Uri uri) : base(uri)
            {
            }

            protected override void Update()
            {
                Model = ObjModel.Import(GetContent());
            }
        }
    }
}