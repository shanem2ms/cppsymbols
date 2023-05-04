using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents.DocumentStructures;
using System.Threading;
using System.Text.Unicode;
using System.Windows.Media.Animation;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using System.Runtime.InteropServices.Marshalling;

namespace cppsymview
{
    public class CPPEngineFile
    {
        string srcDir = string.Empty;
        string osyDir = string.Empty;

        OSYFile curFile;

        public class Node
        {
            public Node parent;
            public OSYFile.DbNode dbNode;
            public List<Node> children = new List<Node>();
        }

        public Node []nodesArray;

        public void Init(string sourcedir, string osydir)
        {
            srcDir = sourcedir;
            osyDir = osydir;
        }

        public void CompileFile(string filename)
        {
            string osypath = filename.Replace(srcDir, osyDir);
            osypath = osypath + ".osy";
            if (File.Exists(osypath))
            {
                curFile = new OSYFile(osypath);

                nodesArray = new Node[curFile.Nodes.Count];
                for (int i = 0; i < curFile.Nodes.Count; i++)
                {
                    nodesArray[i] = new Node();
                }
                foreach (OSYFile.DbNode node in curFile.Nodes)
                {
                    nodesArray[node.key].parent = nodesArray[node.parentNodeIdx];
                    nodesArray[node.key].dbNode = node;
                }
            }
        }

    }
}
