using System;
using System.Collections.Generic;
using Deltin.Deltinteger.I18n;

namespace Deltin.Deltinteger.Lobby
{
    public class SettingNameResolver
    {
        public string Title { get; }

        public SettingNameResolver(string title)
        {
            Title = title;
        }

        public virtual string ResolveName(WorkshopBuilder builder) => builder.Translate($"setting.{Title}");
        public virtual Keyword GetKeyword() => ($"setting.{Title}", Title);
    }

    public class AbilityNameResolver : SettingNameResolver
    {
        public string NodeName { get; }
        public string FormattedIdentifier { get; }

        public AbilityNameResolver(string formattedIdentifier, string settingTitle, string nodeName) : base(settingTitle)
        {
            NodeName = nodeName;
            FormattedIdentifier = formattedIdentifier;
        }

        public override string ResolveName(WorkshopBuilder builder)
        {
            string def = $"setting.{Title}";
            if (LanguageInfo.IsKeyword(def)) return builder.Translate(def).RemoveStructuralChars();
            return builder.Translate(FormattedIdentifier).Replace("%1$s", builder.Translate($"setting.{NodeName}")).RemoveStructuralChars();
        }

        public override Keyword GetKeyword() => ($"setting.{NodeName}", NodeName);
    }
}