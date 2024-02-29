using System.Text.RegularExpressions;
using Deltin.Deltinteger;
using Deltin.Deltinteger.Parse;

namespace Deltinteger.Tests;

static class TestUtils
{
    public static void Setup()
    {
        LoadData.LoadFromFileSystem();
    }

    public static (string, Diagnostics) Compile(string text)
    {
        var d = new Diagnostics();
        var ds = new DeltinScript(new(d, new ScriptFile(d, new Uri("inmemory://temp.ostw"), text))
        {
            SourcedSettings = new(null, new()
            {
                CompileMiscellaneousComments = false,
                CStyleWorkshopOutput = true,
                OptimizeOutput = false
            })
        });
        return (ds.WorkshopCode, d);
    }

    /// <summary>Sends OW code through OSTW and determines if it comes out unscathed.</summary>
    /// <param name="filename">The file name of the workshop code.</param>
    public static void AtomizeAndReconstruct(string filename)
    {
        Setup();
        string text = File.ReadAllText(filename);
        var (compiled, diagnostics) = Compile(text);

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
        Assert.AreEqual(textNoWs.Length, compiledNoWs.Length);
    }
}