namespace Deltinteger.Tests;

[TestClass]
public class SuperscriptTest
{
    [TestMethod("Pokemon Overwatch")]
    [DeploymentItem(@"Assets/TestWorkshopScripts/PokemonOverwatch.ostw")]
    public void PokemonOverwatch()
    {
        TestUtils.AtomizeAndReconstruct("PokemonOverwatch.ostw");
    }

    [TestMethod("Get stick bugged lol")]
    [DeploymentItem(@"Assets/TestWorkshopScripts/GetStickBuggedLol.ostw")]
    public void GetStickBuggedLol()
    {
        TestUtils.AtomizeAndReconstruct("GetStickBuggedLol.ostw");
    }

    [TestMethod("SUPERHOT")]
    [DeploymentItem(@"Assets/TestWorkshopScripts/SUPERHOT.ostw")]
    public void SUPERHOT()
    {
        TestUtils.AtomizeAndReconstruct("SUPERHOT.ostw");
    }

    [TestMethod("Overwatch Unlimited 3")]
    [DeploymentItem(@"Assets/TestWorkshopScripts/OverwatchUnlimited3.ostw")]
    public void OverwatchUnlimited3()
    {
        TestUtils.AtomizeAndReconstruct("OverwatchUnlimited3.ostw");
    }

    [TestMethod("Roll For Initiative")]
    [DeploymentItem(@"Assets/TestWorkshopScripts/RollForInitiative.ostw")]
    public void RollForInitiative()
    {
        TestUtils.AtomizeAndReconstruct("RollForInitiative.ostw");
    }
}