namespace Deltin.Deltinteger.Parse.Workshop;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using static Deltin.Deltinteger.Elements.Element;

public class PersistentVariables
{
    public bool Enabled { get; }
    readonly List<PersistentEntry> persistentEntries = new List<PersistentEntry>();
    readonly IndexReference loadPersist;
    readonly ToWorkshop toWorkshop;

    public PersistentVariables(ToWorkshop toWorkshop)
    {
        var settings = toWorkshop.DeltinScript.Settings;
        Enabled = settings.ResetNonpersistent;
        this.toWorkshop = toWorkshop;

        if (Enabled)
        {
            loadPersist = toWorkshop.DeltinScript.VarCollection.Assign("__loadPersist", true, settings.PasteCheckIsExtended);
            AddPersistent(loadPersist, True());
        }
    }

    public void AddPersistent(IndexReference indexReference, Element initialValue)
    {
        if (indexReference.WorkshopVariable.IsGlobal)
            persistentEntries.Add(new PersistentEntry(indexReference, initialValue));
    }

    public void AddNonpersistent(IndexReference indexReference, Element initialValue)
    {
        if (indexReference.WorkshopVariable.IsGlobal)
            indexReference.Set(toWorkshop.DeltinScript.InitialGlobal.ActionSet, initialValue);
    }

    public void ToWorkshop()
    {
        if (!Enabled)
            return;

        var actionSet = toWorkshop.DeltinScript.InitialGlobal.ActionSet;

        actionSet.AddAction(If(Not(loadPersist.Get())));

        foreach (var initialize in persistentEntries)
            initialize.IndexReference.Set(actionSet, initialize.InitializeWith);

        actionSet.AddAction(End());
    }

    record struct PersistentEntry(IndexReference IndexReference, Element InitializeWith);
}