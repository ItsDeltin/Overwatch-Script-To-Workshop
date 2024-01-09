#nullable enable

namespace Deltin.Deltinteger.Compiler.SyntaxTree;

using System;
using System.Collections.Generic;

public readonly struct RootElement
{
    readonly Import? _import = null;
    readonly RuleContext? _rule = null;
    readonly ClassContext? _classContext = null;
    readonly EnumContext? _enumContext = null;
    readonly IDeclaration? _declaration = null;
    readonly VanillaVariableCollection? _variables = null;
    readonly VanillaRule? _vanillaRule = null;
    readonly TypeAliasContext? _typeAlias = null;

    public RootElement(Import import) => _import = import;
    public RootElement(RuleContext rule) => _rule = rule;
    public RootElement(ClassContext classContext) => _classContext = classContext;
    public RootElement(EnumContext enumContext) => _enumContext = enumContext;
    public RootElement(IDeclaration declaration) => _declaration = declaration;
    public RootElement(VanillaVariableCollection variables) => _variables = variables;
    public RootElement(VanillaRule vanillaRule) => _vanillaRule = vanillaRule;
    public RootElement(TypeAliasContext typeAlias) => _typeAlias = typeAlias;

    public static void Iter(
        IEnumerable<RootElement> rootObjects,
        Action<Import>? import = null,
        Action<RuleContext>? rule = null,
        Action<ClassContext>? classContext = null,
        Action<EnumContext>? enumContext = null,
        Action<IDeclaration>? declaration = null,
        Action<VanillaVariableCollection>? variables = null,
        Action<VanillaRule>? vanillaRule = null,
        Action<TypeAliasContext>? typeAlias = null
    )
    {
        foreach (var rootObject in rootObjects)
        {
            rootObject.Match(import, rule, classContext, enumContext, declaration, variables, vanillaRule, typeAlias);
        }
    }

    public void Match(
        Action<Import>? import = null,
        Action<RuleContext>? rule = null,
        Action<ClassContext>? classContext = null,
        Action<EnumContext>? enumContext = null,
        Action<IDeclaration>? declaration = null,
        Action<VanillaVariableCollection>? variables = null,
        Action<VanillaRule>? vanillaRule = null,
        Action<TypeAliasContext>? typeAlias = null
        )
    {
        if (_import is not null) import?.Invoke(_import!);
        else if (_rule is not null) rule?.Invoke(_rule!);
        else if (_classContext is not null) classContext?.Invoke(_classContext!);
        else if (_enumContext is not null) enumContext?.Invoke(_enumContext!);
        else if (_declaration is not null) declaration?.Invoke(_declaration!);
        else if (_variables is not null) variables?.Invoke(_variables!);
        else if (_vanillaRule is not null) vanillaRule?.Invoke(_vanillaRule!);
        else if (_typeAlias is not null) typeAlias?.Invoke(_typeAlias!);
    }
}