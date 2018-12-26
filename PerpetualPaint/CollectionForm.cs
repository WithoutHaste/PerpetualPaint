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

		/// <summary>Maximum width and height of each image.</summary>
		public static readonly int IMAGE_WIDTH = 100;

		/// <summary>Layout panel for the images.</summary>
		private FlowLayoutPanel flowPanel;

		/// <summary>The collection being displayed.</summary>
		private PPCollection collection = new PPCollection();

		public CollectionForm()
		{
			this.Text = "Collection";
			this.WindowState = FormWindowState.Normal;
			this.Width = 500;
			this.Height = 500;
			this.FormClosing += new FormClosingEventHandler(Form_Closing);

			InitMenus();
			InitControls();
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
			openFileDialog.Filter = "Collection Files|*" + PPCollection.COLLECTION_EXTENSION_UPPERCASE;
			openFileDialog.Title = "Open Collection";
			if(openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
			{
				return;
			}

			collection = PPCollection.Load(openFileDialog.FileName);

			flowPanel.Controls.Clear();
			foreach(PPProject project in collection.Projects)
			{
				DisplayProject(project);
			}
		}

		private void Form_OnAdd(object sender, EventArgs e)
		{
			//todo: is there a way to combine this with the similar from OneImageForm
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Project and Image Files|*" + PPProject.PROJECT_EXTENSION_UPPERCASE + ";*.BMP;*.PNG;*.JPG;*.JPEG;*.GIF;*.TIFF";
			openFileDialog.Title = "Open Project or Image";
			if(openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
			{
				return;
			}

			PPProject project = collection.LoadProject(openFileDialog.FileName);
			DisplayProject(project);
		}

		private void Form_OnSave(object sender, EventArgs e)
		{
			if(!SetAllProjectFileNames())
				return;

			if(String.IsNullOrEmpty(collection.SaveToFileName))
				Form_OnSaveAs(sender, e);
			else
				collection.Save();
		}

		private void Form_OnSaveAs(object sender, EventArgs e)
		{
			if(!SetAllProjectFileNames())
				return;

			SaveFileDialog saveFileDialog = new SaveFileDialog();
			saveFileDialog.Filter = "Collection Files|*" + PPCollection.COLLECTION_EXTENSION_UPPERCASE;
			saveFileDialog.Title = "Save Collection";
			if(saveFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
			{
				return;
			}
			if(!EditProjectOptions())
			{
				return;
			}

			collection.SaveAs(saveFileDialog.FileName);
		}

		private void Form_Closing(object sender, FormClosingEventArgs e)
		{
			bool continueOperation = PossiblySaveChangesBeforeClosingCollection();
			if(!continueOperation)
			{
				e.Cancel = true;
			}
		}

		#endregion

		private void DisplayProject(PPProject project)
		{
			PictureBox pictureBox = new PictureBox();
			pictureBox.Width = IMAGE_WIDTH;
			pictureBox.Height = IMAGE_WIDTH;
			pictureBox.Image = project.GetThumbnail(pictureBox.Width, pictureBox.Height);
			pictureBox.SizeMode = PictureBoxSizeMode.CenterImage;
			flowPanel.Controls.Add(pictureBox);
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

			using(SaveProjectsDialog dialog = new SaveProjectsDialog(projects.Select(p => p.GetThumbnail(150, 150)).ToArray()))
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
			//todo: implement check if collection should be saved
			return true;
		}
	}
}
