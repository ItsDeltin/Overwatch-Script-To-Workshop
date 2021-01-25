using System;
using Newtonsoft.Json;
using DocRange = Deltin.Deltinteger.Compiler.DocRange;
using DocPos = Deltin.Deltinteger.Compiler.DocPos;

namespace Deltin.Deltinteger.Decompiler.TextToElement
{
    public class DecompileResult
    {
        [JsonProperty("result")]
        public string Result { get; }

        [JsonProperty("range")]
        public DocRange ErrorRange { get; }

        [JsonProperty("exception")]
        public string Exception { get; }

        [JsonProperty("code")]
        public string Code { get; }

        [JsonProperty("original")]
        public string Original { get; }
        
        public bool Success => Result == "success";

        public DecompileResult(ConvertTextToElement tte, string code)
        {
            Code = code;
            Original = tte.Content;
            if (tte.ReachedEnd)
            {
                Result = "success";
            }
            else
            {
                Result = "incompleted";
                ErrorRange = GetErrorRange(tte);
            }
        }

        public DecompileResult(Exception exception)
        {
            Result = "exception";
            Exception = exception.ToString();
        }

        public DocRange GetErrorRange(ConvertTextToElement tte)
        {
            string localStream = tte.LocalStream;
            int endLine = tte.Line, endCharacter = tte.Character;
            
            for (int i = 0; i < localStream.Length; i++)
                // Get the range up until the next whitespace.
                if (char.IsWhiteSpace(localStream[i]) || char.IsSymbol(localStream[i]))
                    break;
                // When a newline is enountered, increment the end line and reset the end character.
                else if (localStream[i] == '\n')
                {
                    endLine++;
                    endCharacter = 0;
                }
                // Otherwise, increment the end character.
                else
                    endCharacter++;

            return new DocPos(tte.Line, tte.Character) + new DocPos(endLine, endCharacter);
        }
    }
}