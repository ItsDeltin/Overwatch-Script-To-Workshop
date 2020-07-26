using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.JsonRpc.Server;
using OmniSharp.Extensions.JsonRpc.Server.Messages;

namespace Deltin.Deltinteger.Debugger
{
    class DebuggerServerOptions : JsonRpcServerOptionsBase<DebuggerServerOptions>, IRequestProcessIdentifier
    {
        public override IRequestProcessIdentifier RequestProcessIdentifier { get => this; set => throw new NotImplementedException(); }

        public RequestProcessType Identify(IHandlerDescriptor descriptor)
        {
            // todo
            return RequestProcessType.Serial;
        }
    }

    #region oldOptions
    /*
    class DebuggerServerOptions : IJsonRpcServerOptions, IRequestProcessIdentifier
    {
        public PipeReader Input { get; set; } = PipeReader.Create(Console.OpenStandardInput());
        public PipeWriter Output { get; set; } = PipeWriter.Create(Console.OpenStandardOutput());
        public IServiceCollection Services { get; set; }
        public IRequestProcessIdentifier RequestProcessIdentifier { get => this; set => throw new NotImplementedException(); }
        public int? Concurrency { get; set; }
        public Action<Exception> OnUnhandledException { get => ex => {
            string msg = ex.Message;
        }; set => throw new NotImplementedException(); }
        public Func<ServerError, string, Exception> CreateResponseException { get => (err, str) => new Exception(err.Error.Message); set => throw new NotImplementedException(); }
        public bool SupportsContentModified { get => false; set => throw new NotImplementedException(); } // TODO
        public TimeSpan MaximumRequestTimeout { get => TimeSpan.FromSeconds(5); set => throw new NotImplementedException(); }
        public IDisposable RegisteredDisposables { get; private set; }
        public IEnumerable<Assembly> Assemblies { get; }

        public RequestProcessType Identify(IHandlerDescriptor descriptor)
        {
            // TODO
            return RequestProcessType.Serial;
        }

        public void RegisterForDisposal(IDisposable disposable)
        {
            RegisteredDisposables = disposable;
        }
    }
    */
    #endregion

    class CanItBeSimple : OmniSharp.Extensions.JsonRpc.JsonRpcServerBase, ISerializer
    {
        public static void Server()
        {
            var server = new CanItBeSimple(new DebuggerServerOptions());
        }

        public JsonSerializer JsonSerializer { get; }
        public JsonSerializerSettings Settings { get; }
        protected override IResponseRouter ResponseRouter { get; }
        protected override IHandlersManager HandlersManager { get; }

        public CanItBeSimple(DebuggerServerOptions serverOptions) : base(serverOptions)
        {
            JsonSerializer = new JsonSerializer();
            Settings = new JsonSerializerSettings();
            ResponseRouter = new ResponseRouter(new OutputHandler(serverOptions.Output, this, null), this);
        }

        public object DeserializeObject(string json, Type type) => JsonSerializer.Deserialize(new StringReader(json), type);
        public T DeserializeObject<T>(string json) => JsonSerializer.Deserialize<T>(new JsonTextReader(new StringReader(json)));
        public string SerializeObject(object value)
        {
            TextWriter writer = new StringWriter();
            JsonSerializer.Serialize(writer, value);
            return writer.ToString();
        }

        public long GetNextId()
        {
            // tododododo
            throw new NotImplementedException();
        }
    }

    // public class DebuggerServerOptions : JsonRpcServerOptionsBase<DebuggerServerOptions>, IDebuggerServerRegistry
    // {

    // }

    // public interface IDebuggerServerRegistry : IJsonRpcHandlerRegistry<IDebuggerServerRegistry>, IJsonRpcHandlerRegistry
    // {
    // }
}