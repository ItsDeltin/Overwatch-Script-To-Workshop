using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CustomMethod : Attribute
    {
        public CustomMethod(string elementName, CustomMethodType methodType)
        {
            MethodName = elementName;
            MethodType = methodType;
        }

        public string MethodName { get; private set; }
        public CustomMethodType MethodType { get; private set; }
    }

    public class MethodResult
    {
        public MethodResult(Element[] elements, Element result)
        {
            Elements = elements;
            Result = result;
        }
        public Element[] Elements { get; private set; }
        public Element Result { get; private set; }
    }

    public enum CustomMethodType
    {
        Value,
        MultiAction_Value,
        Action
    }

    public class CustomMethodData : IMethod
    {
        public string Name { get; }
        public ParameterBase[] Parameters { get; }
        public CustomMethodType CustomMethodType { get; }
        public Type Type { get; }
        public WikiMethod Wiki { get; }

        // IScopeable defaults
        public Location DefinedAt { get; } = null;
        public AccessLevel AccessLevel { get; } = AccessLevel.Public;
        public string ScopeableType { get; } = "method";

        public string GetLabel(bool markdown)
        {
            return Name + "(" + Parameter.ParameterGroupToString(Parameters, markdown) + ")"
            + (markdown && Wiki?.Description != null ? "\n\r" + Wiki.Description : "");
        }

        public CustomMethodData(Type type)
        {
            Type = type;

            CustomMethod data = type.GetCustomAttribute<CustomMethod>();
            Name = data.MethodName;
            CustomMethodType = data.MethodType;

            Parameters = type.GetCustomAttributes<ParameterBase>()
                .ToArray();
            
            CustomMethodWiki cmWiki = GetObject().Wiki();
            if (cmWiki != null)
            {
                WikiParameter[] parameters = null;

                if (cmWiki.ParameterDescriptions != null)
                {
                    parameters = new WikiParameter[cmWiki.ParameterDescriptions.Length];
                    for (int i = 0; i < parameters.Length; i++)
                        parameters[i] = new WikiParameter(Parameters[i].Name, cmWiki.ParameterDescriptions[i]);
                }

                string description = cmWiki.Description;
                if (CustomMethodType == CustomMethodType.MultiAction_Value)
                {
                    if (description != null)
                        description += "\nThis method cannot be used in conditions.";
                    else
                        description = "This method cannot be used in conditions.";
                }

                Wiki = new WikiMethod(Name, description, parameters);
            }
        }

        // public Element Parse(TranslateRule context, bool needsToBeValue, ScopeGroup scope, MethodNode methodNode, IWorkshopTree[] parameters)
        // {
        //     TranslateRule.CheckMethodType(needsToBeValue, CustomMethodType, methodNode.Name, methodNode.Location);

        //     var customMethodResult = GetObject(context, scope, parameters, methodNode.Location, methodNode.Parameters.Select(p => p.Location).ToArray())
        //         .Result();

        //     // Some custom methods have extra actions.
        //     if (customMethodResult.Elements != null)
        //         context.Actions.AddRange(customMethodResult.Elements);

        //     return customMethodResult.Result;
        // }

        // public CustomMethodBase GetObject(TranslateRule context, ScopeGroup scope, IWorkshopTree[] parameters, Location methodLocation, Location[] parameterLocations)
        // {
        //     CustomMethodBase customMethod = GetObject();
        //     customMethod.TranslateContext = context;
        //     customMethod.Scope = scope;
        //     customMethod.Parameters = parameters;
        //     customMethod.ParameterLocations = parameterLocations;
        //     customMethod.MethodLocation = methodLocation;
        //     return customMethod;
        // }
        private CustomMethodBase GetObject()
        {
            return (CustomMethodBase)Activator.CreateInstance(Type);
        }

        static CustomMethodData[] _customMethodData = null;
        public static CustomMethodData[] GetCustomMethods()
        {
            if (_customMethodData == null)
            {
                Type[] types = Assembly.GetExecutingAssembly().GetTypes()
                    .Where(type => type.GetCustomAttribute<CustomMethod>() != null)
                    .ToArray();

                _customMethodData = new CustomMethodData[types.Length];
                for (int i = 0; i < _customMethodData.Length; i++)
                    _customMethodData[i] = new CustomMethodData(types[i]);
            }
            return _customMethodData;
        }

        public static CustomMethodData GetCustomMethod(string name)
        {
            return GetCustomMethods().FirstOrDefault(method => method.Name == name);
        }

        public static CompletionItem[] GetCompletion()
        {
            return GetCustomMethods().Select(cm => new CompletionItem(cm.Name) 
            { 
                detail = cm.GetLabel(false),
                kind = CompletionItem.Method,
                documentation = cm.Wiki?.Description
            }).ToArray();
        }
    }

    public abstract class CustomMethodBase
    {
        // public TranslateRule TranslateContext { get; set; }
        public IWorkshopTree[] Parameters { get; set; }
        // public ScopeGroup Scope { get; set; }
        public Location[] ParameterLocations { get; set; }
        public Location MethodLocation { get; set; }

        public MethodResult Result()
        {
            // if (TranslateContext == null)
                // throw new ArgumentNullException(nameof(TranslateContext));
            
            if (Parameters == null)
                throw new ArgumentNullException(nameof(Parameters));
            
            // if (Scope == null)
                // throw new ArgumentNullException(nameof(Scope));
            
            if (ParameterLocations == null)
                throw new ArgumentNullException(nameof(ParameterLocations));
            
            if (MethodLocation == null)
                throw new ArgumentNullException(nameof(MethodLocation));
            
            return Get();
        }

        protected abstract MethodResult Get();

        public abstract CustomMethodWiki Wiki();
    }

    public class CustomMethodWiki
    {
        public string Description { get; }
        public string[] ParameterDescriptions { get; }
        public CustomMethodWiki(string description, params string[] parameterDescriptions)
        {
            Description = description;
            ParameterDescriptions = parameterDescriptions;
        }
    }
}
