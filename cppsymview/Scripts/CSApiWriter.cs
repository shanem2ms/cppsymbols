using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace cppsymview.script
{
	class CSApiWriter
	{
		string outfile;
		List<string> fileLines = new List<string>();
					string header = @"using System;

namespace flashnet
{
";

		string footer = @"}";
		public CSApiWriter(string _outfile)
		{
			outfile = _outfile;
			fileLines.Add(header);
		}

		public void AddClass(Node classNode)
		{
			List<Node> nodes = new List<Node>();
			Utils.GetCanonicalNodes(classNode, nodes);
			string line = "";
			if (nodes[0].Kind == CXCursorKind.Namespace &&
				nodes[0].Token.Text == "sam")
			{
				nodes.RemoveAt(0);
			}
			foreach (var node in nodes)
			{
				if (node.Kind == CXCursorKind.Namespace)
					line += $"[{node.Token.Text}]";
				else
					line += node.Token.Text + "::";
			}
		}
		
		public void AddFunction(Function f)
		{
		}
		
		public void Write()
		{
			fileLines.Add(footer);
			File.WriteAllLines(outfile, fileLines);
		}		
	}
}