using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Models;

namespace Deltin.Deltinteger.Csv
{
    public class CsvFrame
    {
        public static CsvFrame[] ParseSet(string value)
        {
            string[] lines = value.Split(
                new[] { Environment.NewLine },
                StringSplitOptions.None
            );

            return ParseSet(lines);
        }

        public static CsvFrame[] ParseSet(string[] lines)
        {
            CsvFrame[] frames = new CsvFrame[lines.Length];
             for (int i = 0; i < lines.Length; i++)
                frames[i] = ParseOne(lines[i]);
            return frames;
        }

        public static CsvFrame ParseOne(string value)
        {
            string[] infoSplit = value.Split(',');
            for (int i = 0; i < infoSplit.Length; i++)
                infoSplit[i] = infoSplit[i].Trim();

            const int expectedLength = 26 + 2; // Every letter of the alphabet plus the time and variable set owner.
            if (infoSplit.Length != expectedLength)
                throw new CsvParseFailedException("Expected " + expectedLength + " nodes, got " + infoSplit.Length + " instead.");
            
            if (!double.TryParse(infoSplit[0], out double time))
                throw new CsvParseFailedException("Failed to get the time.");
            
            string owner = infoSplit[1];

            Variable[] variables = Enum.GetValues(typeof(Variable)).Cast<Variable>().ToArray();
            CsvPart[] variableValues = new CsvPart[variables.Length];
            if (variables.Length != 26) throw new Exception();

            for (int i = 0; i < variables.Length; i++)
            {
                // Element is an array.
                if (infoSplit[i + 2][0] == '{' && infoSplit[i + 2].Last() == '}')
                {
                    // Trim the {}
                    string work = infoSplit[i + 2].Substring(1, infoSplit[i + 2].Length - 2);
                    
                    // Split the array.
                    var splitAt = Regex.Matches(work, @";(?![^(]*\))");
                    string[] arrayElements = new string[splitAt.Count + 1];
                    for (int s = 0; s < splitAt.Count + 1; s++)
                    {
                        int start = 0, end = work.Length;
                        if (s > 0)
                            start = splitAt[s - 1].Index + 1;
                        if (s < splitAt.Count)
                            end = splitAt[s].Index;
                        
                        arrayElements[s] = work.Substring(start, end - start).Trim();
                    }

                    // Get the values
                    CsvPart[] array = new CsvPart[arrayElements.Length];
                    for (int a = 0; a < array.Length; a++)
                        array[a] = ParseValue(arrayElements[a]);
                    
                    variableValues[i] = new CsvArray(array);
                }
                else variableValues[i] = ParseValue(infoSplit[i + 2]);
            }

            // Convert the variables array and the variableValues array to a dictionary.
            var final = new Dictionary<Variable, CsvPart>();
            for (int i = 0; i < variables.Length; i++)
                final.Add(variables[i], variableValues[i]);

            return new CsvFrame(time, owner, final);
        }

        private static CsvPart ParseValue(string value)
        {
            // Element is a vector
            if (value[0] == '(' && value.Last() == ')')
            {
                string work = value.Substring(1, value.Length - 2);
                string[] split = work.Split(';');

                if (split.Length != 3)
                    throw new CsvParseFailedException("Vector length does not equal 3.");

                if (!double.TryParse(split[0], out double x))
                    throw new CsvParseFailedException("Failed to get vector X value.");
                if (!double.TryParse(split[1], out double y))
                    throw new CsvParseFailedException("Failed to get vector Y value.");
                if (!double.TryParse(split[2], out double z))
                    throw new CsvParseFailedException("Failed to get vector Z value.");

                return new CsvVector(new Vertex(x,y,z));
            }
            // Element is a number
            else if (double.TryParse(value, out double number))
            {
                return new CsvNumber(number);
            }
            // Element is a boolean
            else if (bool.TryParse(value, out bool boolean))
            {
                return new CsvBoolean(boolean);
            }
            // Element is unknown
            else return new CsvString(value);
        }

        public double Time { get; }
        public string VariableSetOwner { get; }
        public Dictionary<Variable, CsvPart> VariableValues { get; }

        public CsvFrame(double time, string variableSetOwner, Dictionary<Variable, CsvPart> variableValues)
        {
            Time = time;
            VariableSetOwner = variableSetOwner;
            VariableValues = variableValues;
        }
    }

    public abstract class CsvPart {}

    class CsvArray : CsvPart
    {
        public CsvPart[] Values { get; }

        public CsvArray(CsvPart[] values)
        {
            Values = values;
        }
    }

    class CsvNumber : CsvPart
    {
        public double Value { get; }

        public CsvNumber(double value)
        {
            Value = value;
        }
    }

    class CsvVector : CsvPart
    {
        public Vertex Value { get; }

        public CsvVector(Vertex value)
        {
            Value = value;
        }
    }

    class CsvBoolean : CsvPart
    {
        public bool Value { get; }

        public CsvBoolean(bool value)
        {
            Value = value;
        }
    }

    class CsvString : CsvPart
    {
        public string Value { get; }

        public CsvString(string value)
        {
            Value = value;
        }
    }

    class CsvParseFailedException : Exception
    {
        public CsvParseFailedException(string message) : base(message) {}
    }
}