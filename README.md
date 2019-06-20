# Deltin's Script To Workshop
<center>
<img src="https://i.imgur.com/38SWJCm.png" alt="" height="350"/>
<img src="https://i.imgur.com/hFdmeew.png" alt="" height="350"/>
</center>

Create a scipt that will be converted to an Overwatch Workshop. Includes if/else if/else, for loops, easy string creation, infinite named variables, no more expression trees, easy array creation, and methods!

## Usage
### Infinite named variables
```
define globalvar allZombies;
define playervar isZombie;
```
These variables can be referenced anywhere in the script.
```
rule: "Swap player when they die."
Event.PlayerDied
if (!isZombie)
{
    isZombie = true;
    AppendToArray(allZombies, EventPlayer());
}
```
### No more operator or compare trees.
Or/and statements can easily be done by doing `true | true & true`.
```
rule: "Swap players when they die"
  Event.Player_Died
  if (!isZombie & !isVaccinated)
  {
  	isZombie = true;
    AppendToArray(allZombies, EventPlayer());
  }
```
You can multiply/divide/subtract/add/pow/modulo any expression and it will automatically create the tree following orders of operations.
(Sqrt is converted to Square Root in the workshop. XOf, YOf, and ZOf are converted to their respective ComponentOf. Most methods in the workshop have the same name.)
```
Sqrt(XOf(vec1) * XOf(vec1) + YOf(vec2) * YOf(vex2) + ZOf(vec3) * ZOf(vec3))
```
### Easy array creation
Arrays can easily be created with brackets.
```
locations = [Vector(56.64, 21.00, -67.14), Vector(50.46, 9.15, -92.95), Vector(30.00, 14.00, -77.91), Vector(82.59, 12.68, -88.21)];
``` 
This will generate a tree of Append To Arrays.
### If - Else If - Else
```
if (IsOnGround(EventPlayer()) & IsCrouching(EventPlayer))
{
	height = 0;
}
else if (IsOnGround(EventPlayer()))
{
	height = 1;
}
else
{
	height = 2;
}
```
### Effortless for loops
The script below will create 5 red spheres on Eichenwalde. If a player goes inside any of the spheres, they will die. The vector array containing the sphere locations is created like so:
`locations = [Vector(56.64, 21.00, -67.14), Vector(50.46, 9.15, -92.95), Vector(30.00, 14.00, -77.91), Vector(82.59, 12.68, -88.21)];`. The for loop then loops through it doing `for (loc in locations)`.

```
// Make sure the map is Eichenwalde!

usevar globalvar A;
usevar playervar A;

define globalvar locations;
define globalvar radius;
define playervar deathCount;

rule: "Setup Death Spheres."

    {
        radius = 8;
        locations = [Vector(56.64, 21.00, -67.14), Vector(50.46, 9.15, -92.95), Vector(30.00, 14.00, -77.91), Vector(82.59, 12.68, -88.21)];

        for (loc in locations)
        {
            CreateEffect(AllPlayers(), Effect.Sphere, Color.Red, locations[loc], radius, EffectRev.VisibleTo);
        }
    }

rule: "Kill players in sphere."
    Event.Ongoing_EachPlayer
    if (IsAlive())
    {
        // Kill the player if they enter the radius of the death sphere.
        for (loc in locations)
        {
            if (DistanceBetween(EventPlayer(), locations[loc]) < radius)
            {
                deathCount += 1;
                SmallMessage(EventPlayer(), <"Danger! ... dead, try_again and survive! sudden_death: <0>", deathCount>);
                Kill();
                loc = CountOf(locations);
            }
        }

        // Loop
        Wait(0.06);
        LoopIfConditionIsTrue();
    }
```
### Effortless strings
Strings can easily be created. They will be translated for the workshop to use. The strings must be already in the game (https://pastebin.com/ZuvCeFRp). An exception will be thrown if a string is unrecognized.
```
SmallMessage(AllPlayers(), <"hello? thank_you teammate, that_was_awesome!">);
```
Format works as well.
```
SmallMessage(AllPlayers(), <"hello? thank_you <0>, that_was_awesome!", PlayerClosestToReticle(EventPlayer())>);
```
### Setting player variables
`AllPlayers().variable = 4` will set every player's player-variable to the specified value.
`EventPlayer().target.speedBuff = 120` A list of players work too, this will set the event player's target's speed boost to 120.  

### Methods
```
method IsAI(player)
{
	define currentHero = HeroOf(player); 
	define heroSwap = team.team;

	/*
	Swap a player to Ana (Bastion if they are Ana), check if they are the new hero, then swap them back.
	Possible improvements: Swap to a hero that isnt an option for AI.
	*/
	
	if (currentHero == Hero.Ana)
		heroSwap = Hero.Bastion;

	ForcePlayerHero(player, heroSwap);

	define isAI = HeroOf(player) == currentHero;

	ForcePlayerHero(player, currentHero);
	StopForcingHero(player);
	
	return isAI;
}
```
IsAI() will return true if the player is an AI, otherwise it will return false. By default, recursive methods are disabled because it would make the final output of methods a lot more complicated. Start `Deltinteger.exe` with the argument `-allowresursion` to enable it.

### Custom methods
This contains some custom methods that are not included in the Workshop.

`GetMapID()` gets the current map. This is based off of Xerxes's workshop code found from here:
https://us.forums.blizzard.com/en/overwatch/t/workshop-resource-get-the-current-map-name-updated-1-action/

`AngleOfVectors(vector, vector, vector)` gets the angle of 3 vectors. Returns a value between -1 to 1. -1 being 180 degrees, 0 being 90 degrees, and 1 being 0 degrees.

## Deltinteger.exe
### Arguments:
- `-langserver`: Starts the language server.
- `-port xxxx yyyy`: The 2 ports the language server uses.
- `-allowrecursion`: Allows methods to be recursive.
- `-verbose`/`-quiet`

### Copying the script into Overwatch

#### With the VSCode extension

Install the `overwatch-script-to-workshop-x.x.x.vsix` extension file.

![](https://i.imgur.com/cwTBkNp.png)

In `Start Language Server.bat` make sure the `-port` argument matches the ports set in the settings `ostw.port1` and `ostw.port2`. Is 3000 and 3001 by default. Launch the bat to start the language server.

You can press `ctrl+space` to get a list of all the methods.

The extention adds a channel in vscode's output tab with the compiled workshop code. This can easily be copied into Overwatch.

![](https://i.imgur.com/bB2kZcE.png)

#### Without the VSCode extension
Drop your script into the Deltinteger.exe executable to generate the script. The workshop code will be copied into your clipboard.