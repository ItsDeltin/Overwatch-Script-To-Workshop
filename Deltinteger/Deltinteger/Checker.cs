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
        
        public static void RequestLoop()
        {
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
            Parse.Parser.ParseText(document, out SyntaxError[] errors);

            return JsonConvert.SerializeObject(errors);
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

            int line      = inputJson.caret.line + 1;
            int character = inputJson.caret.character;
            DocumentPos caret = new DocumentPos(line, character);

            string document = inputJson.textDocument;

            DeltinScriptParser parser = Parse.Parser.GetParser(document);
            var ruleSet = parser.ruleset();

            ParserRuleContext selectedRule = GetSelectedRule(ruleSet, caret, parser);

            Completion[] completion;
            
            switch(selectedRule.GetType().Name)
            {
                // TODO seperate expressions and values
                case nameof(DeltinScriptParser.BlockContext):
                case nameof(DeltinScriptParser.Rule_ifContext):
                case nameof(DeltinScriptParser.MethodContext):
                case nameof(DeltinScriptParser.User_methodContext):
                case nameof(DeltinScriptParser.ExprContext):

                    completion = Element.MethodList.Select(m => 
                        new Completion(m.Name.Substring(2))
                        {
                            kind = Method,
                            detail = ((Element)Activator.CreateInstance(m)).ToString()
                        }
                        ).ToArray();

                    break;
                
                case nameof(DeltinScriptParser.ruleset):
                    // TODO ruleset autocomplete
                    completion = new Completion[0];
                    break;

                default: 
                    Console.WriteLine(selectedRule.GetType().Name + " not implemented.");
                    Debugger.Break();
                    throw new NotImplementedException();
            }

            return JsonConvert.SerializeObject(completion);
        }

        static ParserRuleContext GetSelectedRule(ParserRuleContext tree, DocumentPos caret, DeltinScriptParser parser)
        {
            if (tree.ChildCount == 0)
                return tree;

            ParserRuleContext selectedContext = null;
            int selectedRange = 0;
            for (int i = 0; i < tree.ChildCount; i++)
                if (tree.GetChild(i) is ParserRuleContext)
                {
                    var child = tree.GetChild(i) as ParserRuleContext;
                    var compare = GetSelectedRule(child, caret, parser);

                    int compareRange = tree.SourceInterval.b - tree.SourceInterval.a;

                    string name = compare.ToString(parser);

                    if ((compare.start.Line  < caret.Line || (compare.start.Line == caret.Line && compare.start.Column <= caret.Character)) &&
                        (compare.stop.Line   > caret.Line || (compare.stop.Line  == caret.Line && compare.stop.Column >= caret.Character)) &&
                        (compareRange <= selectedRange || selectedRange == 0))
                        {
                            selectedRange = compareRange;
                            selectedContext = compare;
                        }
                }

            return selectedContext ?? tree;
        }

        static string GetSignatures(string json)
        {
            dynamic inputJson = JsonConvert.DeserializeObject(json);

            int line      = inputJson.caret.line + 1;
            int character = inputJson.caret.character;
            DocumentPos caret = new DocumentPos(line, character);

            string document = inputJson.textDocument;

            DeltinScriptParser parser = Parse.Parser.GetParser(document);
            var ruleSet = parser.ruleset();

            ParserRuleContext selectedRule = GetSelectedRule(ruleSet, caret, parser);

            int methodIndex = 0;
            int parameterIndex = 0;

            string parentDebug = selectedRule.ToString(parser);
            Type methodType = null;
            SignatureInformation information = null;

            if (selectedRule is DeltinScriptParser.MethodContext)
            {
                var method = selectedRule as DeltinScriptParser.MethodContext;
                string name = method.PART().GetText();
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

        class DocumentPos
        {
            public int Line;
            public int Character;

            public DocumentPos(int line, int character)
            {
                Line = line;
                Character = character;
            }
        }

        class Completion
        {
            public Completion(string label)
            {
                this.label = label;
            }

            public string label;

            public int kind;

            public string detail;

            public string documentation;
        }

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
    }
}