using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WithoutHaste.Drawing.Colors;
using WithoutHaste.Windows.GUI;
using PerpetualPaintLibrary;


namespace PerpetualPaint
{
	public class CollectionForm : Form
	{
		/// <summary>Width of vertical scroll bar.</summary>
		public static readonly int SCROLLBAR_WIDTH = System.Windows.Forms.SystemInformation.VerticalScrollBarWidth + 5;

		/// <summary>Maximum width and height of each thumbnail image.</summary>
		public static readonly int THUMBNAIL_SIZE = 100;

		private ToolStrip toolStrip;
		private ToolStripItem saveToolStripItem;
		/// <summary>Layout panel for the images.</summary>
		private FlowLayoutPanel flowPanel;
		private PalettePanel palettePanel;

		/// <summary>The collection being displayed.</summary>
		private PPCollection collection;

		private ContextMenu projectContextMenu;

		private int selectedProjectIndex = -1;

		public WithoutHaste.Drawing.Colors.ColorPalette ColorPalette { get { return palettePanel.ColorPalette; } }

		public event ProjectEventHandler ProjectSelected;

		public CollectionForm(string fileName = null)
		{
			this.Text = "Collection";
			this.WindowState = FormWindowState.Normal;
			this.Width = 500;
			this.Height = 500;
			this.FormClosing += new FormClosingEventHandler(Form_Closing);

			InitMenus();
			InitTools();
			InitControls();

			collection = new PPCollection();
			SetupCollection();

			if(!String.IsNullOrEmpty(fileName))
			{
				OpenCollection(fileName);
			}
		}

		#region Init

		private void InitMenus()
		{
			MenuItem fileMenu = new MenuItem("File");
			MenuItem newCollection = new MenuItem("New Collection", new EventHandler(Collection_OnNew));
			MenuItem openCollection = new MenuItem("Open Collection", new EventHandler(Collection_OnOpen));
			MenuItem addProject = new MenuItem("Add Image/Project", new EventHandler(Collection_OnAddProject));
			MenuItem saveCollection = new MenuItem("Save Collection", new EventHandler(Colleciton_OnSave), Shortcut.CtrlS);
			MenuItem saveAsCollection = new MenuItem("Save Collection As", new EventHandler(Collection_OnSaveAs), Shortcut.F12);
			fileMenu.MenuItems.Add(newCollection);
			fileMenu.MenuItems.Add(openCollection);
			fileMenu.MenuItems.Add(addProject);
			fileMenu.MenuItems.Add(saveCollection);
			fileMenu.MenuItems.Add(saveAsCollection);

			MenuItem paletteMenu = new MenuItem("Palette");
			paletteMenu.MenuItems.Add("New", new EventHandler(Form_OnNewPalette));
			paletteMenu.MenuItems.Add("Load", new EventHandler(Form_OnLoadPalette));
			paletteMenu.MenuItems.Add("Edit", new EventHandler(Form_OnEditPalette));

			this.Menu = new MainMenu();
			this.Menu.MenuItems.Add(fileMenu);
			this.Menu.MenuItems.Add(paletteMenu);

			projectContextMenu = new ContextMenu();
			projectContextMenu.MenuItems.Add("Remove", Project_OnRemove);
		}

		private void InitTools()
		{
			toolStrip = new ToolStrip();
			toolStrip.Dock = DockStyle.Top;
			toolStrip.Items.Add("New", IconManager.NEW_FOLDER, Collection_OnNew);
			toolStrip.Items.Add("Open", IconManager.OPEN_FILE, Collection_OnOpen);
			saveToolStripItem = toolStrip.Items.Add("Save", IconManager.SAVE, Colleciton_OnSave);
			toolStrip.Items.Add(new ToolStripSeparator());
			toolStrip.Items.Add("Add", IconManager.ADD, Collection_OnAddProject);

			this.Controls.Add(toolStrip);
		}

		private void InitControls()
		{
			palettePanel = new PalettePanel();
			LayoutHelper.Below(toolStrip).Left(this).Bottom(this).Width(palettePanel.Width).Apply(palettePanel);
			palettePanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;

			palettePanel.SettingChanged += new EventHandler(PalettePanel_OnSettingChanged);
			palettePanel.SizeChanged += new EventHandler(PalettePanel_OnSizeChanged);

			this.Controls.Add(palettePanel);

			if(collection != null)
			{
				if(collection.ColorPalette != null)
					palettePanel.Set(collection.ColorPalette);
				else if(collection.Config.PaletteFileName != null)
					palettePanel.Set(collection.Config.PaletteFileName);
			}

			flowPanel = new FlowLayoutPanel();
			flowPanel.AutoScroll = true;
			flowPanel.BorderStyle = BorderStyle.FixedSingle;
			LayoutHelper.RightOf(palettePanel).Right(this).MatchTop(palettePanel).Bottom(this).Apply(flowPanel);
			flowPanel.Anchor = LayoutHelper.AnchorAll;
			this.Controls.Add(flowPanel);
		}

