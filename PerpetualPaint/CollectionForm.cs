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

		/// <summary>Layout panel for the images.</summary>
		private FlowLayoutPanel flowPanel;

		/// <summary>The collection being displayed.</summary>
		private PPCollection collection = new PPCollection();

		private ContextMenu projectContextMenu;

		public CollectionForm(string fileName = null)
		{
			this.Text = "Collection";
			this.WindowState = FormWindowState.Normal;
			this.Width = 500;
			this.Height = 500;
			this.FormClosing += new FormClosingEventHandler(Form_Closing);

			InitMenus();
			InitControls();

			if(!String.IsNullOrEmpty(fileName))
			{
				OpenCollection(fileName);
			}
		}

		#region Init

		private void InitMenus()
		{
			MenuItem fileMenu = new MenuItem("File");
			MenuItem openCollection = new MenuItem("Open Collection", new EventHandler(Form_OnOpen));
			MenuItem addProject = new MenuItem("Add Image/Project", new EventHandler(Form_OnAdd));
			MenuItem saveCollection = new MenuItem("Save Collection", new EventHandler(Form_OnSave), Shortcut.CtrlS);
			MenuItem saveAsCollection = new MenuItem("Save Collection As", new EventHandler(Form_OnSaveAs), Shortcut.F12);
			fileMenu.MenuItems.Add(openCollection);
			fileMenu.MenuItems.Add(addProject);
			fileMenu.MenuItems.Add(saveCollection);
			fileMenu.MenuItems.Add(saveAsCollection);

			this.Menu = new MainMenu();
			this.Menu.MenuItems.Add(fileMenu);

			projectContextMenu = new ContextMenu();
			projectContextMenu.MenuItems.Add("Remove", Project_OnRemove);
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

		#region Event Handlers

		private void Form_OnOpen(object sender, EventArgs e)
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

		private void Form_OnAdd(object sender, EventArgs e)
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

		private void Form_OnSave(object sender, EventArgs e)
		{
			Save();
		}

		private void Form_OnSaveAs(object sender, EventArgs e)
		{
			SaveAs();
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

		#endregion

		public void OpenCollection(string fileName)
		{
			collection = PPCollection.Load(fileName);
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
			pictureBox.Image = project.GetThumbnail(pictureBox.Width, pictureBox.Height);
			pictureBox.SizeMode = PictureBoxSizeMode.CenterImage;

			pictureBox.Cursor = Cursors.Hand;
			pictureBox.MouseEnter += new EventHandler(Project_OnMouseEnter);
			pictureBox.MouseLeave += new EventHandler(Project_OnMouseLeave);

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
			if(collection == null)
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
	}
}
