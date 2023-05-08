using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static cppsymview.ClangTypes;
using System.Diagnostics;

namespace cppsymview
{
    public class Token
    {
        public ulong Key { get; set; }
        public string Text { get; set; } = string.Empty;
    }
    public class OSYFile
    {
        public class DbNode
        {
            public long key;
            public long compilingFile;
            public long parentNodeIdx;
            public long referencedIdx;
            public CXCursorKind kind;
            public CXTypeKind typeKind;
            public long token;
            public long typetoken;
            public uint line;
            public uint column;
            public uint startOffset;
            public uint endOffset;
            public long sourceFile;
        };

        string []filenames;
        string[] filenamesLwr;
        Token []tokens;
        DbNode []nodes;

        public string []Filenames => filenames;
        public string[] FilenamesLower => filenamesLwr;
        public Token []Tokens => tokens;
        public DbNode []Nodes => nodes;

        public OSYFile(string filename)
        {
            ParseOsyFile(filename);
        }

        ulong ReadUint64(MemoryStream stream)
        {
            byte[] bytes = new byte[8];
            stream.Read(bytes, 0, bytes.Length);
            return BitConverter.ToUInt64(bytes);
        }
        long ReadInt64(MemoryStream stream)
        {
            byte[] bytes = new byte[8];
            stream.Read(bytes, 0, bytes.Length);
            return BitConverter.ToInt64(bytes);
        }

        int ReadInt32(MemoryStream stream)
        {
            byte[] bytes = new byte[4];
            stream.Read(bytes, 0, bytes.Length);
            return BitConverter.ToInt32(bytes);
        }
        uint ReadUInt32(MemoryStream stream)
        {
            byte[] bytes = new byte[4];
            stream.Read(bytes, 0, bytes.Length);
            return BitConverter.ToUInt32(bytes);
        }
        ushort ReadUint16(MemoryStream stream)
        {
            byte[] bytes = new byte[2];
            stream.Read(bytes, 0, bytes.Length);
            return BitConverter.ToUInt16(bytes);
        }

        string ReadString(MemoryStream stream)
        {
            ushort strLength = ReadUint16(stream);
            byte[] bytes = new byte[strLength];
            stream.Read(bytes, 0, bytes.Length);
            return Encoding.UTF8.GetString(bytes);
        }

        Token ReadToken(MemoryStream stream)
        {
            Token t = new Token();
            t.Key = ReadUint64(stream);
            t.Text = ReadString(stream);
            return t;
        }
        DbNode ReadNode(MemoryStream stream)
        {
            DbNode n = new DbNode();
            n.key = ReadInt64(stream);
            n.compilingFile = ReadInt64(stream);
            n.parentNodeIdx = ReadInt64(stream);
            n.referencedIdx = ReadInt64(stream);
            n.kind = (CXCursorKind)ReadInt32(stream);
            n.typeKind = (CXTypeKind)ReadInt32(stream);
            n.token = ReadInt64(stream);
            n.typetoken = ReadInt64(stream);
            n.line = ReadUInt32(stream);
            n.column = ReadUInt32(stream);
            n.startOffset = ReadUInt32(stream);
            n.endOffset = ReadUInt32(stream);
            n.sourceFile = ReadInt64(stream);
            return n;
        }
        T ReadItem<T>(MemoryStream stream) where T : class
        {
            if (typeof(T) == typeof(string))
            {
                return ReadString(stream) as T;
            }
            else if (typeof(T) == typeof(Token))
            {
                return ReadToken(stream) as T;
            }
            else if (typeof(T) == typeof(DbNode))
            {
                return ReadNode(stream) as T;
            }

            return null;
        }
        T[] ReadList<T>(MemoryStream stream) where T : class
        {
            ulong vecLength = ReadUint64(stream);
            T[] items = new T[vecLength];
            for (ulong idx = 0; idx < vecLength; ++idx)
            {
                T val = ReadItem<T>(stream);
                items[idx] = val;
            }

            return items;
        }

        void ParseOsyFile(string osyPath)
        {
            byte[] decompressed;
            using (FileStream fs = new FileStream(osyPath, FileMode.Open, FileAccess.Read))
            {
                byte[] uncompressedSizeBytes = new byte[4];
                int read = fs.Read(uncompressedSizeBytes, 0, 4); //discard 2 bytes
                uint uncompressedSize = BitConverter.ToUInt32(uncompressedSizeBytes);
                decompressed = new byte[uncompressedSize];
                int outputSize;
                ZLibStream compressed_file = new ZLibStream(fs, CompressionMode.Decompress);
                int offset = 0;
                do
                {
                    outputSize = compressed_file.Read(decompressed, offset, (int)uncompressedSize - offset);
                    offset += outputSize;
                } while (outputSize != 0);
            }

            MemoryStream stream = new MemoryStream(decompressed, false);
            filenames = ReadList<string>(stream);
            filenamesLwr = filenames.Select(f => f.ToLower()).ToArray();
            tokens = ReadList<Token>(stream);
            nodes = ReadList<DbNode>(stream);           
        }
    }
}
