using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
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
                string document = reader.ReadToEnd();

                inputStream.Close();
                reader.Close();

                byte[] buffer;
                switch (url)
                {
                    case "parse":
                        string json = ParseDocument(document);
                        buffer = GetBytes(json);
                        break;

                    case "color":
                        json = GetColors(document);
                        buffer = GetBytes(json);
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

        static string GetColors(string document)
        {
            DeltinScriptParser parser = Parse.Parser.GetParser(document);
            var ruleSet = parser.ruleset();

            var colors = new List<DocumentColor>();

            GetColors(ruleSet, colors);

            return JsonConvert.SerializeObject(colors.ToArray());
        }

        static void GetColors(IParseTree tree, List<DocumentColor> colors)
        {
            if (tree is ParserRuleContext)
            {
                var context = tree as ParserRuleContext;
                if (tree is DeltinScriptParser.Ow_ruleContext)
                {
                    var symbol = (tree as DeltinScriptParser.Ow_ruleContext).RULE_WORD().Symbol;
                    colors.Add(new DocumentColor(50, 229, 113, 255, symbol.StartIndex, symbol.StopIndex));
                }
            }

            for (int i = 0; i < tree.ChildCount; i++)
                GetColors(tree.GetChild(i), colors);
        }

        static string Autocomplete(string document, int line, int column)
        {
            DeltinScriptParser parser = Parse.Parser.GetParser(document);
            return null;
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

        class DocumentColor
        {
            public int r;
            public int g;
            public int b;
            public int a;
            public int start;
            public int end;

            public DocumentColor(int r, int g, int b, int a, int start, int end)
            {
                this.r = r;
                this.g = g;
                this.b = b;
                this.a = a;
                this.start = start;
                this.end = end;
            }
        }
    }
}