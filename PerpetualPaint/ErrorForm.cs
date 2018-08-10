using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsExtensions;

namespace PerpetualPaint
{
	/// <summary>
	/// Use "\r\n" for line breaks.
	/// </summary>
	public class ErrorForm : Form
	{
		private Button okButton;

		public ErrorForm(string title, string message)
		{
			InitForm(title, message);
		}

		public ErrorForm(string title, string[] message)
		{
			InitForm(title, String.Join("\r\n", message));
		}

		private void InitForm(string title, string message)
		{
			this.Text = title;
			this.Width = 400;
			this.Height = 300;
			this.Icon = SystemIcons.Error;

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

			TextBox textBox = new TextBox();
			textBox.Location = new Point(margin, margin);
			textBox.Size = LayoutHelper.FillAbove(this, okButton, margin);
			textBox.Anchor = LayoutHelper.AnchorAll;
			textBox.Multiline = true;
			textBox.WordWrap = true;
			textBox.ReadOnly = true;
			textBox.ScrollBars = ScrollBars.Vertical;
			textBox.Text = text;

			this.Controls.Add(textBox);
			this.Controls.Add(okButton);
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
