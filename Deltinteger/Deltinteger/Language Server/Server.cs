using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using Deltin.Deltinteger.Parse;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OmniSharp.Extensions.LanguageServer.Server;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

using ProtocolRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using ILanguageServer = OmniSharp.Extensions.LanguageServer.Server.ILanguageServer;

using MediatR;
using Serilog;

namespace Deltin.Deltinteger.LanguageServer
{
    public class DeltintegerLanguageServer
    {
        public const string SendWorkshopCode = "workshopCode";
        public const string SendElementCount = "elementCount";
        public const string Version = "version";

        public static void Run()
        {
            new DeltintegerLanguageServer().RunServer().Wait();
        }

        private static string LogFile() => Path.Combine(Program.ExeFolder, "Log", "log.txt");

        public ILanguageServer Server { get; private set; }

        private DeltinScript _lastParse = null;
        private object _lastParseLock = new object();
        public DeltinScript LastParse {
            get {
                lock (_lastParseLock) return _lastParse;
            }
            set {
                lock (_lastParseLock) _lastParse = value;
            }
        }

        public DocumentHandler DocumentHandler { get; private set; }
        public FileGetter FileGetter { get; private set; }
        public ConfigurationHandler ConfigurationHandler { get; private set; }

        async Task RunServer()
        {
            Serilog.Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.File(LogFile(), rollingInterval: RollingInterval.Day, flushToDiskInterval:new TimeSpan(0, 0, 10))
                .CreateLogger();
            
            Serilog.Log.Information("Deltinteger Language Server");

            DocumentHandler = new DocumentHandler(this);
            FileGetter = new FileGetter(DocumentHandler);
            CompletionHandler completionHandler = new CompletionHandler(this);
            SignatureHandler signatureHandler = new SignatureHandler(this);
            ConfigurationHandler = new ConfigurationHandler(this);
            DefinitionHandler definitionHandler = new DefinitionHandler(this);
            HoverHandler hoverHandler = new HoverHandler(this);
            ReferenceHandler referenceHandler = new ReferenceHandler(this);
            CodeLensHandler codeLensHandler = new CodeLensHandler(this);
            DoRenameHandler renameHandler = new DoRenameHandler(this);
            PrepareRenameHandler prepareRenameHandler = new PrepareRenameHandler(this);

            Server = await OmniSharp.Extensions.LanguageServer.Server.LanguageServer.From(options => options
                .WithInput(Console.OpenStandardInput())
                .WithOutput(Console.OpenStandardOutput())
                .ConfigureLogging(x => x
                    .AddSerilog()
                    .AddLanguageServer()
                    .SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug))
                .WithHandler<DocumentHandler>(DocumentHandler)
                .WithHandler<CompletionHandler>(completionHandler)
                .WithHandler<SignatureHandler>(signatureHandler)
                .WithHandler<ConfigurationHandler>(ConfigurationHandler)
                .WithHandler<DefinitionHandler>(definitionHandler)
                .WithHandler<HoverHandler>(hoverHandler)
                .WithHandler<ReferenceHandler>(referenceHandler)
                .WithHandler<CodeLensHandler>(codeLensHandler)
                .WithHandler<DoRenameHandler>(renameHandler)
                .WithHandler<PrepareRenameHandler>(prepareRenameHandler));
            
            Server.SendNotification(Version, Program.VERSION);
            
            await Server.WaitForExit;
        }

        public static readonly DocumentSelector DocumentSelector = new DocumentSelector(
            new DocumentFilter() {
                Language = "ostw",
                Pattern = "**/*.del"
            },
            new DocumentFilter() {
                Language = "ostw",
                Pattern = "**/*.ostw"
            },
            new DocumentFilter() {
                Language = "ostw",
                Pattern = "**/*.workshop"
            }
        );
    }
}