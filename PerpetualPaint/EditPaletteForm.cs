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
		private bool editedSinceSave;
		private History history;

		public string FullFilename {
			get {
				return fullFilename;
			}
		}

		public EditPaletteForm(string fullFilename)
		{
			this.fullFilename = fullFilename;
			this.colorPalette = FormatACO.Load(fullFilename);
			this.editedSinceSave = false;

			this.Width = 500;
			this.Height = 500;
			this.Text = "Edit Palette";
			this.FormClosing += new FormClosingEventHandler(Form_OnClosing);

			Init();
			InitHistory();
		}

		private void Init()
		{
			int margin = 10;

			ToolStrip toolStrip = new ToolStrip();
			toolStrip.Dock = DockStyle.Top;
			toolStrip.Items.Add("Undo", Image.FromFile("resources/icons/icon_undo.png"), Form_OnUndo);
			toolStrip.Items.Add("Redo", Image.FromFile("resources/icons/icon_redo.png"), Form_OnRedo);
			this.Controls.Add(toolStrip);

			Button addButton = new Button();
			LayoutHelper.Bottom(this, margin).Left(this, margin).Height(25).Width(80).Apply(addButton);
			addButton.Text = "Add";
			addButton.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
			addButton.Click += new EventHandler(Form_OnAdd);
			this.Controls.Add(addButton);

			Button okButton = new Button();
			LayoutHelper.Bottom(this, margin).Right(this, margin).Height(25).Width(80).Apply(okButton);
			okButton.Text = "Done";
			okButton.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
			okButton.Click += new EventHandler(Form_OnDone);
			this.Controls.Add(okButton);

			Button saveAsButton = new Button();
			LayoutHelper.Bottom(this, margin).LeftOf(okButton, margin * 3).Height(25).Width(80).Apply(saveAsButton);
			saveAsButton.Text = "Save As";
			saveAsButton.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
			saveAsButton.Click += new EventHandler(Form_OnSaveAs);
			this.Controls.Add(saveAsButton);

			Button saveButton = new Button();
			LayoutHelper.Bottom(this, margin).LeftOf(saveAsButton, margin).Height(25).Width(80).Apply(saveButton);
			saveButton.Text = "Save";
			saveButton.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
			saveButton.Click += new EventHandler(Form_OnSave);
			this.Controls.Add(saveButton);

			ContextMenu colorContextMenu = new ContextMenu();
			colorContextMenu.MenuItems.Add("Edit", Color_OnEdit);
			colorContextMenu.MenuItems.Add("Add New Based on This", Color_OnAddBasedOn);
			colorContextMenu.MenuItems.Add("Delete", Color_OnDelete);

			swatchPanel = new SwatchPanel(colorPalette, null, colorContextMenu);
			LayoutHelper.Left(this, margin).Below(toolStrip).Right(this, margin).Above(addButton, margin).Apply(swatchPanel);
			swatchPanel.Anchor = LayoutHelper.AnchorAll;
			this.Controls.Add(swatchPanel);
		}

		private void InitHistory()
		{
			history = new History();
			AddPaletteColorAction.DoFunc = new AddPaletteColorAction.DoOperation(AddColorToPalette);
			AddPaletteColorAction.UndoFunc = new AddPaletteColorAction.UndoOperation(RemoveColorFromPalette);
			RemovePaletteColorAction.DoFunc = new RemovePaletteColorAction.DoOperation(RemoveColorFromPalette);
			RemovePaletteColorAction.UndoFunc = new RemovePaletteColorAction.UndoOperation(AddColorToPalette);
			ReplacePaletteColorAction.DoUndoFunc = new ReplacePaletteColorAction.Operation(ReplaceColorInPalette);
		}

		#region Event Handlers

		private void Form_OnUndo(object sender, EventArgs e)
		{
			history.Undo();
		}

		private void Form_OnRedo(object sender, EventArgs e)
		{
			history.Redo();
		}

		private void Form_OnClosing(object sender, FormClosingEventArgs e)
		{
			if(editedSinceSave)
			{
				DialogResult result = MessageBox.Show("Discard changes to palette since last save?", "Discard Changes", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
				if(result == DialogResult.Cancel)
				{
					e.Cancel = true; //don't close form after all
					return;
				}
			}
			this.DialogResult = DialogResult.OK;
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
					int index = colorPalette.Count;
					AddColorToPalette(newColor, index);
					history.Add(new AddPaletteColorAction(newColor, index));
				}
			}
		}

		private void Form_OnSaveAs(object sender, EventArgs e)
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog();
			saveFileDialog.Filter = "Palette Files|*.ACO";
			saveFileDialog.Title = "Save as Palette File";

			if(saveFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
			{
				return;
			}
			Save(saveFileDialog.FileName);
			fullFilename = saveFileDialog.FileName; //when save is successful, keep new filename
		}

		private void Form_OnSave(object sender, EventArgs e)
		{
			Save(fullFilename);
		}

		private void Form_OnDone(object sender, EventArgs e)
		{
			this.Close();
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
					ReplaceColorInPalette(control.TabIndex, newColor);
					history.Add(new ReplacePaletteColorAction(control.TabIndex, oldColor, newColor));
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
					int index = colorPalette.Count;
					AddColorToPalette(newColor, index);
					history.Add(new AddPaletteColorAction(newColor, index));
				}
			}
		}

		private void Color_OnDelete(object sender, EventArgs e)
		{
			Control control = (sender as MenuItem).GetContextMenu().SourceControl;
			RemoveColorFromPalette(control.TabIndex);
			history.Add(new RemovePaletteColorAction(control.BackColor, control.TabIndex));
		}

#endregion

		private void Save(string fullFilename)
		{
			FormatACO.Save(fullFilename, colorPalette);
			editedSinceSave = false;
		}

		private void AddColorToPalette(Color color, int index)
		{
			colorPalette.Insert(color, index);
			swatchPanel.DisplayColors(colorPalette);
			editedSinceSave = true;
		}

		private void RemoveColorFromPalette(int index)
		{
			colorPalette.RemoveAt(index);
			swatchPanel.DisplayColors(colorPalette);
			editedSinceSave = true;
		}

		private void ReplaceColorInPalette(int index, Color newColor)
		{
			colorPalette.ReplaceAt(index, newColor);
			swatchPanel.DisplayColors(colorPalette);
			editedSinceSave = true;
		}

		private void ResetHistory()
		{
			history.Clear();
		}

	}
}
