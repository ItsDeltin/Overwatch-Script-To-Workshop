using System.Collections.Generic;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class ElementVariableDecoder
    {
        public Element Player { get; private set; }
        public bool IsEventPlayer { get; private set; }
        public bool RetrievedVariable { get; private set; }
        public WorkshopVariable WorkshopVariable { get; private set; }
        public List<int> ConstantIndex { get; private set; } = new List<int>();
        public List<Element> Index { get; } = new List<Element>();
        public bool IsGlobal { get; private set; }

        private ElementVariableDecoder(IWorkshopTree value)
        {
            if (value is Element element) Decode(element);
        }
        public static ElementVariableDecoder Decode(IWorkshopTree value) => new ElementVariableDecoder(value);

        void Decode(Element element)
        {
            // Global variable
            if (element is V_GlobalVariable)
            {
                WorkshopVariable = (WorkshopVariable)element.ParameterValues[0];
                RetrievedVariable = true;
                IsGlobal = true;
            }
            // Player variable
            else if (element is V_PlayerVariable)
            {
                Player = (Element)element.ParameterValues[0];
                IsEventPlayer = Player is V_EventPlayer;
                WorkshopVariable = (WorkshopVariable)element.ParameterValues[1];
                RetrievedVariable = true;
                IsGlobal = false;
            }
            // Value in array
            else if (element is V_ValueInArray)
            {
                Decode((Element)element.ParameterValues[0]);

                // Add to the constant index list.
                if (element.ParameterValues[1] is V_Number num)
                    ConstantIndex?.Add((int)num.Value);
                else // Remove constant index list.
                    ConstantIndex = null;
                
                // Add to index list.
                Index.Add((Element)element.ParameterValues[1]);
            }
        }
    }
}