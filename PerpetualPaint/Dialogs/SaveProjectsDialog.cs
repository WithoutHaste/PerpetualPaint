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
			this.Width = 400;
			this.Height = 500;
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
			int buttonWidth = 150;

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
			cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			this.Controls.Add(cancelButton);

			Label instructionLabel = new Label();
			instructionLabel.Text = "Some of the projects in this collection have not been saved.\n\nSet the save location for each project.";
			LayoutHelper.Left(this, margin).Right(this, margin).Top(this, margin).Height(40).Apply(instructionLabel);
			instructionLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			this.Controls.Add(instructionLabel);

			Panel scrollPanel = new Panel();
			scrollPanel.AutoScroll = true;
			LayoutHelper.Left(this, margin).Right(this, margin).Below(instructionLabel, margin).Above(okButton, margin).Apply(scrollPanel);
			scrollPanel.Anchor = LayoutHelper.AnchorAll;
			this.Controls.Add(scrollPanel);

			int maxThumbnailWidth = thumbnails.Max(t => t.Width);
			int maxThumbnailHeight = thumbnails.Max(t => t.Height);
			int maxScrollHeight = 0;
			for(int i = 0; i < thumbnails.Length; i++)
			{
				Bitmap thumbnail = thumbnails[i];

				Panel projectPanel = new Panel();

				PictureBox pictureBox = new PictureBox();
				pictureBox.Image = thumbnail;
				pictureBox.SizeMode = PictureBoxSizeMode.CenterImage;
				LayoutHelper.Left(projectPanel, margin).Top(projectPanel, margin).Width(maxThumbnailWidth).Height(thumbnail.Height).Apply(pictureBox);
				pictureBox.Anchor = AnchorStyles.Top | AnchorStyles.Left;
				projectPanel.Controls.Add(pictureBox);

				Button setFileNameButton = new Button();
				setFileNameButton.TabIndex = i;
				setFileNameButton.Text = "Set Save Location";
				LayoutHelper.RightOf(pictureBox, margin).MatchTop(pictureBox).Width(buttonWidth).Height(buttonHeight).Apply(setFileNameButton);
				setFileNameButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;
				setFileNameButton.Click += new EventHandler(SetFileNameButton_OnClick);
				projectPanel.Controls.Add(setFileNameButton);

				Label fileNameLabel = new Label();
				fileNameLabel.TabIndex = i;
				fileNameLabel.TabStop = false;
				fileNameLabel.Text = "<<no file name>>";
				LayoutHelper.Left(projectPanel, margin).Below(pictureBox, margin).MatchWidth(projectPanel).Height(20).Apply(fileNameLabel);
				fileNameLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
				projectPanel.Controls.Add(fileNameLabel);

				LayoutHelper.Left(scrollPanel).FloatTop(scrollPanel).MatchWidth(scrollPanel).Height(fileNameLabel.Top + fileNameLabel.Height).Apply(projectPanel);
				projectPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
				scrollPanel.Controls.Add(projectPanel);
				maxScrollHeight = projectPanel.Top + projectPanel.Height + margin;
			}

			//shorten dialog if it is unnecessarily tall
			int scrollPanelBottom = scrollPanel.Top + maxScrollHeight;
			if(scrollPanelBottom < okButton.Top - margin)
			{
				this.Height -= (okButton.Top - margin - scrollPanelBottom);
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

		private void SetFileNameButton_OnClick(object sender, EventArgs e)
		{
			Button button = (sender as Button);
			int index = button.TabIndex;

			SaveFileDialog saveFileDialog = new SaveFileDialog();
			saveFileDialog.Filter = "Project Files|*" + PPProject.PROJECT_EXTENSION;
			saveFileDialog.Title = "Save Project As";
			if(saveFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
			{
				return;
			}

			FileNames[index] = saveFileDialog.FileName;
			Label label = GetFileNameLabel(button.Parent);
			label.Text = FileNames[index];
		}

		private Label GetFileNameLabel(Control parent)
		{
			foreach(Control control in parent.Controls)
			{
				if(control is Label)
					return (control as Label);
			}
			return null;
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
