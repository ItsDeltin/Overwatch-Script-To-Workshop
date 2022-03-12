using System;
using System.Linq;
using System.Collections.Generic;

namespace DS.Analysis
{
    using Scopes;
    using Types;

    /// <summary>
    /// Gets the name of the type as represented in the current context. For example, if the user imports a class like so:
    /// <code>
    /// import { x, y as z } from MyModule;
    /// </code>
    /// <para>
    /// In signatures that use these types, <c>x</c> is represented as <c>x</c> and <c>y</c> is represented as <c>z</c>.
    /// </para>
    /// <para>
    /// If the import statement were to be removed, <c>x</c> would be represented as <c>MyModule.x</c> and <c>y</c> as <c>MyModule.y</c>.
    /// </para>
    /// </summary>
    interface IGetIdentifier
    {
        /// <summary>Gets the name of the identifier using the provided context data. This may be an expensive operation, so it is recommended
        /// to execute after analysis is completed.</summary>
        /// <param name="context">Data about the current context.</param>
        /// <returns>The name as represented by the current context.</returns>
        string PathFromContext(GetIdentifierContext context);
    }

    class GetIdentifierContext
    {
        public ScopedElement[] Elements { get; }

        public GetIdentifierContext(ScopedElement[] elements)
        {
            Elements = elements;
        }
    }


    /// <summary>An IGetIdentifier structured with a parenting element.</summary>
    class GetStructuredIdentifier : IGetIdentifier
    {
        readonly string defaultName; // The default name of the element when it can't be found in the current scope.
        readonly CodeType[] typeArgs; // The type args of the element.
        readonly IGetIdentifier parent; // The parent of the element.
        readonly IScopeSearch scopeSearcher; // The element searcher.


        public GetStructuredIdentifier(string defaultName, CodeType[] typeArgs, IGetIdentifier parent, IScopeSearch scopeSearcher)
        {
            this.defaultName = defaultName;
            this.typeArgs = typeArgs;
            this.parent = parent;
            this.scopeSearcher = scopeSearcher;
        }


        public string PathFromContext(GetIdentifierContext context)
        {
            // Search for the element in the scope. If it is found, return the alias.
            string result = scopeSearcher.Find(context);

            if (result == null)
            {
                // Not found in the context, use the default name.
                result = defaultName;

                // Get the parent's path if it exists and prepend it to the current value.
                if (parent != null)
                    result = parent.PathFromContext(context) + "." + result;
            }

            // Get the type arguments.
            if (typeArgs != null && typeArgs.Length != 0)
                result += "<" + string.Join(", ", typeArgs.Select(a => a.GetIdentifier.PathFromContext(context))) + ">";

            return result;
        }


        // Locates the name of the element in the current context.
        public interface IScopeSearch
        {
            string Find(GetIdentifierContext context);
        }

        // An anonymous implementation of IScopeSearch.
        struct AnonymousScopeSearch : IScopeSearch
        {
            readonly Func<GetIdentifierContext, string> action;
            public AnonymousScopeSearch(Func<GetIdentifierContext, string> action) => this.action = action;
            public string Find(GetIdentifierContext context) => action(context);
        }


        public static GetStructuredIdentifier Create(string defaultName, CodeType[] typeArgs, IGetIdentifier parent, Func<ScopedElement, bool> predicate) =>
            new GetStructuredIdentifier(defaultName, typeArgs, parent, PredicateSearch(predicate));

        public static GetStructuredIdentifier Create(string defaultName, CodeType[] typeArgs, IGetIdentifier parent, IScopeSearch scopeSearch) =>
            new GetStructuredIdentifier(defaultName, typeArgs, parent, scopeSearch);

        public static IScopeSearch PredicateSearch(Func<ScopedElement, bool> predicate) => new AnonymousScopeSearch(context =>
        {
            HashSet<string> conflictableIdentifiers = new HashSet<string>();

            foreach (var element in context.Elements.Reverse())
            {
                // Check if the predicate matches the element.
                if (predicate(element))
                {
                    // An identifier with the same name already exists, return null due to conflict.
                    if (conflictableIdentifiers.Contains(element.Name))
                        return null;

                    // Identifier is ok to use.
                    return element.Name;
                }

                conflictableIdentifiers.Add(element.Name);
            }

            // Not found
            return null;
        });
    }

    struct UniversalIdentifier : IGetIdentifier
    {
        readonly string name;
        public UniversalIdentifier(string name) => this.name = name;
        public string PathFromContext(GetIdentifierContext context) => name;
    }
}