		#endregion

		#region Event Triggers

		private void TriggerProjectSelected()
		{
			if(ProjectSelected == null)
				return;
			ProjectSelected(collection, new ProjectEventArgs(collection.Projects[selectedProjectIndex]));
		}

		#endregion

		#region Event Handlers

		private void PalettePanel_OnSettingChanged(object sender, EventArgs e)
		{
			if(collection != null)
			{
				collection.UpdatePaletteOption(palettePanel.ColorPalette, palettePanel.PaletteFileName);
			}
		}

		private void PalettePanel_OnSizeChanged(object sender, EventArgs e)
		{
			int delta = palettePanel.Right - flowPanel.Left;
			flowPanel.Left += delta;
			flowPanel.Width -= delta;
		}

		private void Collection_OnNew(object sender, EventArgs e)
		{
			NewCollection();
		}

		private void Collection_OnOpen(object sender, EventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Collection Files|*" + PPCollection.COLLECTION_EXTENSION;
			openFileDialog.Title = "Open Collection";
			if(openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
			{
				return;
			}
			OpenCollection(openFileDialog.FileName);
		}

		private void Collection_OnAddProject(object sender, EventArgs e)
		{
			//todo: is there a way to combine this with the similar from OneImageForm
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Project and Image Files|*" + PPProject.PROJECT_EXTENSION + ";*.BMP;*.PNG;*.JPG;*.JPEG;*.GIF;*.TIFF";
			openFileDialog.Title = "Open Projects or Images";
			openFileDialog.Multiselect = true;
			if(openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
			{
				return;
			}

			foreach(string fileName in openFileDialog.FileNames)
			{
				try
				{
					PPProject project = collection.LoadProject(fileName);
					DisplayProject(project);
				}
				catch(DuplicateException)
				{
					MessageBox.Show("Skipping '" + fileName + "'. The file is already in the collection.", "Duplicate File", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		private void Colleciton_OnSave(object sender, EventArgs e)
		{
			Save();
		}

		private void Collection_OnSaveAs(object sender, EventArgs e)
		{
			SaveAs();
		}

		private void Collection_OnStatusChanged(object sender, EventArgs e)
		{
			PPCollection collection = (sender as PPCollection);
			if(collection == null)
			{
				saveToolStripItem.Image = IconManager.SAVE;
				return;
			}

			if(collection.EditedSinceLastSave)
				saveToolStripItem.Image = IconManager.SAVE_RED;
			else
				saveToolStripItem.Image = IconManager.SAVE;
		}

		private void Form_OnNewPalette(object sender, EventArgs e)
		{
			using(EditPaletteDialog form = new EditPaletteDialog())
			{
				form.StartPosition = FormStartPosition.Manual;
				form.Location = new Point(this.Location.X + 30, this.Location.Y + 30);
				if(form.ShowDialog() != DialogResult.OK)
					return;
				if(form.FullFilename == null)
					return;
				palettePanel.Set(form.FullFilename);
				if(collection != null)
				{
					collection.UpdatePaletteOption(palettePanel.ColorPalette, palettePanel.PaletteFileName);
				}
			}
		}

		private void Form_OnLoadPalette(object sender, EventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Palette Files|*.aco;*.gpl;*.pal";
			openFileDialog.Title = "Select a Palette File";

			if(openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
			{
				return;
			}
			palettePanel.Set(openFileDialog.FileName);
			if(collection != null)
			{
				collection.UpdatePaletteOption(palettePanel.ColorPalette, palettePanel.PaletteFileName);
			}
		}

		private void Form_OnEditPalette(object sender, EventArgs e)
		{
			EditPaletteDialog dialog;
			if(collection != null)
			{
				if(collection.Config.PaletteOption == PPConfig.PaletteOptions.SaveFile)
				{
					dialog = new EditPaletteDialog(collection.ColorPalette);
				}
				else
				{
					dialog = new EditPaletteDialog(collection.Config.PaletteFileName);
				}
			}
			else
			{
				dialog = new EditPaletteDialog(palettePanel.PaletteFileName);
			}
			dialog.StartPosition = FormStartPosition.Manual;
			dialog.Location = new Point(this.Location.X + 30, this.Location.Y + 30);
			if(dialog.ShowDialog() != DialogResult.OK)
				return;

			if(collection != null && collection.Config.PaletteOption == PPConfig.PaletteOptions.SaveFile)
			{
				palettePanel.Set(dialog.ColorPalette);
			}
			else
			{
				palettePanel.Set(dialog.FullFilename);
			}

			if(collection != null)
			{
				collection.UpdatePaletteOption(palettePanel.ColorPalette, palettePanel.PaletteFileName);
			}
		}

		private void Form_Closing(object sender, FormClosingEventArgs e)
		{
			bool continueOperation = PossiblySaveChangesBeforeClosingCollection();
			if(!continueOperation)
			{
				e.Cancel = true;
			}
		}

		private void Project_OnMouseEnter(object sender, EventArgs e)
		{
			Control control = (sender as Control);
			control.BackColor = Color.LightCyan;
		}

		private void Project_OnMouseLeave(object sender, EventArgs e)
		{
			Control control = (sender as Control);
			if(IsSelected(control.TabIndex))
				control.BackColor = Color.Gold;
			else
				control.BackColor = SystemColors.Control;
		}

		private void Project_OnRemove(object sender, EventArgs e)
		{
			Control control = (sender as MenuItem).GetContextMenu().SourceControl;
			collection.RemoveProjectAt(control.TabIndex);
			flowPanel.Controls.Remove(control);

			int index = 0;
			foreach(Control child in flowPanel.Controls)
			{
				child.TabIndex = index;
				index++;
			}
		}

		private void Project_OnClick(object sender, EventArgs e)
		{
			Control control = (sender as Control);
			if(control is PictureBox)
				control = control.Parent;
			SetSelection(control.TabIndex);
		}

		#endregion

		private void NewCollection()
		{
			bool continueOperation = PossiblySaveChangesBeforeClosingCollection();
			if(!continueOperation)
				return;

			collection = new PPCollection();

			if(palettePanel.PaletteFileName != null)
				collection.SetPaletteOption(PPConfig.PaletteOptions.SaveFileName, paletteFileName: palettePanel.PaletteFileName);

			SetupCollection();
			flowPanel.Controls.Clear();
		}

		private void SetupCollection()
		{
			Collection_OnStatusChanged(collection, new EventArgs());
			collection.StatusChanged += new EventHandler(Collection_OnStatusChanged);
			Collection_OnStatusChanged(collection, new EventArgs());
		}

		public void OpenCollection(string fileName)
		{
			if(String.IsNullOrEmpty(fileName))
			{
				NewCollection();
				return;
			}

			collection = PPCollection.Load(fileName);
			SetupCollection();
			switch(collection.Config.PaletteOption)
			{
				case PPConfig.PaletteOptions.SaveFile:
					palettePanel.Set(collection.ColorPalette);
					break;
				case PPConfig.PaletteOptions.SaveFileName:
					palettePanel.Set(collection.Config.PaletteFileName);
					break;
			}


			flowPanel.Controls.Clear();
			foreach(PPProject project in collection.Projects)
			{
				DisplayProject(project);
			}
		}

		private void DisplayProject(PPProject project)
		{
			int padding = 4;

			Panel panel = new Panel();
			panel.Cursor = Cursors.Hand;
			panel.ContextMenu = projectContextMenu;

			PictureBox pictureBox = new PictureBox();
			pictureBox.Width = THUMBNAIL_SIZE + padding + padding;
			pictureBox.Height = THUMBNAIL_SIZE + padding + padding;
			pictureBox.Left = 0;
			pictureBox.Top = 0;
			pictureBox.Image = project.GetThumbnail(THUMBNAIL_SIZE, THUMBNAIL_SIZE);
			pictureBox.SizeMode = PictureBoxSizeMode.CenterImage;
			pictureBox.MouseEnter += new EventHandler(Project_OnMouseEnter);
			pictureBox.MouseLeave += new EventHandler(Project_OnMouseLeave);
			pictureBox.Click += new EventHandler(Project_OnClick);

			panel.Controls.Add(pictureBox);

			Label label = new Label();
			if(!String.IsNullOrEmpty(project.SaveToFileName))
			{
				label.Text = Path.GetFileNameWithoutExtension(project.SaveToFileName);
			}
			label.Width = THUMBNAIL_SIZE;
			label.Height = 20;
			label.Left = padding;
			label.Top = pictureBox.Bottom;
			panel.Controls.Add(label);

			panel.Width = pictureBox.Width;
			panel.Height = label.Bottom;
			flowPanel.Controls.Add(panel);
		}

		/// <summary>
		/// Returns True if the operation continues, False if the operation is cancelled.
		/// </summary>
		private bool Save()
		{
			if(!SetAllProjectFileNames())
				return false;

			if(String.IsNullOrEmpty(collection.SaveToFileName))
				return SaveAs();
			else
				collection.Save();
			return true;
		}

		/// <summary>
		/// Returns True if the operation continues, False if the operation is cancelled.
		/// </summary>
		private bool SaveAs()
		{
			if(!SetAllProjectFileNames())
				return false;

			SaveFileDialog saveFileDialog = new SaveFileDialog();
			saveFileDialog.Filter = "Collection Files|*" + PPCollection.COLLECTION_EXTENSION;
			saveFileDialog.Title = "Save Collection";
			if(saveFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
			{
				return false;
			}
			if(!EditProjectOptions())
			{
				return false;
			}

			collection.SaveAs(saveFileDialog.FileName);
			return true;
		}

		/// <summary>
		/// Returns true if the operation is OK, returns false if the operation is Canceled.
		/// </summary>
		private bool EditProjectOptions()
		{
			using(ProjectOptionsDialog form = new ProjectOptionsDialog(collection.Config))
			{
				if(form.ShowDialog(this) != DialogResult.OK)
					return false;
				collection.SetPaletteOption(form.PaletteOption, palettePanel.ColorPalette, palettePanel.PaletteFileName);
			}
			return true;
		}

		/// <summary>
		/// If any projects have no filename,
		/// gets all those from the user and saves those files.
		/// </summary>
		/// <returns>Returns True for continue operation, returns False for cancel operation.</returns>
		private bool SetAllProjectFileNames()
		{
			List<PPProject> projects = collection.Projects.Where(p => String.IsNullOrEmpty(p.SaveToFileName)).ToList();
			if(projects.Count == 0)
				return true;

			Bitmap[] thumbnails = projects.Select(p => p.GetThumbnail(300, 300)).ToArray();
			using(SaveProjectsDialog dialog = new SaveProjectsDialog(thumbnails))
			{
				bool gotFileNames = (dialog.ShowDialog() == DialogResult.OK);
				if(!gotFileNames)
					return false;

				for(int i = 0; i < projects.Count; i++)
				{
					projects[i].SetFileName(dialog.FileNames[i]);
				}
			}
			return true;
		}

		/// <summary>
		/// Checks with user if they want to save changes to collection before closing it.
		/// </summary>
		/// <returns>True for continue operation; False for cancel operation.</returns>
		private bool PossiblySaveChangesBeforeClosingCollection()
		{
			if(collection == null || !collection.EditedSinceLastSave)
				return true;
			DialogResult result = MessageBox.Show("You are about to lose your changes.\nDo you want to save changes before closing the image?", "Save Before Closing", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
			switch(result)
			{
				case DialogResult.Cancel: return false;
				case DialogResult.No: return true;
				case DialogResult.Yes:
					return Save();
			}
			return true;
		}

		//todo: copy colors example image from https://stackoverflow.com/questions/4556559/is-there-an-online-example-of-all-the-colours-in-system-drawing-color to code notes

		/// <summary>
		/// Set which project is selected by 0-based index;
		/// </summary>
		private void SetSelection(int index)
		{
			selectedProjectIndex = index;

			foreach(Control control in flowPanel.Controls)
			{
				if(IsSelected(control.TabIndex))
				{
					control.BackColor = Color.Gold;
				}
				else
				{
					control.BackColor = SystemColors.Control;
				}
			}

			TriggerProjectSelected();
		}

		/// <summary>
		/// Returns true if this is the selected index.
		/// </summary>
		private bool IsSelected(int index)
		{
			return (selectedProjectIndex == index);
		}

		/// <summary>
		/// Tells collection form that project open here has been edited in OneImageForm.
		/// Update the thumbnail.
		/// </summary>
		public void UpdateProject(PPProject project)
		{
			int index = Array.IndexOf(collection.Projects, project);
			if(index == -1)
				return;
			int i = 0;
			foreach(Control control in flowPanel.Controls)
			{
				if(i == index)
				{
					PictureBox pictureBox = (control as PictureBox);
					pictureBox.Image = project.GetThumbnail(THUMBNAIL_SIZE, THUMBNAIL_SIZE);
					return;
				}
				i++;
			}
		}
	}
}
