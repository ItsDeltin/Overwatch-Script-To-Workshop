# Deltinteger
Create a scipt that will be automatically generated in the workshop using simulated input. Includes if/else if/else, for loops, easy string creation, infinite named variables, no more expression trees, and easy array creation!

A lot of the action/value data was not tested. If you find any methods that are not being inputed correctly, please file an issue.

### Usage
#### Infinite named variables
(Or whatever the max array count is on the workshop.)
```
usevar globalvar A;
usevar playervar A;

define globalvar zombieCount;
define playervar isZombie;

define globalvar allZombies;
```
These variables can be referenced anywhere in the script.
```
rule: "Swap player when they die."
	Event.Player_Died
	if (!isZombie)
    {
    	isZombie = true;
        AppendToArray(allZombies, EventPlayer());
    }
```
#### No more operator or compare trees.
Or/and statements can easily be done by doing `true | true & true`.
```
rule: "Swap players when they die"
  Event.Player_Died
  // Make sure they do not have the vaccination powerup!
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
#### Easy array creation
Arrays can easily be created with brackets.
```
locations = [Vector(56.64, 21.00, -67.14), Vector(50.46, 9.15, -92.95), Vector(30.00, 14.00, -77.91), Vector(82.59, 12.68, -88.21)];
``` 
This will generate a tree of Append To Arrays.
#### If - Else If - Else
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
#### Effortless for loops
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
#### Effortless strings
Strings can easily be created. They will be translated for the workshop to use. The strings must be already in the game (https://pastebin.com/ZuvCeFRp). An exception will be thrown if a string is unrecognized.
```
SmallMessage(AllPlayers(), <"hello? thank_you teammate, that_was_awesome!">);
```
Format works as well.
```
SmallMessage(AllPlayers(), <"hello? thank_you <0>, that_was_awesome!", PlayerClosestToReticle(EventPlayer())>);
```
#### Setting player variables
`AllPlayers().variable = 4` will set every player's player-variable to the specified value.
`EventPlayer().target.speedBuff = 120` A list of players work too, this will set the event player's speed boost to 120.  

#### Custom methods
This contains some custom methods that are not included in the Workshop.

`GetMapID()` gets the current map. This is based off of Xerxes's workshop code found from here:
https://us.forums.blizzard.com/en/overwatch/t/workshop-resource-get-the-current-map-name-updated-1-action/

`AngleOfVectors(vector, vector, vector)` gets the angle of 3 vectors. Returns a value between -1 to 1. -1 being 180 degrees, 0 being 90 degrees, and 1 being 0 degrees.
## Generating the script
After creating your script, drop the file into the .exe. When it is ready to input, leave the workshop menu then go back in. Press enter to start the input. Press ctrl+c to cancel. If the input has an error, it is usually because of lag. In the config, increase the `smallstep`, `mediumstep`, and/or `bigstep` values. If the input is still failing and it is always at the same spot, then it is a bug with the program. File an issue with your code/the affected methods.

You can get a list of all actions and values by opening the executable directly and typing in "list all".

In Notepad++,. you can press F5 to quickly compile the script. In the `The Program to Run` input, type `C:/path/to/ScriptToWorkshop.exe $(FULL_CURRENT_PATH)`