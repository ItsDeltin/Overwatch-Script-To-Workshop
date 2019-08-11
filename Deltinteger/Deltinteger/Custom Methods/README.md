[This folder](https://github.com/ItsDeltin/Overwatch-Script-To-Workshop/tree/master/Deltinteger/Deltinteger/Custom%20Methods) contains all the custom methods. [Pi.cs](https://github.com/ItsDeltin/Overwatch-Script-To-Workshop/blob/master/Deltinteger/Deltinteger/Custom%20Methods/Pi.cs) is a simple example:
```
[CustomMethod("Pi", CustomMethodType.Value)]
    class Pi : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            return new MethodResult(null, new V_Number(Math.PI));
        }

        public override WikiMethod Wiki()
        {
            return new WikiMethod("Pi", "Represents the ratio of the circumference of a circle to its diameter, specified by the constant π. Equal to " + Math.PI, null);
        }
    }
```
`[CustomMethod("Pi", CustomMethodType.Value)]`: the first argument "Pi" is the name of the method, so it is called by doing Pi(). CustomMethodType.Value means that this method returns a value. There are 3 types:
* `CustomMethodType.Value`: These methods returns a value.
    * `InsertValueInArray`
    * `RemoveFromArrayAtIndex`
    * `Pi`
    * `AngleOfVectorsCom`
* `CustomMethodType.MultiAction_Value`: These methods returns a value but requires some actions to get the result. These can't be used in conditions.
    * `GetMap`
    * `AngleOfVectors`
    * `IsConditionTrue`
    * `IsConditionFalse`
* `CustomMethodType.Action`: These methods are just actions.
    * `ChaseVariable`
    * `MinWait`
	
`protected override MethodResult Get()`: gets the actions and resulting value.

`new MethodResult(null, new V_Number(Math.PI))`: the first argument (`null`) are the actions. The second argument (`new V_Number(Math.PI)`) is the resulting value.

`public override WikiMethod Wiki()`: gets documentation data for the vscode extension:
![VSCode extension](https://user-images.githubusercontent.com/34138844/62826305-425ed100-bb87-11e9-9052-93ada40984d1.png)

[AngleOfVectors.cs](https://github.com/ItsDeltin/Overwatch-Script-To-Workshop/blob/master/Deltinteger/Deltinteger/Custom%20Methods/AngleOfVectors.cs) is a multi-action value.

To add parameters, add the Parameter attribute:
```
    [CustomMethod("AngleOfVectors", CustomMethodType.MultiAction_Value)]
    [Parameter("Vector1", ValueType.VectorAndPlayer, null)]
    [Parameter("Vector2", ValueType.VectorAndPlayer, null)]
    [Parameter("Vector3", ValueType.VectorAndPlayer, null)]
    class AngleOfVectors : CustomMethodBase
    {
```

The parameter values are obtained with the `CustomGameBase.Parameters` array.
```
Element vector1Parameter = (Element)Parameters[0];
Element vector2Parameter = (Element)Parameters[1];
Element vector3Parameter = (Element)Parameters[2];
```