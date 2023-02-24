using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using MetaComment = Deltin.Deltinteger.Compiler.SyntaxTree.MetaComment;

namespace Deltin.Deltinteger.Parse
{
    public class ParsedMetaComment
    {
        public static ParsedMetaComment FromMetaComment(MetaComment metaComment)
        {
            if (metaComment == null)
                return null;

            MarkupBuilder description = new MarkupBuilder();
            MarkupBuilder stub = description;
            bool addNewLine = false;

            var parameters = new List<MetaCommentParameter>();

            foreach (var line in metaComment.GetLines())
            {
                // Match parameter indicator.
                var match = Regex.Match(line, @"^\s*-\s*`([a-zA-Z0-9_]+)`\s*:");
                var addLine = line;

                if (match.Success && match.Groups.Count > 0)
                {
                    // The name of the parameter will be in group #1.
                    var parameterName = match.Groups[1].Value;
                    // Remove the parameter name from the line that will be added.
                    addLine = line.Substring(match.Length).Trim();

                    parameters.Add(new MetaCommentParameter(parameterName));
                    stub = parameters.Last().Description;
                }
                else if (addNewLine)
                    stub.NewLine();

                stub.Add(addLine);
                addNewLine = true;
            }

            return new ParsedMetaComment(description, parameters);
        }

        public MarkupBuilder Description { get; }
        public IEnumerable<MetaCommentParameter> Parameters { get; }

        private ParsedMetaComment(MarkupBuilder description, IEnumerable<MetaCommentParameter> parameters) =>
            (Description, Parameters) = (description, parameters);

        public MarkupBuilder GetParameterDescription(string parameterName) => Parameters.FirstOrDefault(p => p.Name == parameterName).Description;

        public struct MetaCommentParameter
        {
            public string Name { get; }
            public MarkupBuilder Description { get; } = new MarkupBuilder();
            public MetaCommentParameter(string name) => Name = name;
        }
    }
}