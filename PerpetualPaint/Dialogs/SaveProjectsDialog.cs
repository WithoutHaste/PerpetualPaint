using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WithoutHaste.Windows.GUI;
using PerpetualPaintLibrary;

namespace PerpetualPaint
{
	/// <summary>
	/// Shows a scrolling list of thumbnails of projects that don't have filenames.
	/// User can set the filename on each in turn using normal file selector dialog.
	/// Buttons at bottom to Save All or Cancel.
	/// </summary>
	public class SaveProjectsDialog : Form
	{
		/// <summary>
		/// File names associated with each bitmap, in the same order as the bitmaps provided.
		/// All file names will have a value.
		/// </summary>
		public string[] FileNames = new string[0];

		private Bitmap[] thumbnails;

		public SaveProjectsDialog(Bitmap[] thumbnails)
		{
			this.thumbnails = thumbnails;
			FileNames = new string[thumbnails.Length];

			InitForm();
		}

		private void InitForm()
		{
			this.Text = "Save Projects";
			this.Width = 500;
			this.Height = 800;
			this.Icon = SystemIcons.Application;
			this.MinimizeBox = false;
			this.MaximizeBox = false;
			this.FormBorderStyle = FormBorderStyle.Sizable; //allow resize
			this.Shown += new EventHandler(Form_OnShown);

			InitControls();
		}

		private void InitControls()
		{
			int margin = 10;
			int buttonHeight = 20;
			int buttonWidth = 50;

			Button okButton = new Button();
			okButton.Text = "Save Projects";
			LayoutHelper.Bottom(this, margin).Left(this, margin).Width(buttonWidth).Height(buttonHeight).Apply(okButton);
			okButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
			okButton.Click += new EventHandler(OkButton_OnClick);
			this.Controls.Add(okButton);

			Button cancelButton = new Button();
			cancelButton.Text = "Cancel";
			LayoutHelper.MatchTop(okButton).Right(this, margin).Width(buttonWidth).Height(buttonHeight).Apply(cancelButton);
			cancelButton.Click += new EventHandler(CancelButton_OnClick);
			okButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			this.Controls.Add(cancelButton);

			Panel scrollPanel = new Panel();
			scrollPanel.AutoScroll = true;
			LayoutHelper.Left(this, margin).Right(this, margin).Top(this, margin).Above(okButton, margin).Apply(scrollPanel);
			scrollPanel.Anchor = LayoutHelper.AnchorAll;
			this.Controls.Add(scrollPanel);

			for(int i = 0; i < thumbnails.Length; i++)
			{
				Bitmap thumbnail = thumbnails[i];
				PictureBox pictureBox = new PictureBox();
				pictureBox.Width = thumbnail.Width;
				pictureBox.Height = thumbnail.Height;
				pictureBox.Image = thumbnail;
				LayoutHelper.Left(scrollPanel, margin).FloatTop(scrollPanel, margin).Apply(pictureBox);
				scrollPanel.Controls.Add(pictureBox);
			}
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
			if(FileNames.Any(f => String.IsNullOrEmpty(f)))
			{
				MessageBox.Show("Fill in all filenames.", "Cannot Save", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		private void CancelButton_OnClick(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}
	}
}
