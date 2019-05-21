using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;
using System.Reflection;

namespace Deltin.Deltinteger
{
    class Config
    {
        private static readonly Log Log = new Log("Config");

        public static Config GetConfig()
        {
            string file = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "config.xml");

            if (!File.Exists(file))
            {
                Log.Write("Config file not found, using defaults.");
                return new Config();
            }

            Config config = new Config();
            var doc = XDocument.Load(file);

            ParseInt(doc, "smallstep", ref config.SmallStep);
            ParseInt(doc, "mediumstep", ref config.MediumStep);
            ParseInt(doc, "bigstep", ref config.BigStep);
            ParseBool(doc, "stopinput", ref config.StopInput);

            return config;
        }

        static void ParseInt(XDocument doc, string name, ref int value)
        {
            if (int.TryParse(doc.Element(name)?.Value, out int set))
                value = set;
        }

        static void ParseBool(XDocument doc, string name, ref bool value)
        {
            if (bool.TryParse(doc.Element(name)?.Value, out bool set))
                value = set;
        }

        public int SmallStep = 35;
        public int MediumStep = 150;
        public int BigStep = 500;
        public bool StopInput = true;
    }
}
