using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Deltin.Deltinteger.Animation
{
    public class GetBlendFile
    {
        public string File { get; }
        readonly Process _process;
        private StreamWriter _biWriter;

        public GetBlendFile(string file)
        {
            File = file;

            var py = @"C:\Users\Deltin\Documents\GitHub\Overwatch-Script-To-Workshop\animation\python\python.exe";
            var pyModule = @"C:\Users\Deltin\Documents\GitHub\Overwatch-Script-To-Workshop\animation\blender_interface.py";

            var info = new ProcessStartInfo {
                FileName = py,
                Arguments = pyModule,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                CreateNoWindow = true
            };
            info.Environment.Add("PYTHONPATH", Path.GetDirectoryName(pyModule));
            _process = new Process { StartInfo = info };
        }

        public BlendFile Start()
        {
            Init();
            _process.StandardInput.WriteLine(File);
            string json = _process.StandardOutput.ReadToEnd().Split(new string[] { "\r\n" }, System.StringSplitOptions.RemoveEmptyEntries)[0];
            return BlendFile.FromJson(json);
        }

        void Init()
        {
            _process.Start();
            _biWriter = _process.StandardInput;
        }
    }
}