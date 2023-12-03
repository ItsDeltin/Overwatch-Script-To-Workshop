namespace Deltin.Deltinteger.Decompiler;

using System;
using Deltin.Deltinteger.Decompiler.ElementToCode;
using TextToElement;

static class Decompiler
{
    public static DecompileResult DecompileWorkshop(string code)
    {
        try
        {
            var tte = new ConvertTextToElement(code);
            var workshop = tte.Get();
            var workshopToCode = new WorkshopDecompiler(workshop, new OmitLobbySettingsResolver(), new CodeFormattingOptions());
            var ostw = workshopToCode.Decompile();

            return new DecompileResult(tte, ostw);
        }
        catch (Exception ex)
        {
            return new DecompileResult(ex);
        }
    }
}