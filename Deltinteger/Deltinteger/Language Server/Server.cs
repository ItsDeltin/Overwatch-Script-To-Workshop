using System;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;
using Newtonsoft.Json;

namespace Deltin.Deltinteger.LanguageServer
{
    public class Server
    {
        static Log Log = new Log("LangServer");

        const int DefaultPort = 9145;

        ParsingData parserData;

        Dictionary<string, Document> documents = new Dictionary<string, Document>();
        
        public void RequestLoop(int serverPort)
        {
            if (serverPort == 0)
                serverPort = DefaultPort;

            Log.Write(LogLevel.Normal, new ColorMod("Language server", ConsoleColor.Magenta), " started on port ", new ColorMod(serverPort.ToString(), ConsoleColor.DarkCyan));

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
                    
                    case "code":
                        buffer = GetBytes(
                            GetCode(input)
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
            try
            {
                return Uri.UnescapeDataString(new Uri(Uri.UnescapeDataString(uri)).AbsolutePath);
            }
            catch (UriFormatException)
            {
                return null;
            }
        }

        string ParseDocument(string input)
        {
            dynamic json; 
            json = JsonConvert.DeserializeObject(input);
            string uri = uriPath((string)json.uri);
            if (uri == null) return null;
            string content = json.content;

            // Get the document.
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

            // Parse the file.
            parserData = ParsingData.GetParser(document.Uri, content);

            // Update the document's ruleset.
            if (parserData.Rulesets.ContainsKey(uri))
                document.Ruleset = parserData.Rulesets[uri];

            if (parserData.Rules != null && !parserData.Diagnostics.ContainsErrors())
                // Update the document's workshop result.
                document.WorkshopResult = Program.RuleArrayToWorkshop(parserData.Rules.ToArray(), parserData.VarCollection);
            
            PublishDiagnosticsParams[] diagnostics = parserData.Diagnostics.GetDiagnostics();
            return JsonConvert.SerializeObject(diagnostics);
        }

        string GetCode(string input)
        {
            dynamic json; 
            json = JsonConvert.DeserializeObject(input);
            string uri = uriPath((string)json.uri);

            if (uri == null || !documents.ContainsKey(uri))
                return null;

            return documents[uri].WorkshopResult;
        }

        private static void Send(ParsingData data, int clientPort, string uri)
        {
            string final = Program.RuleArrayToWorkshop(data.Rules.ToArray(), data.VarCollection);
            var result = JsonConvert.SerializeObject((code:final, uri:uri));
            try
            {
                using (var wc = new WebClient())
                {
                    wc.Encoding = System.Text.Encoding.UTF8;
                    wc.UploadString($"http://localhost:{clientPort}/", result);
                }
            }
            catch (WebException ex)
            {
                Log.Write(LogLevel.Normal, "Failed to upload workshop result: " + ex.Message);
            }
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
                            new CompletionItem("rule")   { kind = CompletionItem.Keyword, textEdit = TextEdit.Insert(posData.Caret, Extras.Lines(
                                "rule: \"My Rule\"",
                                "Event.OngoingGlobal",
                                "{",
                                "}"
                            )) },
                            new CompletionItem("define") { kind = CompletionItem.Keyword },
                            new CompletionItem("method") { kind = CompletionItem.Keyword, textEdit = TextEdit.Insert(posData.Caret, Extras.Lines(
                                "method myMethod()",
                                "{",
                                "}"
                            )) },
                            new CompletionItem("class") { kind = CompletionItem.Keyword },
                            new CompletionItem("struct") { kind = CompletionItem.Keyword }
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
                        completion.AddRange(Element.GetCompletion(true, true));
                        completion.AddRange(CustomMethodData.GetCompletion());

                        break;

                    // Values
                    case MethodNode methodNode:

                        if (parserData.Success)
                        {
                            completion.AddRange(methodNode.Method.Parameters.Select(param => new CompletionItem(param.Name + ":") { kind = CompletionItem.Keyword }) );

                            // Get all variables
                            if (methodNode.RelatedScopeGroup != null)
                                completion.AddRange(methodNode.RelatedScopeGroup?.GetCompletionItems(posData.Caret));
                            // Get custom methods
                            if (parserData.UserMethods != null)
                                completion.AddRange(UserMethod.CollectionCompletion(parserData.UserMethods.ToArray()));
                        }
                        completion.AddRange(Element.GetCompletion(true, false));
                        completion.AddRange(EnumData.GetAllEnumCompletion());
                        completion.AddRange(CustomMethodData.GetCompletion());

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
                    
                    case IImportNode importNode:

                        string currentPath = importNode.File;

                        string path = Extras.CombinePathWithDotNotation(posData.File, importNode.File);

                        if (path != null)
                        {
                            completion.Add(new CompletionItem("../") { kind = CompletionItem.Folder });

                            // GetDirectoryName can return null even if path isn't null.
                            path = Path.GetDirectoryName(path);

                            if (path != null)
                            {
                                foreach(string fullDirectoryPath in Directory.GetDirectories(path))
                                {
                                    string directory = new DirectoryInfo(fullDirectoryPath).Name;
                                    completion.Add(new CompletionItem(directory) { kind = CompletionItem.Folder });
                                }
                                foreach(string fullFilePath in Directory.GetFiles(path))
                                {
                                    string file = Path.GetFileName(fullFilePath);
                                    completion.Add(new CompletionItem(file) { kind = CompletionItem.File });
                                }
                            }
                        }

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
                        parameterIndex = Array.IndexOf(methodNode.GetParameters(), posData.SelectedNode[0]);
                    }
                    else
                    {
                        if (methodNode.IsNameSelected(posData.Caret))
                            // If the name is selected, -1 will not highlight any parameters.
                            parameterIndex = -1;
                        else
                            // The last parameter is selected.
                            parameterIndex = methodNode.GetParameters().Length;
                    }
                }
                else if (posData.SelectedNode.ElementAtOrDefault(1) is MethodNode)
                {
                    methodNode = (MethodNode)posData.SelectedNode[1];
                    // Get the index of the selected node.
                    parameterIndex = Array.IndexOf(methodNode.GetParameters(), posData.SelectedNode[0]);
                }
                else
                    parameterIndex = 0;

