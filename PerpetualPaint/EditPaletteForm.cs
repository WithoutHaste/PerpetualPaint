using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WithoutHaste.Drawing.Colors;
using WithoutHaste.Windows.GUI;

namespace PerpetualPaint
{
	public class EditPaletteForm : Form
	{
		private string fullFilename;
		private ColorPalette colorPalette;

		private SwatchPanel swatchPanel;

		public EditPaletteForm(string fullFilename)
		{
			this.fullFilename = fullFilename;
			colorPalette = FormatACO.Load(fullFilename);

			this.Width = 500;
			this.Height = 500;
			this.Text = "Edit Palette";

			int margin = 10;

			Button addButton = new Button();
			LayoutHelper.Bottom(this, margin).Left(this, margin).Height(25).Width(80).Apply(addButton);
			addButton.Text = "Add";
			addButton.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
			addButton.Click += new EventHandler(Form_OnAdd);
			this.Controls.Add(addButton);

			ContextMenu colorContextMenu = new ContextMenu();
			colorContextMenu.MenuItems.Add("Edit", Color_OnEdit);
			colorContextMenu.MenuItems.Add("Add New Based on This", Color_OnAddBasedOn);
			colorContextMenu.MenuItems.Add("Delete", Color_OnDelete);

			swatchPanel = new SwatchPanel(colorPalette, null, colorContextMenu);
			LayoutHelper.Left(this, margin).Top(this, margin).Right(this, margin).Above(addButton, margin).Apply(swatchPanel);
			swatchPanel.Anchor = LayoutHelper.AnchorAll;
			this.Controls.Add(swatchPanel);
		}

		private void Form_OnAdd(object sender, EventArgs e)
		{
			using(NewColorDialog dialog = new NewColorDialog())
			{
				dialog.StartPosition = FormStartPosition.Manual;
				dialog.Location = new Point(this.Location.X + 30, this.Location.Y + 30);
				if(dialog.ShowDialog() == DialogResult.OK)
				{
					Color newColor = dialog.Color;
					colorPalette.Add(newColor);
					swatchPanel.DisplayColors(colorPalette);
				}
			}
		}

		private void Color_OnEdit(object sender, EventArgs e)
		{
			Control control = (sender as MenuItem).GetContextMenu().SourceControl;
			Color oldColor = control.BackColor;
			using(NewColorDialog dialog = new NewColorDialog(oldColor))
			{
				dialog.StartPosition = FormStartPosition.Manual;
				dialog.Location = new Point(this.Location.X + 30, this.Location.Y + 30);
				if(dialog.ShowDialog() == DialogResult.OK)
				{
					Color newColor = dialog.Color;
					colorPalette.Replace(oldColor, newColor);
					swatchPanel.DisplayColors(colorPalette);
				}
			}
		}

		private void Color_OnAddBasedOn(object sender, EventArgs e)
		{
			Control control = (sender as MenuItem).GetContextMenu().SourceControl;
			Color oldColor = control.BackColor;
			using(NewColorDialog dialog = new NewColorDialog(oldColor))
			{
				dialog.StartPosition = FormStartPosition.Manual;
				dialog.Location = new Point(this.Location.X + 30, this.Location.Y + 30);
				if(dialog.ShowDialog() == DialogResult.OK)
				{
					Color newColor = dialog.Color;
					colorPalette.Add(newColor);
					swatchPanel.DisplayColors(colorPalette);
				}
			}
		}

		private void Color_OnDelete(object sender, EventArgs e)
		{
			Control control = (sender as MenuItem).GetContextMenu().SourceControl;
			colorPalette.Remove(control.BackColor);
			swatchPanel.DisplayColors(colorPalette);
		}
	}
}
