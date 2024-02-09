namespace Deltinteger.Tests;

[TestClass]
public class SuperscriptTest
{
    [TestMethod("Pokemon Overwatch")]
    [DeploymentItem(@"TestWorkshopScripts\PokemonOverwatch.ostw")]
    public void PokemonOverwatch()
    {
        TestUtils.AtomizeAndReconstruct("PokemonOverwatch.ostw");
    }

    [TestMethod("Get stick bugged lol")]
    [DeploymentItem(@"TestWorkshopScripts\GetStickBuggedLol.ostw")]
    public void GetStickBuggedLol()
    {
        TestUtils.AtomizeAndReconstruct("GetStickBuggedLol.ostw");
    }

    [TestMethod("SUPERHOT")]
    [DeploymentItem(@"TestWorkshopScripts\SUPERHOT.ostw")]
    public void SUPERHOT()
    {
        TestUtils.AtomizeAndReconstruct("SUPERHOT.ostw");
    }
}