                SignatureInformation information = null;
                if (methodNode != null)
                {
                    if (!methodNode.UsingNormalParameters()) parameterIndex = -1;

                    IMethod method = parserData.GetMethod(methodNode.Name);

                    if (method != null)
                    {
                        ParameterInformation[] parameterInfo = new ParameterInformation[method.Parameters.Length];
                        for (int i = 0; i < parameterInfo.Length; i++)
                            parameterInfo[i] = new ParameterInformation(
                                method.Parameters[i].GetLabel(false),
                                // Every value in the tree can potentially be null.
                                method.Wiki?.Parameters?.ElementAtOrDefault(i)?.Description
                            );

                        information = new SignatureInformation(
                            method.GetLabel(false),
                            // Get the method's documentation
                            method.Wiki?.Description,
                            // Get the parameter data
                            parameterInfo
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

            if (posData.SelectedNode != null && posData.SelectedNode.Length > 0)
            {
                if (posData.SelectedNode[0] is MethodNode)
                {
                    MethodNode methodNode = (MethodNode)posData.SelectedNode[0];
                    IMethod method = parserData.GetMethod(methodNode.Name);

                    if (method != null)
                        hover = new Hover(new MarkupContent(MarkupContent.Markdown, method.GetLabel(true)))
                        {
                            range = methodNode.Location.range
                        };
                }
                else if (posData.SelectedNode[0] is IImportNode)
                {
                    IImportNode importNode = (IImportNode)posData.SelectedNode[0];
                    string path = Extras.CombinePathWithDotNotation(posData.File, importNode.File);
                    if (path != null)
                        hover = new Hover(new MarkupContent(MarkupContent.Markdown, path)) { range = importNode.Location.range };
                }
            }

            return JsonConvert.SerializeObject(hover);
        }

        string GetDefinition(string json)
        {
            PosData posData = GetPosData(json);
            if (posData == null)
                return null;

            List<LocationLink> locations = new List<LocationLink>();
            
            if (posData.SelectedNode != null && posData.SelectedNode.Length > 0)
            {
                if (posData.SelectedNode[0] is IImportNode)
                {
                    IImportNode importNode = (IImportNode)posData.SelectedNode[0];
                    string path = Extras.CombinePathWithDotNotation(posData.File, importNode.File);
                    if (path != null)
                        locations.Add(new LocationLink(importNode.Location.range, new Uri(path).AbsoluteUri, DocRange.Zero, DocRange.Zero));
                }
            }
            return JsonConvert.SerializeObject(locations.ToArray());
        }

        PosData GetPosData(string json)
        {
            dynamic inputJson = JsonConvert.DeserializeObject(json);

            string uri = uriPath((string)inputJson.textDocument.uri);

            if (uri == null || !documents.ContainsKey(uri)) return null;

            string content = documents[uri].Content;
            Pos caret = new Pos((int)inputJson.position.line, (int)inputJson.position.character);

            Node[] selectedNode;
            if (!documents.ContainsKey(uri) || documents[uri].Ruleset == null)
                selectedNode = null;
            else
                selectedNode = documents[uri].Ruleset.SelectedNode(caret);

            return new PosData(uri, caret, selectedNode);
        }
    }

    class Document
    {
        public string Uri { get; }
        public string Content { get; set; }
        public RulesetNode Ruleset { get; set; }
        public string WorkshopResult { get; set; }

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