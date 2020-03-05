using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Deltin.Deltinteger.I18n;
using Deltin.Deltinteger.Elements;
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
        public static bool OptimizeOutput = true;
        private DeltintegerLanguageServer _languageServer { get; }
        public bool ReferencesCodeLens { get; private set; }
        public bool ImplementsCodeLens { get; private set; }

        public ConfigurationHandler(DeltintegerLanguageServer languageServer)
        {
            _languageServer = languageServer;
        }

        public Task<Unit> Handle(DidChangeConfigurationParams configChangeParams, CancellationToken token)
        {
            var json = configChangeParams.Settings?.SelectToken("ostw") as JObject;

            if (json != null)
            {
                dynamic config = json;

                ReferencesCodeLens = config.codelens.references;
                ImplementsCodeLens = config.codelens.implements;

                LanguageInfo.LoadLanguage(GetOutputLanguage(config.outputLanguage));
                OptimizeOutput = config.optimizeOutput;
            }

            return Unit.Task;

            OutputLanguage GetOutputLanguage(string languageString)
            {
                switch (languageString)
                {
                    case "English": return OutputLanguage.enUS;
                    case "German": return OutputLanguage.deDE;
                    case "Spanish (Castilian)": return OutputLanguage.esES;
                    case "Spanish (Mexico)": return OutputLanguage.esMX;
                    case "French": return OutputLanguage.frFR;
                    case "Italian": return OutputLanguage.itIT;
                    case "Japanese": return OutputLanguage.jaJP;
                    case "Korean": return OutputLanguage.koKR;
                    case "Polish": return OutputLanguage.plPL;
                    case "Portuguese": return OutputLanguage.ptBR;
                    case "Russian": return OutputLanguage.ruRU;
                    case "Chinese (S)": return OutputLanguage.zhCN;
                    case "Chinese (T)": return OutputLanguage.zhTW;
                    default: return OutputLanguage.enUS;
                }
            }
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
    }
}