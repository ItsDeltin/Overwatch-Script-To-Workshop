using System.Diagnostics;
using System.Text.RegularExpressions;
using Deltin.Deltinteger;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Emulator;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Parse.Settings;

namespace Deltinteger.Tests;

static class TestUtils
{
    public static void Setup()
    {
        LoadData.LoadFromFileSystem();
    }

    public static CompileResult Compile(string text,
        bool classGenerations = false,
        ReferenceValidationType referenceValidationStrategy = ReferenceValidationType.Subroutine,
        string? variablePrefix = null)
    {
        Setup();
        var d = new Diagnostics();
        var ds = new DeltinScript(new(d, text)
        {
            SourcedSettings = new(null, new()
            {
                CompileMiscellaneousComments = false,
                CStyleWorkshopOutput = true,
                OptimizeOutput = false,
                AbortOnError = true,
                TrackClassGenerations = classGenerations,
                LogDeleteReferenceZero = classGenerations,
                GlobalReferenceValidation = classGenerations,
                ReferenceValidationType = referenceValidationStrategy,
                VariablePrefix = variablePrefix
            })
        });
        return new(ds.WorkshopCode, d, ds.WorkshopRules);
    }

    public static void UnitTest(string code, params (string[] Formats, (string Variable, double Value)[] Asserts)[] variants)
    {
        foreach (var (formats, asserts) in variants)
        {
            var compile = code;
            for (int i = 0; i < formats.Length; i++)
                compile = compile.Replace($"{{{i}}}", formats[i]);

            var emulation = Compile(compile).AssertOk().EmulateTick();

            foreach (var (emulatedVariable, expectedValue) in asserts)
                emulation.AssertVariable(emulatedVariable, expectedValue);
        }
    }

    /// <summary>Sends OW code through OSTW and determines if it comes out unscathed.</summary>
    /// <param name="filename">The file name of the workshop code.</param>
    public static void AtomizeAndReconstruct(string filename)
    {
        Setup();
        string text = File.ReadAllText(filename);
        var (compiled, diagnostics, _) = Compile(text);

        Assert.IsFalse(diagnostics.ContainsErrors());

        // Remove all whitespace and trailing zeros in numbers
        // Parentheses are also ignored because ostw doesn't bother with some optional groupings
        // that code from overwatch includes. Unfortunately this means any issues with precedence
        // will not be caught by any test calling this function.
        var r = new Regex(@"\s+|\(|\)|(([0-9]+\.[0-9]*?[1-9])|([0-9]+)\.)0+(?=[^0-9]|$)", RegexOptions.Multiline);
        var textNoWs = r.Replace(text, "$2$3").ToLower();
        var compiledNoWs = r.Replace(compiled, "$2$3").ToLower();

        for (int i = 0; i < textNoWs.Length && i < compiledNoWs.Length; i++)
        {
            if (textNoWs[i] != compiledNoWs[i])
            {
                const int showRange = 30;
                Assert.Fail("Input text and compiled text are different. Change starts at {0}. Compare:\n\"{1}\"\n\"{2}\")",
                    i,
                    textNoWs[(i - showRange)..(i + showRange)],
                    compiledNoWs[(i - showRange)..(i + showRange)]);
            }
        }
        Assert.AreEqual(textNoWs.Length, compiledNoWs.Length, "Input workshop text and compiled text have different lengths");
    }
}

readonly record struct CompileResult(string Code, Diagnostics Diagnostics, List<Rule> Rules)
{
    /// <summary>Ensures that the compilation was successful and without errors.</summary>
    /// <returns>Self</returns>
    public readonly CompileResult AssertOk()
    {
        var error = Diagnostics.FindFirstError();
        Assert.IsNull(error, $"Unexpected error while compiling: {error?.message}");
        return this;
    }

    /// <summary>Tests if the provided text is within any of the diagnostics resulting from the compilation.</summary>
    /// <param name="text">The substring that is searched within every diagnostic.</param>
    /// <returns>Self</returns>
    public readonly CompileResult AssertSearchError(string text)
    {
        Assert.IsTrue(Diagnostics.Enumerate().Any(diagnostic => diagnostic.message.Contains(text)), $"Failed to find error with text '{text}'");
        return this;
    }

    /// <summary>Searches the compiled code for the provided text.</summary>
    /// <param name="text">The substring that is searched within the compiled code.</param>
    /// <returns>Self</returns>
    public readonly CompileResult AssertText(string text)
    {
        Assert.IsTrue(Code.Contains(text), $"Compiled code does not contain \"${text}\"");
        return this;
    }

    /// <summary>Begins an emulation session and executes a single tick.</summary>
    /// <returns>Self</returns>
    public readonly TickEmulationResult EmulateTick()
    {
        string output = string.Empty;
        var emulation = new EmulateScript(Rules, IEmulateLogger.New(log =>
        {
            Debug.WriteLine(log);
            if (log.Contains("[BREAK]"))
                Debugger.Break();
            output += log;
        }));
        var result = emulation.TickOne();
        return new(emulation, output, result);
    }
}

readonly record struct TickEmulationResult(EmulateScript Emulation, string Log, RuleTickResult Result)
{
    public readonly TickEmulationResult AssertVariable(string name, EmulateValue value)
    {
        var actual = Emulation.GetGlobalVariableValue(name);
        Assert.IsTrue(EmulateValue.AreEqual(actual, value), $"'{name}' has incorrect value. Got {actual}, expected {value}");
        return this;
    }

    public readonly TickEmulationResult AssertVariable(string name, EmulateValue[] values)
    {
        var value = EmulateValue.From(values);
        return AssertVariable(name, value);
    }

    public readonly TickEmulationResult AssertSearchLog(string text)
    {
        Assert.IsTrue(Log.Contains(text), $"Missing text '{text}' in output log");
        return this;
    }
}