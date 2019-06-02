using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;
using Newtonsoft.Json;

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
                if (url == "parse")
                {
                    // Parse input
                    string json = ParseDocument(document);
                    buffer = GetBytes(json);
                }
                else throw new Exception("Unsure of how to handle url " + url);

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
            Parser.ParseText(document, out SyntaxError[] errors);

            return JsonConvert.SerializeObject(errors);
        }
    }
}