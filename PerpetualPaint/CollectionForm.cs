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

		/// <summary>Projects in the collection, in the same order as shown in <see cref='flowPanel'/>.</summary>
		private List<PPProject> projects = new List<PPProject>();

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
			//todo
		}

		private void Form_OnAdd(object sender, EventArgs e)
		{
			//todo: is there a way to combine this with the similar from OneImageForm
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Project and Image Files|*.PPP;*.BMP;*.PNG;*.JPG;*.JPEG;*.GIF;*.TIFF";
			openFileDialog.Title = "Open Project or Image";
			if(openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
			{
				return;
			}
			string filename = openFileDialog.FileName;

			PPProject project = new PPProject();
			if(Path.GetExtension(filename) == PPProject.PROJECT_EXTENSION)
			{
				PerpetualPaintLibrary.IO.LoadProject(filename, project);
				projects.Add(project);
			}
			else
			{
				//todo: more code similar to MasterImage.LoadImage
				Bitmap bitmap = ImageHelper.SafeLoadBitmap(filename);
				if(bitmap.Width == 0 && bitmap.Height == 0)
					throw new NotSupportedException("Cannot operate on a 0-pixel by 0-pixel bitmap.");

				if(Utilities.BitmapIsGreyscale(bitmap))
				{
					project.GreyscaleBitmap = bitmap;
				}
				else
				{
					project.ColorBitmap = bitmap;
				}
			}
			projects.Add(project);

			PictureBox pictureBox = new PictureBox();
			pictureBox.Width = IMAGE_WIDTH;
			pictureBox.Height = IMAGE_WIDTH;
			pictureBox.Image = project.GetThumbnail(pictureBox.Width, pictureBox.Height);
			pictureBox.SizeMode = PictureBoxSizeMode.CenterImage;
			flowPanel.Controls.Add(pictureBox);
		}

		private void Form_OnSave(object sender, EventArgs e)
		{
			//todo
		}

		private void Form_OnSaveAs(object sender, EventArgs e)
		{
			//todo
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
