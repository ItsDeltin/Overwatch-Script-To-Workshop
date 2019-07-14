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
            
            Wiki = GetObject().Wiki();
        }

        public CustomMethodBase GetObject(Translate context, ScopeGroup scope, IWorkshopTree[] parameters)
        {
            CustomMethodBase customMethod = GetObject();
            customMethod.TranslateContext = context;
            customMethod.Scope = scope;
            customMethod.Parameters = parameters;
            return customMethod;
        }
        private CustomMethodBase GetObject()
        {
            return (CustomMethodBase)Activator.CreateInstance(Type);
        }

        static CustomMethodData[] _customMethodData = null;
        private static CustomMethodData[] GetCustomMethods()
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
        public Translate TranslateContext { get; set; }
        public IWorkshopTree[] Parameters { get; set; }
        public ScopeGroup Scope { get; set; }

        public MethodResult Result()
        {
            if (TranslateContext == null)
                throw new ArgumentNullException(nameof(TranslateContext));
            
            if (Parameters == null)
                throw new ArgumentNullException(nameof(Parameters));
            
            if (Scope == null)
                throw new ArgumentNullException(nameof(Scope));
            
            return Get();
        }

        protected abstract MethodResult Get();

        public abstract WikiMethod Wiki();
    }
}
