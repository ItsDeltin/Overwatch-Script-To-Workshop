using System;
using System.Net;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using HtmlAgilityPack;

namespace Deltin.Deltinteger.WorkshopWiki
{
    public class Wiki
    {
        public const string URL = "https://us.forums.blizzard.com/en/overwatch/t/wiki-workshop-syntax-script-database/";
        private static Log Log = new Log("Wiki");

        private static Wiki wiki = null;
        public static Wiki GetWiki()
        {
            if (wiki == null)
            {
                try
                {
                    string languageFile = Path.Combine(Program.ExeFolder, "Wiki.xml");
                    XmlSerializer serializer = new XmlSerializer(typeof(Wiki));

                    using (var fileStream = File.OpenRead(languageFile))
                        wiki = (Wiki)serializer.Deserialize(fileStream);
                }
                catch (Exception ex)
                {
                    Log.Write(LogLevel.Normal, "Failed to load Workshop Wiki: ", new ColorMod(ex.Message, ConsoleColor.Red));
                }
            }
            return wiki;
        }

        public static Wiki GenerateWiki()
        {
            try
            {
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.OptionFixNestedTags = true;

                using (var webClient = new WebClient())
                    htmlDoc.Load(webClient.OpenRead(URL), Encoding.UTF8);

                List<WikiMethod> methods = new List<WikiMethod>();

                // Loop through all summaries
                foreach (var summary in htmlDoc.DocumentNode.Descendants("summary"))
                {
                    string name = summary.InnerText.Trim(); // Gets the name

                    // Sometimes methods have "- New!" at the end, be sure to remove it.
                    if (name.EndsWith("- New!"))
                        name = name.Substring(0, name.Length - "- New!".Length).Trim();

                    var details = summary.ParentNode;
                    string description = details.SelectNodes("p").First().InnerText.Trim(); // Gets the description.

                    // Get the parameters.
                    List<WikiParameter> parameters = new List<WikiParameter>();
                    var parameterSummaries = details.SelectSingleNode("ul")?.SelectNodes("li"); // 'ul' being list and 'li' being list element.
                    if (parameterSummaries != null)
                        foreach (var parameterSummary in parameterSummaries)
                        {
                            string[] data = parameterSummary.InnerText.Split(new char[] { '-' }, 2);
                            parameters.Add(new WikiParameter(data[0].Trim(), data.ElementAtOrDefault(1)?.Trim()));
                        }

                    methods.Add(new WikiMethod(name, description, parameters.ToArray()));
                }

                string[] keywords = I18n.GenerateI18n.Keywords();
                for (int i = methods.Count - 1; i >= 0; i--)
                    if (!keywords.Any(keyword => keyword.ToLower() == methods[i].Name.ToLower()))
                        methods.RemoveAt(i);

                return new Wiki(methods.ToArray());
            }
            catch (Exception ex)
            {
                Log.Write(LogLevel.Normal, "Failed to generate the Workshop Wiki: ", new ColorMod(ex.Message, ConsoleColor.Red));
                return null;
            }
        }

        [XmlElement("method")]
        public WikiMethod[] Methods { get; set; }

        public Wiki(WikiMethod[] methods)
        {
            Methods = methods;
        }
        public Wiki() { }

        public WikiMethod GetMethod(string methodName)
        {
            return Methods.FirstOrDefault(m => m.Name.ToLower() == methodName.ToLower());
        }

        public void ToXML(string file)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Wiki));

            using (var fileStream = File.Create(file))
            using (StreamWriter writer = new StreamWriter(fileStream))
                serializer.Serialize(writer, this);
        }
    }

    public class WikiMethod
    {
        [XmlAttribute("name")]
        public string Name { get; set; }
        [XmlAttribute("description")]
        public string Description { get; set; }
        [XmlElement("parameter")]
        public WikiParameter[] Parameters { get; set; }

        public WikiMethod(string name, string description, WikiParameter[] parameters)
        {
            Name = name;
            Description = description;
            Parameters = parameters;
        }
        public WikiMethod() { }

        public override string ToString()
        {
            return Name;
        }

        public WikiParameter GetWikiParameter(string name)
        {
            return Parameters.FirstOrDefault(p => p.Name.ToLower() == name.ToLower());
        }
    }

    public class WikiParameter
    {
        [XmlAttribute("name")]
        public string Name { get; set; }
        [XmlAttribute("description")]
        public string Description { get; set; }

        public WikiParameter(string name, string description)
        {
            Name = name;
            Description = description;
        }
        public WikiParameter() { }

        public override string ToString()
        {
            return Name;
        }
    }
}