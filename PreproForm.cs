using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Aga.Controls.Tree;

namespace VSOExp
{
	public partial class PreproForm : Form
	{
		ParsedPreprocessorOutput PreproOutput;

		private TreeModel _model = new TreeModel();

		public PreproForm(ParsedPreprocessorOutput ppOutput)
		{
			PreproOutput = ppOutput;

			InitializeComponent();

			BuildModelFromOutput();

			hwPreproTree.Model = _model;
			hwPreproTree.KeepNodesExpanded = true;
		}

		void BuildModelFromOutput()
		{
			_model.Nodes.Clear();

			_model.Root.Text = PreproOutput.RootNode.Path;

			foreach (PreproNode ppChildNode in PreproOutput.RootNode.Children)
			{
				ConvertPPNodeToTreeNode(ppChildNode, _model.Root);
			}

			hwStats.Text = string.Format("Files found : {0}", PreproOutput.NumFiles);
		}

		public TreePath GetPath(PreproTreeNode node)
		{
			var stack = new Stack<PreproTreeNode>();
			while (node != null)
			{
				stack.Push(node);
				node = node.Parent as PreproTreeNode;
			}
			return new TreePath(stack.ToArray());
		}

		void ConvertPPNodeToTreeNode(PreproNode ppNode, Node treeNode)
		{
			if (ppNode.UIVisible)
			{
				var newNode = new PreproTreeNode()
				{
					DataNode = ppNode,
					Text = ppNode.Path,
					Line = ppNode.LineCounter.ToString(),
					LineDelta = ppNode.LineDelta.ToString(),
				};

				treeNode.Nodes.Add(newNode);
				hwPreproTree.Model = _model;

				var poot = hwPreproTree.FindNode(GetPath(newNode));
				poot.IsExpanded = ppNode.UIExpanded;

				foreach (PreproNode ppChildNode in ppNode.Children)
				{
					ConvertPPNodeToTreeNode(ppChildNode, newNode);
				}
			}
		}

		private void hwPreproTree_SelectionChanged(object sender, EventArgs e)
		{
			var NodeSelection = hwPreproTree.SelectedNode;
			if (NodeSelection != null)
			{
				hwFilePathSel.Text = (NodeSelection.Tag as PreproTreeNode).Text;
			}
			else
			{
				hwFilePathSel.Text = "";
			}
		}

		void UpdateFilter()
		{
			string matchString = hwFilterBox.Text.ToLower();
			bool doMatch = matchString.Length > 0;

			foreach (PreproNode ppChildNode in PreproOutput.LinearNodeList)
			{
				if (!doMatch || ppChildNode.Path.Contains(matchString))
				{
					PreproNode ppNode = ppChildNode;
					while (ppNode != null)
					{
						ppNode.UIVisible = true;
						ppNode = ppNode.Parent;
					}
				}
				else
				{
					ppChildNode.UIVisible = false;
				}
			}

			BuildModelFromOutput();
		}

		private void hwFilterTimer_Tick(object sender, EventArgs e)
		{
			UpdateFilter();
			hwFilterTimer.Stop();
		}

		private void filterBox_TextChanged(object sender, EventArgs e)
		{
			hwFilterTimer.Stop();
			hwFilterTimer.Start();
		}

		private void hwPreproTree_Expanded(object sender, TreeViewAdvEventArgs e)
		{
			PreproTreeNode ppNode = e.Node.Tag as PreproTreeNode;
			if (ppNode != null)
			{
				ppNode.DataNode.UIExpanded = e.Node.IsExpanded;
			}

		}

		private void hwExpand_Click(object sender, EventArgs e)
		{
			foreach (PreproNode ppChildNode in PreproOutput.LinearNodeList)
			{
				if (ppChildNode.UIVisible)
					ppChildNode.UIExpanded = true;
			}
			BuildModelFromOutput();
		}

		private void hwCollapse_Click(object sender, EventArgs e)
		{
			foreach (PreproNode ppChildNode in PreproOutput.LinearNodeList)
			{
				if (ppChildNode.UIVisible)
					ppChildNode.UIExpanded = false;
			}
			BuildModelFromOutput();
		}
	}
}
