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

namespace Deltin.Deltinteger.LanguageServer
{
    public class Server
    {
        static Log Log = new Log("LangServer");

        const int DefaultPort = 9145;
        const int DefaultClientPort = 9146;

        ParserData parserData;

        Dictionary<string, Document> documents = new Dictionary<string, Document>();
        
        public void RequestLoop(int serverPort, int clientPort)
        {
            if (serverPort == 0)
                serverPort = DefaultPort;
            if (clientPort == 0)
                clientPort = DefaultClientPort;

            Log.Write(LogLevel.Normal, new ColorMod("Language server", ConsoleColor.Magenta), " started on port ", new ColorMod(serverPort.ToString(), ConsoleColor.DarkCyan), 
                " (", new ColorMod(clientPort.ToString(), ConsoleColor.DarkCyan), ")");

            HttpListener server = new HttpListener();
            server.Prefixes.Add($"http://localhost:{serverPort}/");
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
                    case "ping":
                        buffer = GetBytes("OK");
                        break;
                    
                    case "parse":
                        buffer = GetBytes(
                            ParseDocument(input, clientPort)
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

                    case "hover":
                        buffer = GetBytes(
                            GetHover(input)
                        );
                        break;

                    default: 
                        Console.WriteLine("Unsure of how to handle url " + url);
                        buffer = new byte[0];
                        break;
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
            if (value == null) return new byte[0];
            return Encoding.UTF8.GetBytes(value);
        }

        string ParseDocument(string input, int clientPort)
        {
            dynamic json = JsonConvert.DeserializeObject(input);
            string uri = json.uri;
            string content = json.content;

            Document document;

            if (documents.ContainsKey(uri))
            {
                document = documents[uri];
            }
            else
            {
                document = new Document(uri);
                documents.Add(uri, document);
            }
            document.Content = content;

            parserData = ParserData.GetParser(content);

            if (parserData.Rules != null && parserData.Diagnostics.Count == 0)
            {
                string final = Program.RuleArrayToWorkshop(parserData.Rules.ToArray(), parserData.VarCollection);
                using (var wc = new WebClient())
                {
                    wc.Encoding = System.Text.Encoding.UTF8;
                    wc.UploadString($"http://localhost:{clientPort}/", final);
                }
            }
            
            return JsonConvert.SerializeObject(parserData.Diagnostics.ToArray());
        }

        string GetAutocomplete(string json)
        {
            /*
                Format:
                    textDocument
                    caret
                        caret.line
                        caret.character
            */
            PosData posData = GetPosData(json);
            if (posData == null) return null;

            List<CompletionItem> completion = new List<CompletionItem>();
            switch(posData.SelectedNode.FirstOrDefault())
            {
                // Ruleset
                case RulesetNode rulesetNode:

                    completion.AddRange(new CompletionItem[]
                    {
                        // TODO insert text
                        new CompletionItem("rule")   { kind = CompletionItem.Keyword },
                        new CompletionItem("define") { kind = CompletionItem.Keyword },
                        new CompletionItem("method") { kind = CompletionItem.Keyword }
                    });
                    break;

                // Rule node
                case RuleNode ruleNode:

                    // Event type
                    if (ruleNode.IsEventOptionSelected(posData.Caret))
                        completion.AddRange(EnumData.GetEnum<RuleEvent>().GetCompletion());
                    
                    // Player
                    else if (ruleNode.IsPlayerOptionSelected(posData.Caret))
                        completion.AddRange(EnumData.GetEnum<PlayerSelector>().GetCompletion());
                    
                    // Team
                    else if (ruleNode.IsTeamOptionSelected(posData.Caret))
                        completion.AddRange(EnumData.GetEnum<Team>().GetCompletion());
                    
                    else if (ruleNode.IsIfSelected(posData.Caret))
                        completion.AddRange(Element.GetCompletion(true, false));

                    else
                        completion.AddRange(new CompletionItem[] 
                        {
                            new CompletionItem("Event" ) { kind = CompletionItem.Enum },
                            new CompletionItem("Team"  ) { kind = CompletionItem.Enum },
                            new CompletionItem("Player") { kind = CompletionItem.Enum },
                        });
                    break;

                // Actions
                case BlockNode blockNode:

                    // Get all action methods
                    completion.AddRange(Element.GetCompletion(true, true));
                    completion.AddRange(CustomMethodData.GetCompletion());
                    
                    if (parserData.Success)
                    {
                        // Get all variables
                        if (blockNode.RelatedScopeGroup != null)
                            completion.AddRange(blockNode.RelatedScopeGroup.GetCompletionItems(posData.Caret));
                        // Get custom methods
                        if (parserData.UserMethods != null)
                            completion.AddRange(UserMethod.CollectionCompletion(parserData.UserMethods.ToArray()));
                    }

                    break;

                // Values
                case MethodNode methodNode:

                    completion.AddRange(Element.GetCompletion(true, false));
                    completion.AddRange(EnumData.GetAllEnumCompletion());
                    completion.AddRange(CustomMethodData.GetCompletion());

                    if (parserData.Success)
                    {
                        // Get all variables
                        if (methodNode.RelatedScopeGroup != null)
                            completion.AddRange(methodNode.RelatedScopeGroup?.GetCompletionItems(posData.Caret));
                        // Get custom methods
                        if (parserData.UserMethods != null)
                            completion.AddRange(UserMethod.CollectionCompletion(parserData.UserMethods.ToArray()));
                    }

                    break;

                // If the selected node is a string node, show all strings.
                case StringNode stringNode:

                    completion.AddRange(Constants.Strings.Select(str =>
                        new CompletionItem(str)
                        {
                            kind = CompletionItem.Text
                        }
                    ));

                    break;
                
                case EnumNode enumNode:
                    var add = EnumData.GetEnum(enumNode.Type)?.GetCompletion();

                    if (add != null)
                        completion.AddRange(add);
                    
                    break;
            }

            return JsonConvert.SerializeObject(completion.ToArray());
        }

        // TODO comment this
        string GetSignatures(string json)
        {
            PosData posData = GetPosData(json);
            if (posData == null) return null;

            MethodNode methodNode = null;

            SignatureHelp signatures = null;
            
            int methodIndex = 0;
            int parameterIndex;
            if (posData.SelectedNode != null)
            {
                // Get the signature for the method the cursor is on.
                // Check if the selected node is a method node.
                if (posData.SelectedNode.ElementAtOrDefault(0) is MethodNode)
                {
                    methodNode = (MethodNode)posData.SelectedNode[0];

                    // If the parameters of the method node is not selected and the parent is a method node,
                    // select the parent method node.
                    if (!methodNode.IsParametersSelected(posData.Caret) && posData.SelectedNode.ElementAtOrDefault(1) is MethodNode)
                    {
                        methodNode = (MethodNode)posData.SelectedNode[1];
                        // Get the index of the selected node.
                        parameterIndex = Array.IndexOf(methodNode.Parameters, posData.SelectedNode[0]);
                    }
                    else
                    {
                        if (methodNode.IsNameSelected(posData.Caret))
                            // If the name is selected, -1 will not highlight any parameters.
                            parameterIndex = -1;
                        else
                            // The last parameter is selected.
                            parameterIndex = methodNode.Parameters.Length;
                    }
                }
                else if (/*parser.Bav.SelectedNode.ElementAtOrDefault(0) is IExpressionNode
                    &&*/ posData.SelectedNode.ElementAtOrDefault(1) is MethodNode)
                {
                    methodNode = (MethodNode)posData.SelectedNode[1];
                    // Get the index of the selected node.
                    parameterIndex = Array.IndexOf(methodNode.Parameters, posData.SelectedNode[0]);
                }
                else
                    parameterIndex = 0;

                SignatureInformation information = null;
                if (methodNode != null)
                {
                    IMethod method = parserData.GetMethod(methodNode.Name);

                    if (method != null)
                    {
                        information = new SignatureInformation(
                            method.GetLabel(false),
                            // Get the method's documentation
                            method.Wiki?.Description,
                            // Get the parameter data
                            method.Wiki?.Parameters?.Select(v => v.ToParameterInformation())
                                .ToArray()
                        );
                    }
                }

                signatures = new SignatureHelp
                (
                    new SignatureInformation[] { information },
                    methodIndex,
                    parameterIndex
                );
            }

            return JsonConvert.SerializeObject(signatures);
        }

        // https://microsoft.github.io/language-server-protocol/specification#textDocument_hover
        string GetHover(string json)
        {
            PosData posData = GetPosData(json);
            if (posData == null) return null;

            Hover hover = null;

            if (parserData.Success && posData.SelectedNode.Length > 0)
                switch (posData.SelectedNode[0])
                {
                    case MethodNode methodNode:

                        IMethod method = parserData.GetMethod(methodNode.Name);

                        if (method != null)
                            hover = new Hover(new MarkupContent(MarkupContent.Markdown, method.GetLabel(true)))
                            {
                                range = methodNode.Range
                            };
                        break;
                    
                    default:
                        hover = null;
                        break;
                }

            return JsonConvert.SerializeObject(hover);
        }

        PosData GetPosData(string json)
        {
            dynamic inputJson = JsonConvert.DeserializeObject(json);

            string uri = inputJson.textDocument.uri;

            if (!documents.ContainsKey(uri)) return null;

            string content = documents[uri].Content;
            Pos caret = new Pos((int)inputJson.position.line, (int)inputJson.position.character);
            var selectedNode = parserData.RuleSetNode?.SelectedNode(caret);

            return new PosData(caret, selectedNode);
        }
    }

