using Deltin.Deltinteger.Compiler.File;

namespace Deltinteger.Tests;

// [TestClass]
public class VersionInstanceTest
{
    [TestMethod("Convert index and document positions")]
    public void ConvertIndexAndDocumentPositions()
    {
        var version = new VersionInstance("""
            Text1
            Text2
            """);

        {
            // Find the index of the '1' character.
            var index = version.Text.IndexOf('1');
            Assert.AreEqual(4, index);

            var pos = version.GetPos(index);
            // Convert index to position and back to index.
            Assert.AreEqual(new(0, 4), pos);
            Assert.AreEqual(4, version.IndexOf(pos));
            // '1' is the last character on the line.
            // It should be possible to target the end of that character
            // without progressing into the next line.
            Assert.AreEqual(new(0, 5), version.GetPos(index + 1));
        }
        {
            // More tests with Text2.
            var index = version.Text.IndexOf("Text2");
            Assert.AreEqual(7, index); // "Text1" + '\r' + '\n' is 7 characters.
            var pos = version.GetPos(index);
            // Validate pos and convert back to index.
            Assert.AreEqual(new(1, 0), pos);
            Assert.AreEqual(7, version.IndexOf(pos));
            // Get opposite end of "Text2"
            Assert.AreEqual(new(1, 5), version.GetPos(index + 5));
        }
    }
}