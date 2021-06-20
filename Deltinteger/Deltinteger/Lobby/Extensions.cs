namespace Deltin.Deltinteger.Lobby
{
    class ExtensionInfo
    {
        public static readonly ExtensionInfo[] Extensions = new ExtensionInfo[] {
            new ExtensionInfo("Beam Effects", 2),
            new ExtensionInfo("Beam Sounds", 1),
            new ExtensionInfo("Buff Status Effects", 2),
            new ExtensionInfo("Debuff Status Effects", 2),
            new ExtensionInfo("Buff and Debuff Sounds", 2),
            new ExtensionInfo("Energy Explosion Effects", 4),
            new ExtensionInfo("Kinetic Explosion Effects", 4),
            new ExtensionInfo("Explosion Sounds", 2),
            new ExtensionInfo("Play More Effects", 1),
            new ExtensionInfo("Spawn More Dummy Bots", 2)
        };

        public string Name { get; }
        public int Cost { get; }
        public ExtensionInfo(string name, int cost)
        {
            Name = name;
            Cost = cost;
        }

        public static RootSchema GetSchema()
        {
            RootSchema schema = new RootSchema().InitProperties();
            schema.AdditionalProperties = false;

            foreach (var extension in ExtensionInfo.Extensions)
            {
                schema.Properties.Add(extension.Name, new RootSchema() {
                    Type = SchemaObjectType.Boolean,
                    Default = false,
                    Description = "The '" + extension.Name + "' extension costs " + extension.Cost + " extension points."
                });
            }

            return schema;
        }
    }
}