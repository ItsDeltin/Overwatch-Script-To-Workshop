using System;

namespace Deltin.Deltinteger.Parse.Variables.Build
{
    public class VariableTypeHandler
    {
        private readonly VarInfo _varInfo;
        private bool _preset;
        private bool _presetIsGlobal;
        private bool _setType;
        private CodeType _setTypeValue;
        private bool _isWorkshopReference;

        public VariableTypeHandler(VarInfo varInfo)
        {
            _varInfo = varInfo;
        }

        public void SetAttribute(bool isGlobal)
        {
            if (_preset)
                throw new Exception("Preset cannot be set more than once.");

            _preset = true;
            _presetIsGlobal = isGlobal;
        }

        public void SetType(CodeType type)
        {
            if (_setType)
                throw new Exception("Type cannot be set more than once.");
            
            if (type == null)
                return;

            _setType = true;
            _setTypeValue = type;
            _isWorkshopReference = _isWorkshopReference || _setTypeValue.IsConstant();
        }

        public void SetWorkshopReference()
        {
            _isWorkshopReference = true;
        }

        public VariableType GetVariableType()
        {
            if (_isWorkshopReference || (_setType && _setTypeValue.IsConstant()))
                return VariableType.ElementReference;

            else if (_preset)
                return _presetIsGlobal ? VariableType.Global : VariableType.Player;
            
            else
                return VariableType.Dynamic;
        }

        public StoreType GetStoreType()
        {
            // Set the variable and store types.
            if (_isWorkshopReference)
                return StoreType.None;
            // In extended collection.
            else if (_varInfo.InExtendedCollection)
                return StoreType.Indexed;
            // Full workshop variable.
            else
                return StoreType.FullVariable;
        }
    }
}