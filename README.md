# Deltin's Script To Workshop
<center>
<img src="https://i.imgur.com/38SWJCm.png" alt="" height="350"/>
<img src="https://i.imgur.com/hFdmeew.png" alt="" height="350"/>
</center>

Create a scipt that will be converted to an Overwatch Workshop. 

## Usage

### Infinite named variables
```
// These variables can be referenced from anywhere in the script
define globalvar myGlobalVar;
define playervar myPlayerVar;

rule: "My Rule"
{
    // Scoped variables work too. It will be a global/player variable depending on the rule it is defined in.
    define myVar = NumberOfPlayers() - 1;
}
```

### No more operator or compare trees.
Or/and statements can easily be done using OR `|` and AND `&`.
```
rule: "Initial Setup"
if (!isInitialized & IsGameInProgress()) 
{
    // ...

    isInitialized = true;
}
```
You can multiply/divide/subtract/add/pow/modulo any expression and it will automatically create the tree following orders of operations.
```
Sqrt(XOf(vec1) * XOf(vec1) + YOf(vec2) * YOf(vex2) + ZOf(vec3) * ZOf(vec3))
```

### If - Else If - Else
```
if (hunterStep < CountOf(hunterVictim.killer.steps) - 1)
{
    hunterStep += 1;
    hunterLocation = hunterVictim.killer.steps[hunterStep];
}
else
{
    hunterLocation = PositionOf(hunterVictim.killer);
}
```

### Ternary Conditionals
```
// If the variable 'isGoingUp' is true, 'directionModifier' will equal 1. Otherwise, it will equal -1.
define directionModifier = isGoingUp ? 1 : -1;
```

### Arrays
Arrays can be created by a group of values inside brackets.
```
locations = [Vector(56.64, 21.00, -67.14), Vector(50.46, 9.15, -92.95), Vector(30.00, 14.00, -77.91), Vector(82.59, 12.68, -88.21)];
```
This will generate a tree of Append To Arrays. Array values can be accessed like so:
```
// This will send the message "56.64, 21.00, -67.14" to all players.
SmallMessage(AllPlayers(), locations[0]);
```

### Effortless loops

#### for
```
for (define i = 0; i < CountOf(AllPlayers()); i++)
{
    define teamID = RandomInteger(0, 4);

    SmallMessage(AllPlayers()[i], <"team: <0>", teamID + 1>);

    AllPlayers()[i].team = teamID;
}
```

#### foreach
```
define radius = 8;

define locations = [Vector(56.64, 21.00, -67.14), Vector(50.46, 9.15, -92.95), Vector(30.00, 14.00, -77.91), Vector(82.59, 12.68, -88.21)];

for (define loc in locations)
{
    CreateEffect(AllPlayers(), Effect.Sphere, Color.Red, loc, radius, EffectRev.VisibleTo);
}
```

`foreach` takes an optional repeater parameter. Makes the `foreach` faster, but uses more actions. For example:
```
rule: "Repeater test."
Event.OngoingPlayer 
if (IsButtonHeld(EventPlayer(), Button.Interact))
{
    define start = TotalTimeElapsed();    
    foreach 5 (define player in AllPlayers()) // 5 is the repeater count
    {
        SmallMessage(player, "hello!");
    }
    define finished = TotalTimeElapsed();
    SmallMessage(EventPlayer(), <"time finished: <0>", finished - start>);
}
```

| Repeater count  | Time to Complete | Actions |
| --------------- | ---------------- | ------- |
| 1               | 19 ms            | 12      |
| 2               | 10 ms            | 14      |
| 3               | 6 ms             | 16      |
| 4               | 5 ms             | 18      |
| 5               | 3 ms             | 20      |

The number of actions scale with the number of statements in the `foreach`.

#### while
```
while (scanning)
{
    if (AngleOfVectors(cameraPos, cameraLookingAt, targetPlayer) < 45)
    {
        scanning = false;
        SmallMessage(targetPlayer, "warning: detected!");
    }
}
```

### Effortless strings
Strings can easily be created. They will be translated for the workshop to use. The strings must be already in the game. An exception will be thrown if a string is unrecognized.

```
SmallMessage(AllPlayers(), <"hello? thank_you teammate, that_was_awesome!">);
```
Format works as well.
```
SmallMessage(AllPlayers(), <"hello? thank_you <0>, that_was_awesome!", PlayerClosestToReticle(EventPlayer())>);
```

