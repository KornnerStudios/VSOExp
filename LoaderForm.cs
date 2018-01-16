using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VSOExp
{
	public partial class LoaderForm : Form
	{
		public LoaderForm()
		{
			InitializeComponent();
		}

		private async void hwLoadPrepro_Click(object sender, EventArgs e)
		{
			if (hwOpenFilePrepro.ShowDialog(this) == DialogResult.OK)
			{
				hwProgress.Visible = true;

				var output = await Task.Factory.StartNew(() =>
				{
					var ppOutput = new ParsedPreprocessorOutput();
					bool success = ppOutput.ParseFromFile(hwOpenFilePrepro.FileName);
					return success ? ppOutput : null;
				});

				if (output == null)
				{
					MessageBox.Show(this, "Failed to parse: " + hwOpenFilePrepro.FileName, "Parse Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}

				hwProgress.Visible = false;

				var ppForm = new PreproForm(output);
				ppForm.Text = hwOpenFilePrepro.FileName;
				ppForm.Show(this);
			}
		}

		private async void hwLoadLayout_Click(object sender, EventArgs e)
		{
			if (hwOpenFileLayout.ShowDialog(this) == DialogResult.OK)
			{
				hwProgress.Visible = true;

				// load in the background as these files can be enormous and the parse can take a while
				var output = await Task.Factory.StartNew(() =>
				{
					var layoutLoader = new LayoutLoader();
					bool success = layoutLoader.ParseFromFile(hwOpenFileLayout.FileName);
					return success ? layoutLoader.Result : null;
				});

				if (output == null)
				{
					MessageBox.Show(this, "Failed to parse: " + hwOpenFileLayout.FileName, "Parse Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}

				hwProgress.Visible = false;

				var loForm = new LayoutForm(output);
				loForm.Text = hwOpenFileLayout.FileName;
				loForm.Show(this);
			}
		}
	}
}
