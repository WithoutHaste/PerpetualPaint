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
	public class WaitForm : Form
	{
		private Button cancelButton;

		public WaitForm(string message)
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

			InitControls(message);
		}

		private void InitControls(string text)
		{
			int margin = 10;

			cancelButton = new Button();
			cancelButton.Text = "Cancel Operation";
			LayoutHelper.Bottom(this, margin).CenterWidth(this, 50).Height(20).Apply(cancelButton);
			cancelButton.Click += new EventHandler(CancelButton_OnClick);

			Label label = new Label();
			LayoutHelper.Above(cancelButton, margin).Left(this, margin).Top(this, margin).Right(this, margin).Apply(label);
			label.Text = text;

			this.Controls.Add(label);
			this.Controls.Add(cancelButton);
		}

		private void CancelButton_OnClick(object sender, EventArgs e)
		{
			//todo
			this.Close();
		}
	}
}
