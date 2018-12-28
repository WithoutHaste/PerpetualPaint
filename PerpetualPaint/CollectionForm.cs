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

		/// <summary>The collection being displayed.</summary>
		private PPCollection collection;

		private ContextMenu projectContextMenu;

		private int selectedProjectIndex = -1;

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

			this.Menu = new MainMenu();
			this.Menu.MenuItems.Add(fileMenu);

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
			//todo:
			//toolStrip.Items.Add("New", IconManager.NEW_FILE, Form_OnNewProject);
			toolStrip.Items.Add("Add", IconManager.ADD, Collection_OnAddProject);

			this.Controls.Add(toolStrip);
		}

		private void InitControls()
		{
			flowPanel = new FlowLayoutPanel();
			flowPanel.AutoScroll = true;
			flowPanel.BorderStyle = BorderStyle.FixedSingle;
			LayoutHelper.Left(this, 0).Right(this, 0).Top(this, 0).Bottom(this, 0).Apply(flowPanel);
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
			if(collection.EditedSinceLastSave)
				saveToolStripItem.Image = IconManager.SAVE_RED;
			else
				saveToolStripItem.Image = IconManager.SAVE;
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
			SetSelection(control.TabIndex);
		}

		#endregion

		private void NewCollection()
		{
			bool continueOperation = PossiblySaveChangesBeforeClosingCollection();
			if(!continueOperation)
				return;

			collection = new PPCollection();
			SetupCollection();
			flowPanel.Controls.Clear();
		}

		private void SetupCollection()
		{
			Collection_OnStatusChanged(collection, new EventArgs());
			collection.StatusChanged += new EventHandler(Collection_OnStatusChanged);
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

			flowPanel.Controls.Clear();
			foreach(PPProject project in collection.Projects)
			{
				DisplayProject(project);
			}
		}

		private void DisplayProject(PPProject project)
		{
			int padding = 4;

			PictureBox pictureBox = new PictureBox();
			pictureBox.Width = THUMBNAIL_SIZE + padding + padding;
			pictureBox.Height = THUMBNAIL_SIZE + padding + padding;
			pictureBox.Image = project.GetThumbnail(THUMBNAIL_SIZE, THUMBNAIL_SIZE);
			pictureBox.SizeMode = PictureBoxSizeMode.CenterImage;

			pictureBox.Cursor = Cursors.Hand;
			pictureBox.MouseEnter += new EventHandler(Project_OnMouseEnter);
			pictureBox.MouseLeave += new EventHandler(Project_OnMouseLeave);
			pictureBox.Click += new EventHandler(Project_OnClick);

			pictureBox.ContextMenu = projectContextMenu;

			flowPanel.Controls.Add(pictureBox);
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
				collection.SetPaletteOption(form.PaletteOption);
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