[**List Of Strings**](https://github.com/ItsDeltin/Overwatch-Script-To-Workshop/blob/master/Deltinteger/Deltinteger/Constants.cs)

### Setting player variables

`EventPlayer()` is optional, it is used by default. Both of these work:
```
EventPlayer().speedBuff = 120
speedBuff = 120
```
If a variable is set to a player, you can use it to set that player's variable like so:
```
define target = RandomValueInArray(AllPlayers());
target.speedBuff = 120;
```
Player arrays will set every player in the array's variable.
```
// Reset all player's speed buff.
AllPlayers().speedBuff = 100;
```

### Methods

#### IsAI() Example

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
IsAI() will return true if the player is an AI, otherwise it will return false.

#### Recursion

To allow recursion in a method, do `recursive method` instead of just `method`.
```
# Prints every value in a multidimensional array.
recursive method arrayWalker(array, dims)
{
    // ...
}
```

### Structs


```
TeleportSphere playervar spheres;
define playervar wasCreated;

rule: "Create sphere"
Event.OngoingPlayer
if (IsButtonHeld(EventPlayer(), Button.Interact))
{
    if (!wasCreated)
    {
        spheres = new TeleportSphere(EventPlayer(), PositionOf(EventPlayer()));
        spheres.Create(); 
    }
}

rule: "Destroy"
Event.OngoingPlayer
if (IsButtonHeld(EventPlayer(), Button.PrimaryFire))
{
    spheres.Destroy();
}

rule: "Return"
Event.OngoingPlayer
if (IsButtonHeld(EventPlayer(), Button.SecondaryFire))
{
    spheres.Return();
}

struct TeleportSphere
{
    define location;
    define owner;
    define radius = 5;
    define initialRadius = radius;
    define effectID;
    define wasDestroyed = false;

    public TeleportSphere(owner, location)
    {
        this.owner = owner;
        this.location = location;
        owner.wasCreated = true;
    }

    private method Create()
    {
        CreateEffect(AllPlayers(), Effect.Sphere, Color.Red, this.location, radius, EffectRev.VisibleToPositionAndRadius);
        effectID = LastCreatedEntity();
        ApplyImpulse(EventPlayer(), Vector(0, 10, 0), 10);
    }

    private method Destroy()
    {
        if (wasDestroyed) return;

        ChaseVariable(radius, 0, 10);

        // Wait for the effect to be destroyed
        CreateEffect(AllPlayers(), Effect.BadAura, Color.Red, location, radius + 1);
        define sparkEffect = LastCreatedEntity();
        while (radius != 0);
        DestroyEffect(sparkEffect);

        ChaseVariable(radius, 0, 0);

        // Play an explosion
        PlayEffect(AllPlayers(), PlayEffect.BadExplosion, Color.Red, location, 3);

        // Damage nearby players
        foreach 6 (define player in AllPlayers())
            if (IsInRange(player))
                Damage(player, owner, 50);

        DestroyEffect(effectID);
        wasDestroyed = true;
        owner.wasCreated = false;
    }

    private method IsInRange(player)
    {
        return DistanceBetween(PositionOf(player), location) < initialRadius;
    }

    private method Return()
    {
        if (wasDestroyed) return;
        Teleport(owner, location);
        PlayEffect(AllPlayers(), PlayEffect.GoodExplosion, Color.Blue, location, radius - 1);
    }
}
```

Public, private, and static support will come later.

### Custom methods
OSTW contains methods that are not found in the Overwatch Workshop.

| Method                 | Type              | Description |
| ---------------------- | ----------------- | ----------- |
| GetMap                 | Multiaction Value | `GetMap()` gets the current map. This is based off of [Xerxes's Map Identifier](https://us.forums.blizzard.com/en/overwatch/t/workshop-resource-map-identifier-map-detection-script-v2-0-only-2-actions/341132). The result can be compared to with the `Map` enum.
| AngleOfVectors         | Multiaction Value | Gets the angle of 3 vectors. 
| AngleOfVectorsCom      | Value             | Behaves the same as AngleOfVectors but condensed into one action.
| ChaseVariable          | Action            | Behaves the same as the workshop method `Chase Global/Player Variable At Rate`, but will work with named variables. Works with numbers and vectors.
| MinWait                | Action            | Same as doing `Wait(0.016)`.
| InsertValueInArray     | Value             | Inserts a value into an array at an index.
| RemoveFromArrayAtIndex | Value             | Removes value from an array at an index.
| Pi                     | Value             | Returns the constant π: `3.14159265358979`.
| IsConditionTrue        | Multiaction Value | Determines if the condition is true. Has a 0.016 second delay.
| IsConditionFalse       | Multiaction Value | Determines if the condition is false. Has a 0.016 second delay.

#### GetMap()
```
define globalvar currentMap;

rule: "Get current map"
{
    currentMap = GetMap();

    if (currentMap == Map.Dorado)
    {
        // ...
    }
}
```

#### AngleOfVectors(vector, vector, vector)
```
define angle = AngleOfVectors(
    Vector(0, 1, 0),
    Vector(0, 0, 0),
    Vector(1, 0, 0)
);
// angle should now equal 90.
```

#### ChaseVariable(ref Variable, Destination, Rate)
```
define myVar = 0;

// Counts to 10 in 10 seconds.
ChaseVariable(myVar, 10, 1);

// Player variables work too
ChaseVariable(AllPlayers().playerVar, 10, 1);
```

## Deltinteger.exe
### Arguments:
- `-langserver`: Starts the language server.
- `-port xxxx yyyy`: The 2 ports the language server uses.
- `-verbose`/`-quiet`

### Copying the script into Overwatch

#### With the VSCode extension

Install the `overwatch-script-to-workshop-x.x.x.vsix` extension file.

![](https://i.imgur.com/cwTBkNp.png)

In `Start Language Server.bat` make sure the `-port` argument matches the ports set in the settings `ostw.port1` and `ostw.port2`. Is 9145 and 9146 by default. Launch the bat to start the language server.

You can press `ctrl+space` to get a list of all the methods.

The extention adds a channel in vscode's output tab with the compiled workshop code. This can easily be copied into Overwatch.

![](https://i.imgur.com/bB2kZcE.png)

#### Without the VSCode extension
Drop your script into the Deltinteger.exe executable to generate the script. The workshop code will be copied into your clipboard.