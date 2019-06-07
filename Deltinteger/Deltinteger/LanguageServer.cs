using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;
using Newtonsoft.Json;
using Antlr4;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace Deltin.Deltinteger.Checker
{
    public class Check
    {
        const int PORT = 3000;

        static Log Log = new Log("LangServer");
        
        public static void RequestLoop()
        {
            Log.Write(LogLevel.Normal, $"Language server started on port {PORT}.");

            HttpListener server = new HttpListener();
            server.Prefixes.Add($"http://localhost:{PORT}/");
            server.Start();

            bool isRunning = true;
            while (isRunning)
            {
                // Get request
                HttpListenerContext context = server.GetContext();
                HttpListenerRequest request = context.Request;

                var inputStream = request.InputStream;
                var encoding = request.ContentEncoding;
                var reader = new StreamReader(inputStream, encoding);

                string url = request.RawUrl.Substring(1);
                string input = reader.ReadToEnd();

                inputStream.Close();
                reader.Close();

                byte[] buffer;
                switch (url)
                {
                    case "parse":
                        buffer = GetBytes(
                            ParseDocument(input)
                        );
                        break;

                    case "completion":
                        buffer = GetBytes(
                            GetAutocomplete(input)
                        );
                        break;

                    case "signature":
                        buffer = GetBytes(
                            GetSignatures(input)
                        );
                        break;

                    default: throw new Exception("Unsure of how to handle url " + url);
                }

                HttpListenerResponse response = context.Response;
                response.ContentLength64 = buffer.LongLength;
                Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
            }
        }

        static byte[] GetBytes(string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }

        static string ParseDocument(string document)
        {
            Parse.Parser.ParseText(document, out var data);

            return JsonConvert.SerializeObject(data.ErrorListener.Errors);
        }

        static string GetAutocomplete(string json)
        {
            /*
                Format:
                    textDocument
                    caret
                        caret.line
                        caret.character
            */
            dynamic inputJson = JsonConvert.DeserializeObject(json);

            string document = inputJson.textDocument;

            int line      = inputJson.caret.line + 1;
            int character = inputJson.caret.character;
            DocumentPos caret = new DocumentPos(line, character);

            var parser = ParserElements.GetParser(document, new Pos(caret.line, caret.character));

            CompletionItem[] completion;            
            switch(parser.Bav.SelectedNode.GetType().Name)
            {
                // Ruleset
                case nameof(RulesetNode):

                    completion = new CompletionItem[]
                    {
                        // TODO insert text
                        new CompletionItem("rule"),
                        new CompletionItem("define"),
                        new CompletionItem("method")
                    };
                    break;

                // Actions
                case nameof(BlockNode):

                    completion = Element.ActionList.Select(m => 
                            new CompletionItem(m.Name.Substring(2))
                            {
                                kind = Method,
                                detail = ((Element)Activator.CreateInstance(m)).ToString(),
                            }
                        ).ToArray();

                    break;

                // Values
                case nameof(MethodNode):
                case nameof(UserMethodNode):
                case nameof(VariableNode):

                    completion = Element.ValueList.Select(m => 
                        new CompletionItem(m.Name.Substring(2))
                        {
                            kind = Method,
                            detail = ((Element)Activator.CreateInstance(m)).ToString(),
                        }
                        ).ToArray();

                    break;

                // Any rules that do not have any autocomplete.
                case nameof(DeltinScriptParser.StatementContext):
                case nameof(DeltinScriptParser.VarsetContext):
                // -- todos:
                // --
                    completion = new CompletionItem[0];
                    break;

                default: 
                    Console.WriteLine(parser.Bav.SelectedNode.GetType().Name + " context not implemented.");
                    completion = new CompletionItem[0];
                    break;
            }

            /*
            string filter = selectedRule.GetText();
            foreach(CompletionItem comp in completion)
            {
                comp.filterText = filter;
            }
            */

            return JsonConvert.SerializeObject(completion);
        }

        // TODO comment this
        static string GetSignatures(string json)
        {
            dynamic inputJson = JsonConvert.DeserializeObject(json);

            string document = inputJson.textDocument;

            int line      = inputJson.caret.line + 1;
            int character = inputJson.caret.character;
            DocumentPos caret = new DocumentPos(line, character);

            var parser = ParserElements.GetParser(document, new Pos(caret.line, caret.character));

            int methodIndex = 0;
            int parameterIndex = 0;

            Type methodType = null;
            SignatureInformation information = null;
            
            bool foundMethodContext = false;

            if (parser.Bav.SelectedNode is IExpressionNode)
            {
                methodContext = (DeltinScriptParser.MethodContext)selectedRule.Parent;
                parameterIndex = ((DeltinScriptParser.ParametersContext)selectedRule).expr().Length;
                foundMethodContext = true;
            }
            else if (selectedRule.Parent is DeltinScriptParser.ParametersContext)
            {
                methodContext = (DeltinScriptParser.MethodContext)selectedRule.Parent.Parent;
                parameterIndex = Array.IndexOf(((DeltinScriptParser.ParametersContext)selectedRule.Parent).expr(), selectedRule);
                foundMethodContext = true;
            }
            else if (selectedRule is DeltinScriptParser.MethodContext)
            {
                methodContext = (DeltinScriptParser.MethodContext)selectedRule;
                parameterIndex = 0;
                foundMethodContext = true;
            }

            if (foundMethodContext)
            {
                string name = methodContext.PART().GetText();
                methodType = Element.GetMethod(name);

                if (methodType != null)
                {
                    Element element = (Element)Activator.CreateInstance(methodType);

                    information = new SignatureInformation(
                        element.ToString(),
                        "",
                        element.ParameterData.Select(p => 
                            new ParameterInformation(p.Name, "")
                        ).ToArray());
                }
            }

            SignatureHelp signatures = new SignatureHelp
            (
                new SignatureInformation[] { information },
                methodIndex,
                parameterIndex
            );

            return JsonConvert.SerializeObject(signatures);
        }

#region Kinds
        private const int Text = 1;
        private const int Method = 2;
        private const int Function = 3;
        private const int Constructor = 4;
        private const int Field = 5;
        private const int Variable = 6;
        private const int Class = 7;
        private const int Interface = 8;
        private const int Module = 9;
        private const int Property = 10;
        private const int Unit = 11;
        private const int Value = 12;
        private const int Enum = 13;
        private const int Keyword = 14;
        private const int Snippet = 15;
        private const int Color = 16;
        private const int File = 17;
        private const int Reference = 18;
        private const int Folder = 19;
        private const int EnumMember = 20;
        private const int Constant = 21;
        private const int Struct = 22;
        private const int Event = 23;
        private const int Operator = 24;
        private const int TypeParameter = 25;
#endregion

        class DocumentPos
        {
            public int line;
            public int character;

            public DocumentPos(int line, int character)
            {
                this.line = line;
                this.character = character;
            }
        }

        class CompletionItem
        {
            public CompletionItem(string label)
            {
                this.label = label;
            }

            public string label;
            public int kind;
            public string detail;
            public object documentation;
            public bool deprecated;
            public string sortText;
            public string filterText;
            public int insertTextFormat;
            public TextEdit textEdit;
            public TextEdit[] additionalTextEdits;
            public string[] commitCharacters;
            public Command command;
            public object data;
        }

#region Signature
        class SignatureHelp
        {
            public SignatureInformation[] signatures;
            public int activeSignature;
            public int activeParameter;

            public SignatureHelp(SignatureInformation[] signatures, int activeSignature, int activeParameter)
            {
                this.signatures = signatures;
                this.activeSignature = activeSignature;
                this.activeParameter = activeParameter;
            }
        }

        class SignatureInformation
        {
            public string label;
            public object documentation; // string or markup
            public ParameterInformation[] parameters;

            public SignatureInformation(string label, object documentation, ParameterInformation[] parameters)
            {
                this.label = label;
                this.documentation = documentation;
                this.parameters = parameters;
            }
        }

        class ParameterInformation
        {
            public object label; // string or int[]

            public object documentation; // string or markup

            public ParameterInformation(object label, object documentation)
            {
                this.label = label;
                this.documentation = documentation;
            }
        }
        #endregion

        class MarkupContent
        {
            public string kind;
            public string value;

            public const string PlainText = "plaintext";
            public const string Markdown = "markdown";

            public MarkupContent(string kind, string value)
            {
                this.kind = kind;
                this.value = value;
            }
        }

        class Range
        {
            public DocumentPos start;
            public DocumentPos end;

            public Range(DocumentPos start, DocumentPos end)
            {
                this.start = start;
                this.end = end;
            }
        }

        class TextEdit
        {
            public static TextEdit Replace(Range range, string newText)
            {
                return new TextEdit()
                {
                    range = range,
                    newText = newText
                };
            }
            public static TextEdit Insert(DocumentPos pos, string newText)
            {
                return new TextEdit()
                {
                    range = new Range(pos, pos),
                    newText = newText
                };
            }
            public static TextEdit Delete(Range range)
            {
                return new TextEdit()
                {
                    range = range,
                    newText = string.Empty
                };
            }

            public Range range;
            public string newText;
        }

        class Command
        {
            public string title;
            public string command;
            public object[] arguments;

            public Command(string title, string command)
            {
                this.title = title;
                this.command = command;
            }
        }
    }
}