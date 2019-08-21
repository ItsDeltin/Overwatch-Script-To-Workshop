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
using System.Web;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;
using Newtonsoft.Json;

namespace Deltin.Deltinteger.LanguageServer
{
    public class Server
    {
        static Log Log = new Log("LangServer");

        const int DefaultPort = 9145;
        const int DefaultClientPort = 9146;

        ParsingData parserData;

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
                    
                    case "definition":
                        buffer = GetBytes(
                            GetDefinition(input)
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

        string uriPath(string uri)
        {
            return Uri.UnescapeDataString(new Uri(Uri.UnescapeDataString(uri)).AbsolutePath);
        }

        string ParseDocument(string input, int clientPort)
        {
            dynamic json; 
            json = JsonConvert.DeserializeObject(input);
            string uri = uriPath((string)json.uri);

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

            parserData = ParsingData.GetParser(document.Uri, content);

            if (parserData.Rulesets.ContainsKey(uri))
                document.Ruleset = parserData.Rulesets[uri];

            if (parserData.Rules != null && !parserData.Diagnostics.ContainsErrors())
            {
                string final = Program.RuleArrayToWorkshop(parserData.Rules.ToArray(), parserData.VarCollection);
                try
                {
                    using (var wc = new WebClient())
                    {
                        wc.Encoding = System.Text.Encoding.UTF8;
                        wc.UploadString($"http://localhost:{clientPort}/", final);
                    }
                }
                catch (WebException)
                {
                    Log.Write(LogLevel.Normal, "Failed to upload workshop result.");
                }
            }
            
            PublishDiagnosticsParams[] diagnostics = parserData.Diagnostics.GetDiagnostics();
            return JsonConvert.SerializeObject(diagnostics);
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
            if (posData.SelectedNode != null)
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
                            // Get structs
                            if (parserData.DefinedTypes != null)
                                completion.AddRange(DefinedType.CollectionCompletion(parserData.DefinedTypes.ToArray()));
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

            if (/*parserData.Success &&*/ posData.SelectedNode != null && posData.SelectedNode.Length > 0)
                switch (posData.SelectedNode[0])
                {
                    case MethodNode methodNode:

                        IMethod method = parserData.GetMethod(methodNode.Name);

                        if (method != null)
                            hover = new Hover(new MarkupContent(MarkupContent.Markdown, method.GetLabel(true)))
                            {
                                range = methodNode.Location.range
                            };
                        break;

                    case ImportNode importNode:
                        
                        string path = null;
                        try
                        {
                            path = Extras.CombinePathWithDotNotation(posData.File, importNode.File);
                        }
                        catch (ArgumentException) {}

                        if (path != null)
                            hover = new Hover(new MarkupContent(MarkupContent.Markdown, path)) { range = importNode.Location.range };

                        break;
                    
                    default:
                        hover = null;
                        break;
                }

            return JsonConvert.SerializeObject(hover);
        }

        string GetDefinition(string json)
        {
            PosData posData = GetPosData(json);
            if (posData == null)
                return null;

            /*
            LocationLink location = null;

            if (posData.SelectedNode != null && posData.SelectedNode.Length > 0)
                switch(posData.SelectedNode[0])
                {
                    case ImportNode importNode:

                        string path = null;
                        try
                        {
                            path = Extras.CombinePathWithDotNotation(posData.File, importNode.File);
                        }
                        catch (ArgumentException) {}

                        if (path != null)
                            //location = new Location(new Uri(path).AbsoluteUri, Range.Zero);
                            location = new LocationLink(importNode.Location.range, new Uri(path).AbsoluteUri, Range.Zero, Range.Zero);
                        
                        break;
                }
            
            if (location == null) return null;
            return JsonConvert.SerializeObject(new LocationLink[] { location });
            */

            List<LocationLink> locations = new List<LocationLink>();
            
            if (documents.ContainsKey(posData.File) && documents[posData.File].Ruleset != null)
            {
                foreach (ImportNode node in documents[posData.File].Ruleset.Imports)
                {
                    string path = null;
                    try
                    {
                        path = Extras.CombinePathWithDotNotation(posData.File, node.File);
                    }
                    catch (ArgumentException) {}

                    if (path != null)
                        locations.Add(new LocationLink(node.Location.range, new Uri(path).AbsoluteUri, Range.Zero, Range.Zero));
                }
            }
            return JsonConvert.SerializeObject(locations.ToArray());
        }

        PosData GetPosData(string json)
        {
            dynamic inputJson = JsonConvert.DeserializeObject(json);

            string uri = uriPath((string)inputJson.textDocument.uri);

            if (!documents.ContainsKey(uri)) return null;

            string content = documents[uri].Content;
            Pos caret = new Pos((int)inputJson.position.line, (int)inputJson.position.character);

            Node[] selectedNode;
            if (!parserData.Rulesets.ContainsKey(uri))
                selectedNode = null;
            else
                selectedNode = parserData.Rulesets[uri].SelectedNode(caret);

            return new PosData(uri, caret, selectedNode);
        }
    }

    class Document
    {
        public string Uri { get; }
        public string Content { get; set; }
        public RulesetNode Ruleset { get; set; }

        public Document(string uri)
        {
            Uri = uri;
        }
    }

    class PosData
    {
        public PosData(string file, Pos caret, Node[] selectedNode)
        {
            File = file;
            Caret = caret;
            SelectedNode = selectedNode;
        }

        public string File { get; }
        public Pos Caret { get; }
        public Node[] SelectedNode { get; }
    }
}