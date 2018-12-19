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
	public class ProjectOptionsDialog : Form
	{
		public PPPConfig.PaletteOptions PaletteOption {
			get {
				foreach(PPPConfig.PaletteOptions option in paletteOptionRadioButtons.Keys)
				{
					if(paletteOptionRadioButtons[option].Checked)
						return option;
				}
				return PPPConfig.PaletteOptions.SaveNothing;
			}
		}

		private PPPConfig.PaletteOptions paletteOption { get; set; }

		private Dictionary<PPPConfig.PaletteOptions, RadioButton> paletteOptionRadioButtons = new Dictionary<PPPConfig.PaletteOptions, RadioButton>();

		public ProjectOptionsDialog(PPPConfig config)
		{
			paletteOption = config.PaletteOption;
			InitForm();
		}

		private void InitForm()
		{
			this.Text = "Project Options";
			this.Width = 310;
			this.Height = 150;
			this.Icon = SystemIcons.Application;
			this.MinimizeBox = false;
			this.MaximizeBox = false;
			this.FormBorderStyle = FormBorderStyle.FixedSingle; //don't allow resize
			this.Shown += new EventHandler(Form_OnShown);

			InitControls();
		}

		private void InitControls()
		{
			int margin = 10;
			int radioHeight = 20;
			int buttonHeight = 20;
			int buttonWidth = 50;

			RadioButton radio1 = new RadioButton();
			radio1.Text = "Do not save any palette information with the project.";
			paletteOptionRadioButtons[PPPConfig.PaletteOptions.SaveNothing] = radio1;

			RadioButton radio2 = new RadioButton();
			radio2.Text = "Save filename of the palette with the project.";
			paletteOptionRadioButtons[PPPConfig.PaletteOptions.SaveFileName] = radio2;

			RadioButton radio3 = new RadioButton();
			radio3.Text = "Save entire palette file with the project.";
			paletteOptionRadioButtons[PPPConfig.PaletteOptions.SaveFile] = radio3;

			LayoutHelper.Top(this, margin).Left(this, margin).Right(this, margin).Height(radioHeight).Apply(radio1);
			this.Controls.Add(radio1);

			LayoutHelper.Below(radio1).MatchLeft(radio1).MatchRight(radio1).Height(radioHeight).Apply(radio2);
			this.Controls.Add(radio2);

			LayoutHelper.Below(radio2).MatchLeft(radio1).MatchRight(radio1).Height(radioHeight).Apply(radio3);
			this.Controls.Add(radio3);

			paletteOptionRadioButtons[paletteOption].Checked = true;

			Button okButton = new Button();
			okButton.Text = "Ok";
			LayoutHelper.Below(radio3, margin).Left(this, margin).Width(buttonWidth).Height(buttonHeight).Apply(okButton);
			okButton.Click += new EventHandler(OkButton_OnClick);
			this.Controls.Add(okButton);

			Button cancelButton = new Button();
			cancelButton.Text = "Cancel";
			LayoutHelper.MatchTop(okButton).Right(this, margin).Width(buttonWidth).Height(buttonHeight).Apply(cancelButton);
			cancelButton.Click += new EventHandler(CancelButton_OnClick);
			this.Controls.Add(cancelButton);
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