    class Document
    {
        public string Uri { get; }
        public string Content { get; set; }

        public Document(string uri)
        {
            Uri = uri;
        }
    }

    class PosData
    {
        public PosData(Pos caret, Node[] selectedNode)
        {
            Caret = caret;
            SelectedNode = selectedNode;
        }

        public Pos Caret { get; }
        public Node[] SelectedNode { get; }
    }

    public class CompletionItem
    {
        #region Kinds
        public const int Text = 1;
        public const int Method = 2;
        public const int Function = 3;
        public const int Constructor = 4;
        public const int Field = 5;
        public const int Variable = 6;
        public const int Class = 7;
        public const int Interface = 8;
        public const int Module = 9;
        public const int Property = 10;
        public const int Unit = 11;
        public const int Value = 12;
        public const int Enum = 13;
        public const int Keyword = 14;
        public const int Snippet = 15;
        public const int Color = 16;
        public const int File = 17;
        public const int Reference = 18;
        public const int Folder = 19;
        public const int EnumMember = 20;
        public const int Constant = 21;
        public const int Struct = 22;
        public const int Event = 23;
        public const int Operator = 24;
        public const int TypeParameter = 25;
        #endregion

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

    public class ParameterInformation
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

#region Diagnostic
    public class Diagnostic
    {
        public const int Error = 1;
        public const int Warning = 2;
        public const int Information = 3;
        public const int Hint = 4;
        
