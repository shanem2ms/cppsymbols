﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace symlib.script
{
    public static class Utils
    {		
	
		public static string GetCanonicalName(Node n)
        {
        	string parent = "";
        	if (n.Parent != null)
        	{
        		parent = GetCanonicalName(n.Parent);
        	}
        	
        	if (n.Kind == CXCursorKind.ClassDecl ||
        		n.Kind == CXCursorKind.StructDecl ||
        		n.Kind == CXCursorKind.Namespace)
        	{
        		parent = parent.Length > 0 ? parent + "::" + n.Token.Text : n.Token.Text;
        	}
        	
        	return parent;
        }
                 
		public static void GetCanonicalNodes(Node n, List<Node> nodes)
        {
        	if (n.Parent != null)
        	{
        		GetCanonicalNodes(n.Parent, nodes);
        	}
        	
        	if (n.Kind == CXCursorKind.ClassDecl ||
        		n.Kind == CXCursorKind.Namespace)
        	{
        		nodes.Add(n);
        	}
        }
        
        public static bool HasParentOfTypeAndName(Node n, CXCursorKind cursorKind, string name)
        {
        	Node curNode = n;
        	while (curNode != null)
        	{
        		if (curNode.Kind == cursorKind && curNode.Token.Text == name)
        			return true;
        		curNode = curNode.Parent;
        	}
        	return false;
        }
        
        static HashSet<string> functionMap = new HashSet<string>();
        public static string GetCFuncName(string classname, string cppname)
        {
        	string cfunc = cppname;
        	if (cppname.StartsWith("operator"))
        	{
        		if (cppname[8] == '[')
        			cfunc = "oparray";
        		else if (cppname[8] == '<')
					cfunc = "oplt";        		
        		else if (cppname[8] == '>')
					cfunc = "opgt";        		
        		else if (cppname[8] == '=' &&
        			cppname[9] == '=')
					cfunc = "opeq";        		
        		else if (cppname[8] == '!' &&
        			cppname[9] == '=')
					cfunc = "opneq";        		
        	}
        	string orig = cfunc;
        	int nextIdx = 0;
        	while (functionMap.Contains(classname + cfunc))
        	{
        		cfunc = orig + nextIdx++;
        	}
        	functionMap.Add(classname + cfunc);
			return cfunc;
        }
    }
}
