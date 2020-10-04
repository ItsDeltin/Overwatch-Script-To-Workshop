using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Animation;

namespace Deltin.Deltinteger.Parse
{
    public abstract class ImportedFile
    {
        public Uri Uri { get; }
        private byte[] Hash { get; set; }

        public ImportedFile(Uri uri)
        {
            Uri = uri;
            
            using (FileStream stream = GetStream())
            {
                // Get the file hash and content.
                Hash = GetFileHash(stream);
                GetContent(stream);
            }
        }

        private FileStream GetStream() => new FileStream(Uri.FilePath(), FileMode.Open, FileAccess.Read);

        private byte[] GetFileHash(FileStream stream)
        {
            using (HashAlgorithm sha1 = HashAlgorithm.Create("SHA1"))
                return sha1.ComputeHash(stream);
        }

        protected abstract void GetContent(FileStream stream);

        public bool Update()
        {
            using (FileStream stream = GetStream())
            {
                byte[] newHash = GetFileHash(stream);
            
                if (!Hash.SequenceEqual(newHash))
                {
                    Hash = newHash;
                    GetContent(stream);
                    return true;
                }
                else
                    return false;
            }
        }
    }

    public abstract class TextFile : ImportedFile
    {
        public string Content { get; private set; }

        public TextFile(Uri uri) : base(uri) {}

        protected override void GetContent(FileStream stream)
        {
            stream.Position = 0;
            using (StreamReader reader = new StreamReader(stream))
                Content = reader.ReadToEnd();
            OnUpdate();
        }

        protected abstract void OnUpdate();
    }

    public class ImportedScript : TextFile
    {
        public Document Document { get; }

        public ImportedScript(Uri uri) : base(uri) {
            Document = new Document(uri, Content);
            Document.Update(Content);
        }

        protected override void OnUpdate()
        {
            if (Document != null)
                Document.Update(Content);
        }
    }

    public class ImportedBlendFile : ImportedFile
    {
        public BlendFile BlendFile { get; private set; }

        public ImportedBlendFile(Uri uri) : base(uri)
        {
            GetContent(null);
        }

        protected override void GetContent(FileStream stream)
        {
            var blendGetter = new GetBlendFile(Uri.LocalPath.TrimStart('/'));
            BlendFile = blendGetter.Start();
        }
    }
}