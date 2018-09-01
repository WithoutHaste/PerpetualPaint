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
	public class WaitDialog : Form
	{
		public WaitDialog(string message)
		{
			InitForm(message);
		}

		private void InitForm(string message)
		{
			this.Text = "Please Wait";
			this.Width = 300;
			this.Height = 150;
			this.Icon = SystemIcons.Application;
			this.MinimizeBox = false;
			this.MaximizeBox = false;
			this.FormBorderStyle = FormBorderStyle.FixedSingle; //don't allow resize
			this.Shown += new EventHandler(Form_OnShown);

			InitControls(message);
		}

		private void InitControls(string text)
		{
			int margin = 10;

			Button okButton = new Button();
			okButton.Text = "Ok";
			LayoutHelper.Bottom(this, margin).CenterWidth(this, 50).Height(20).Apply(okButton);
			okButton.Click += new EventHandler(OkButton_OnClick);

			Label label = new Label();
			LayoutHelper.Above(okButton, margin).Left(this, margin).Top(this, margin).Right(this, margin).Apply(label);
			label.Text = text;

			this.Controls.Add(label);
			this.Controls.Add(okButton);
		}

		private void Form_OnShown(object sender, EventArgs e)
		{
			if(this.Owner != null)
			{
				this.StartPosition = FormStartPosition.Manual;
				LayoutHelper.CenterBothInScreen(this, this.Owner);
			}
		}

		private void OkButton_OnClick(object sender, EventArgs e)
		{
			this.Close();
		}
	}
}
