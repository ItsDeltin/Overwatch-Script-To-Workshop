using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Deltin.Deltinteger.Parse
{
    public class ImportedFile
    {
        public Uri Uri { get; }
        public string Content { get; private set; }
        private byte[] Hash { get; set; }

        public ImportedFile(Uri uri)
        {
            Uri = uri;
            
            using (FileStream stream = GetStream())
            {
                // Get the file hash and content.
                Hash = GetFileHash(stream);
                stream.Position = 0;
                GetContent(stream);
                OnUpdate();
            }
        }

        private FileStream GetStream() => new FileStream(Uri.FilePath(), FileMode.Open, FileAccess.Read);

        private byte[] GetFileHash(FileStream stream)
        {
            HashAlgorithm sha1 = HashAlgorithm.Create("SHA1");
            return sha1.ComputeHash(stream);
        }

        private void GetContent(FileStream stream)
        {
            using (StreamReader reader = new StreamReader(stream))
                Content = reader.ReadToEnd();
        }

        public bool Update()
        {
            using (FileStream stream = GetStream())
            {
                byte[] newHash = GetFileHash(stream);
            
                if (!Hash.SequenceEqual(newHash))
                {
                    Hash = newHash;
                    stream.Position = 0;
                    GetContent(stream);
                    OnUpdate();
                    return true;
                }
                else
                    return false;
            }
        }

        protected virtual void OnUpdate() {}
    }

    public class ImportedScript : ImportedFile
    {
        public ScriptParseInfo ScriptParseInfo { get; } = new ScriptParseInfo();

        public ImportedScript(Uri uri) : base(uri) {}

        protected override void OnUpdate()
        {
            ScriptParseInfo.Update(Content);
        }
    }
}