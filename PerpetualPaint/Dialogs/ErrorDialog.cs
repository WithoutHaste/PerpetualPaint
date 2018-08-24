using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WithoutHaste.Windows.GUI;

namespace PerpetualPaint
{
	public class ErrorDialog : Form
	{
		public const string LineBreak = "\r\n";

		private Button okButton;

		public ErrorDialog(string title, string message)
		{
			InitForm(title, message);
		}

		public ErrorDialog(string title, string[] message)
		{
			InitForm(title, String.Join(LineBreak, message));
		}

		private void InitForm(string title, string message)
		{
			this.Text = title;
			this.Width = 400;
			this.Height = 300;
			this.Icon = SystemIcons.Error;
			this.MinimizeBox = false;

			this.Shown += new EventHandler(Form_OnShown);

			InitControls(message);
		}

		private void InitControls(string text)
		{
			int margin = 10;

			okButton = new Button();
			okButton.Text = "OK";
			okButton.Dock = DockStyle.Bottom;
			okButton.Click += new EventHandler(OkButton_OnClick);
			this.Controls.Add(okButton);

			TextBox textBox = new TextBox();
			LayoutHelper.Above(okButton, margin).Left(this, margin).Top(this, margin).Right(this, margin).Apply(textBox);
			textBox.Anchor = LayoutHelper.AnchorAll;
			textBox.Multiline = true;
			textBox.WordWrap = true;
			textBox.ReadOnly = true;
			textBox.ScrollBars = ScrollBars.Vertical;
			textBox.Text = text;
			this.Controls.Add(textBox);
		}

		private void Form_OnShown(object sender, EventArgs e)
		{
			okButton.Focus();
		}

		private void OkButton_OnClick(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.OK;
			this.Close();
		}
	}
}