        public string message;
        public Range range;
        public int severity;
        public object code; // string or number
        public string source;
        public DiagnosticRelatedInformation[] relatedInformation;

        public Diagnostic(string message, Range range)
        {
            this.message = message;
            this.range = range;
        }

        override public string ToString()
        {
            return $"Syntax error at {range.start.ToString()}: " + message;
        }
    }

    public class DiagnosticRelatedInformation
    {
        public Location location;
        public string message;

        public DiagnosticRelatedInformation(Location location, string message)
        {
            this.location = location;
            this.message = message;
        }
    }
#endregion

#region Hover
    // https://microsoft.github.io/language-server-protocol/specification#textDocument_hover
    class Hover
    {
        public object contents; // TODO MarkedString support 
        public Range range;

        public Hover(MarkupContent contents)
        {
            this.contents = contents;
        }
        #pragma warning disable 0618
        public Hover(MarkedString contents)
        {
            this.contents = contents;
        }
        public Hover(MarkedString[] contents)
        {
            this.contents = contents;
        }
        #pragma warning restore 0618
    }
#endregion

    public class MarkupContent
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

    [Obsolete("MarkedString is obsolete, use MarkupContent instead.")]
    public class MarkedString
    {
        public string language;
        public string value;

        public MarkedString(string language, string value)
        {
            this.language = language;
            this.value = value;
        }
    }

    public class Location 
    {
        public string uri;
        public Range range;

        public Location(string uri, Range range)
        {
            this.uri = uri;
            this.range = range;
        }
    }

    public class TextEdit
    {
        public static TextEdit Replace(Range range, string newText)
        {
            return new TextEdit()
            {
                range = range,
                newText = newText
            };
        }
        public static TextEdit Insert(Pos pos, string newText)
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

    public class Command
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