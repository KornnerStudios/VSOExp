using System;
using System.Collections.Generic;
using System.IO;

namespace VSOExp
{
	public sealed class PreproTreeNode
		: Aga.Controls.Tree.Node
	{
		public PreproNode DataNode;

		private string _line;
		public /*virtual*/ string Line
		{
			get { return _line; }
			set
			{
				if (_line != value)
				{
					_line = value;
					NotifyModel();
				}
			}
		}

		private string _lineDelta;
		public /*virtual*/ string LineDelta
		{
			get { return _lineDelta; }
			set
			{
				if (_lineDelta != value)
				{
					_lineDelta = value;
					NotifyModel();
				}
			}
		}
	};

	public sealed class PreproNode
	{
		public string Path = "";
		public long LineCounter = 1;
		public long LineDelta = 0;

		public int LinearIndex = -1;
		public int Depth = 0;
		public PreproNode Parent = null;
		public List<PreproNode> Children = new List<PreproNode>();

		public bool UIVisible = true;
		public bool UIExpanded = false;

		public PreproNode Add(string path)
		{
			var newNode = new PreproNode();

			newNode.Parent = this;
			newNode.Path = path;

			for (var parentWalk = newNode.Parent; parentWalk != null; parentWalk = parentWalk.Parent)
			{
				newNode.Depth++;
			}

			Children.Add(newNode);

			return newNode;
		}
	};

	public class ParsedPreprocessorOutput
	{
		static readonly char[] kSpaceCharArray = new char[] { ' ' };
		static readonly char[] kSpaceThenQuoteCharArray = new char[] { ' ', '\"' };

		public PreproNode RootNode = new PreproNode();
		public int NumFiles = 0;
		public int TotalLines = 0;

		public List<PreproNode> LinearNodeList = new List<PreproNode>();

		public bool ParseFromFile(string filename)
		{
			PreproNode lNode = RootNode;

			int lastLineNumber = -1;
			string lastLinePath = "";

			var nodePathLookup = new Dictionary<string, Queue<PreproNode>>();

			foreach (string line in File.ReadLines(filename))
			{
				TotalLines++;

				string inputLine = line.Trim().Replace("\\\\", "\\").Replace("\\", "/");
				if (inputLine.Length == 0)
					continue;

				if (inputLine.StartsWith("#line "))
				{
					var lineStringBits = inputLine.Split(kSpaceCharArray, 3);

					int lineLineNum = int.Parse(lineStringBits[1]);

					string lineFilePath = lineStringBits[2].Trim(kSpaceThenQuoteCharArray).ToLowerInvariant();

					if (lineLineNum == 1)
					{
						lNode = lNode.Add(lineFilePath);
						NumFiles++;

						lNode.LinearIndex = LinearNodeList.Count;
						LinearNodeList.Add(lNode);

						lNode.LineCounter = TotalLines;

						if (nodePathLookup.ContainsKey(lineFilePath))
						{
							nodePathLookup[lineFilePath].Enqueue(lNode);
						}
						else
						{
							var lineNewQueue = new Queue<PreproNode>();
							lineNewQueue.Enqueue(lNode);

							nodePathLookup.Add(lineFilePath, lineNewQueue);
						}
					}
					else
					{
						lNode = nodePathLookup[lineFilePath].Peek();
					}

					lastLinePath = lineFilePath;
					lastLineNumber = lineLineNum;
				}
				else
				{
					lastLinePath = "";
					lastLineNumber = -1;
				}
			}

			SumUpDeltas(RootNode);

			return true;
		}

		internal void SumUpDeltas(PreproNode ppNode)
		{
			ppNode.LineDelta = (TotalLines - ppNode.LineCounter);

			foreach (PreproNode ppSearch in LinearNodeList)
			{
				if (ppSearch.Depth == 1)
					ppSearch.UIExpanded = true;

				if (ppSearch.Depth <= ppNode.Depth)
				{
					if (ppSearch.LineCounter > ppNode.LineCounter)
					{
						ppNode.LineDelta = (ppSearch.LineCounter - ppNode.LineCounter);
						break;
					}
				}
			}

			foreach (PreproNode ppChildNode in ppNode.Children)
			{
				SumUpDeltas(ppChildNode);
			}
		}
	};
}
