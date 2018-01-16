using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using Aga.Controls.Tree;

namespace VSOExp
{
	public partial class LayoutForm : Form
	{
		LayoutLoaderOutput LayoutOutput;

		private TreeModel _model = new TreeModel();

		public LayoutForm(LayoutLoaderOutput llOutput)
		{
			LayoutOutput = llOutput;

			InitializeComponent();

			BuildModelFromOutput();

			hwObjectTree.Model = _model;
			hwObjectTree.KeepNodesExpanded = true;
		}

		internal void BuildModelFromOutput()
		{
			_model.Nodes.Clear();

			var sortedDict = LayoutOutput.Classes.OrderByDescending(key => key.Value.Size);

			foreach (var nameLayoutPair in sortedDict)
			{
				if (nameLayoutPair.Value.Members.Count > 0 && nameLayoutPair.Value.Size > 0)
				{
					var newNode = new LayoutTreeNode()
					{
						Text = nameLayoutPair.Key,

						Size = nameLayoutPair.Value.Size.ToString(),
						Padding = nameLayoutPair.Value.TotalAlignmentPadding.ToString(),
					};


					_model.Root.Nodes.Add(newNode);

					foreach (var layoutClassMember in nameLayoutPair.Value.Members)
					{
						var memberNode = new LayoutTreeNode()
						{
							Text = layoutClassMember.Name,
							Size = layoutClassMember.Size.ToString(),
						};

						string memberType = layoutClassMember.Type;
						if (memberType != null)
						{
							if (LayoutOutput.Classes.ContainsKey(memberType))
							{
								memberNode.Padding = "(" + LayoutOutput.Classes[memberType].TotalAlignmentPadding + ")";
							}

							memberNode.TypeName = memberType;
						}


						newNode.Nodes.Add(memberNode);
					}
				}
			}
		}
	}
}
