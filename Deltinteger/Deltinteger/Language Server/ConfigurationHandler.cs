using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Deltin.Deltinteger.Parse;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using MediatR;
using IDidChangeConfigurationHandler = OmniSharp.Extensions.LanguageServer.Protocol.Server.IDidChangeConfigurationHandler;
using DidChangeConfigurationCapability = OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities.DidChangeConfigurationCapability;

namespace Deltin.Deltinteger.LanguageServer
{
    public class ConfigurationHandler : IDidChangeConfigurationHandler
    {
        private DeltintegerLanguageServer _languageServer { get; }

        public ConfigurationHandler(DeltintegerLanguageServer languageServer)
        {
            _languageServer = languageServer;
        }

        public Task<Unit> Handle(DidChangeConfigurationParams configChangeParams, CancellationToken token)
        {
            var json = configChangeParams.Settings?.SelectToken("ostw") as JObject;

            if (json != null)
            {
                var config = json.ToObject<RawConfiguration>();
            }

            return Unit.Task;
        }

        public object GetRegistrationOptions()
        {
            return null;
        }

        // Client capability
        private DidChangeConfigurationCapability _capability;
        public void SetCapability(DidChangeConfigurationCapability capability)
        {
            _capability = capability;
        }
    
        class RawConfiguration
        {
            public string outputLanguage;
            public string deltintegerPath;
        }
    }
}