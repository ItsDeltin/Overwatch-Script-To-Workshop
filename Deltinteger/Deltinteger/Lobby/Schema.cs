using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using Newtonsoft.Json.Serialization;

namespace Deltin.Deltinteger.Lobby
{
    public class RulesetSchema
    {
        // Generates the schema.
        public static JSchema GenerateSchema()
        {
            JSchemaGenerator generator = new JSchemaGenerator();
            // generator.GenerationProviders.Add(new StringEnumGenerationProvider());
            generator.GenerationProviders.Add(new HeroSchemaProvider());
            generator.DefaultRequired = Required.Default;
            generator.ContractResolver = new HeroContractResolver();

            JSchema schema = generator.Generate(typeof(RulesetSchema));

            string result = schema.ToString();
            Console.WriteLine(result);
            return schema;
        }

        public HeroesRoot Heroes { get; set; }
    }

    public class HeroesRoot
    {
        [JsonProperty("General")]
        public HeroList General { get; set; }

        [JsonProperty("Team 1")]
        public HeroList Team1 { get; set; }

        [JsonProperty("Team 2")]
        public HeroList Team2 { get; set; }
    }

    public class HeroList
    {
        public AnaSettings Ana { get; set; }
    }

    // class HeroProvider : JSchemaGenerationProvider
    // {
    //     public override JSchema GetSchema(JSchemaTypeGenerationContext context)
    //     {
    //         return null;
    //     }
    // }
}