using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using Deltin.Deltinteger.Parse;

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
                HttpListenerContext context = server.GetContext();
                HttpListenerRequest request = context.Request;

                var inputStream = request.InputStream;
                var encoding = request.ContentEncoding;
                var reader = new StreamReader(inputStream, encoding);

                string document = reader.ReadToEnd();

                Console.WriteLine("Document:");
                Console.WriteLine(document);

                inputStream.Close();
                reader.Close();
            }
        }
    }
}