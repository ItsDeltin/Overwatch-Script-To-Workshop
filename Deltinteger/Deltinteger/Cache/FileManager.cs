using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Deltin.Deltinteger.Cache
{
    class FileIdentifier<T> : ICacheIdentifier where T : LoadedFile
    {
        private readonly Uri _uri;
        private readonly Func<Uri, T> _make;

        public FileIdentifier(Uri uri, Func<Uri, T> makeFile)
        {
            _uri = uri;
            _make = makeFile;
        }

        public bool Matches(ICacheIdentifier other) => other is FileIdentifier<T> fileIdentifier && _uri.Compare(fileIdentifier._uri);
        public object GetValue() => _make(_uri);

        public static T FromUri(ICacheWatcher watcher, Uri uri, Func<Uri, T> makeFile) => watcher.Get<T>(new FileIdentifier<T>(uri, makeFile));
        public static T FromFile(ICacheWatcher watcher, string file, Func<Uri, T> makeFile) => FromUri(watcher, new Uri(file), makeFile);
    }

    abstract class LoadedFile
    {
        public Uri Uri { get; }
        private string _content;
        private byte[] _hash;

        protected LoadedFile(Uri uri)
        {
            Uri = uri;

            using (FileStream stream = GetStream())
            {
                // Get the file hash and content.
                _hash = GetFileHash(stream);
                stream.Position = 0;
                GetNewContent(stream);
            }
        }

        private FileStream GetStream() => new FileStream(Uri.LocalPath, FileMode.Open, FileAccess.Read);

        private byte[] GetFileHash(FileStream stream)
        {
            HashAlgorithm sha1 = HashAlgorithm.Create("SHA1");
            return sha1.ComputeHash(stream);
        }

        private void GetNewContent(FileStream stream)
        {
            using (StreamReader reader = new StreamReader(stream))
                _content = reader.ReadToEnd();
            Update();
        }

        public string GetContent()
        {
            using (FileStream stream = GetStream())
            {
                byte[] newHash = GetFileHash(stream);

                if (!_hash.SequenceEqual(newHash))
                {
                    _hash = newHash;
                    stream.Position = 0;
                    GetNewContent(stream);
                }
            }
            return _content;
        }

        protected abstract void Update();
    }
}