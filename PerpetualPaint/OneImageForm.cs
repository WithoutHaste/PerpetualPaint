using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PerpetualPaint
{
	public class OneImageForm : Form
	{
		private ToolStrip toolStrip;
		private PictureBox pictureBox;

		private string saveFullFilename;
		private Bitmap masterImage;
		private Bitmap zoomedImage;
		private double imageScale = 1; //0.5 means zoomedImage width is half that of masterImage
		private double zoomUnits = 0.2; //this is the percentage of change

		private bool HasImage { get { return masterImage != null; } }

		public OneImageForm()
		{
			this.Text = "Perpetual Paint";
			this.Width = 800;
			this.Height = 600;

			InitMenus();
			InitTools();
			InitImage();
		}

		#region Init

		private void InitMenus()
		{
			MenuItem fileMenu = new MenuItem("File");
			fileMenu.MenuItems.Add("Open", new EventHandler(OnOpenFile));

			this.Menu = new MainMenu();
			this.Menu.MenuItems.Add(fileMenu);
		}

		private void InitTools()
		{
			toolStrip = new ToolStrip();
			toolStrip.Dock = DockStyle.Top;
			toolStrip.Items.Add("Fit", Image.FromFile("resources/icons/icon_fit.png"), OnFit);
			toolStrip.Items.Add("Zoom In", Image.FromFile("resources/icons/icon_plus.png"), OnZoomIn);
			toolStrip.Items.Add("Zoom Out", Image.FromFile("resources/icons/icon_minus.png"), OnZoomOut);

			this.Controls.Add(toolStrip);
		}

		private void InitImage()
		{
			pictureBox = new PictureBox();
			pictureBox.Location = new Point(0, toolStrip.Location.Y + toolStrip.Height);
			pictureBox.Size = new Size(this.ClientSize.Width, this.ClientSize.Height - toolStrip.Height);
			pictureBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			pictureBox.SizeMode = PictureBoxSizeMode.Zoom;

			this.Controls.Add(pictureBox);
		}

		/// <summary>
		/// Start zoom based on the current zoom level of the picture box.
		/// </summary>
		private void InitZoom()
		{
			if(!this.HasImage) return;
			if(pictureBox.SizeMode != PictureBoxSizeMode.Zoom) return;

			double widthScale = (double)pictureBox.DisplayRectangle.Width / (double)masterImage.Width;
			double heightScale = (double)pictureBox.DisplayRectangle.Height / (double)masterImage.Height;
			imageScale = Math.Min(widthScale, heightScale);
		}

		#endregion

		#region Event Handlers

		private void OnOpenFile(object sender, EventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Image Files|*.BMP;*.PNG;*.JPG;*.JPEG;*.GIF;*.TIFF";
			openFileDialog.Title = "Select an Image File";

			if(openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
			{
				return;
			}
			try
			{
				UpdateMasterImage(openFileDialog.FileName);
			}
			catch(FileNotFoundException exception)
			{
				HandleError("Failed to open file.", exception);
			}
		}

		private void OnFit(object sender, EventArgs e)
		{
			if(!this.HasImage) return;

			imageScale = 1;
			pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
			UpdateZoomedImage();
		}

		private void OnZoomIn(object sender, EventArgs e)
		{
			if(!this.HasImage) return;

			InitZoom();
			imageScale *= (1 + zoomUnits);
			pictureBox.SizeMode = PictureBoxSizeMode.CenterImage;
			UpdateZoomedImage();
		}

		private void OnZoomOut(object sender, EventArgs e)
		{
			if(!this.HasImage) return;

			InitZoom();
			imageScale = Math.Max(zoomUnits, imageScale * (1 - zoomUnits));
			pictureBox.SizeMode = PictureBoxSizeMode.CenterImage;
			UpdateZoomedImage();
		}

		#endregion

		private void UpdateMasterImage(string fullFilename)
		{
			saveFullFilename = fullFilename;
			masterImage = (Bitmap)Image.FromFile(fullFilename);
			UpdateZoomedImage();
		}

		private void UpdateZoomedImage()
		{
			this.SuspendLayout();

			int zoomedWidth = (int)(masterImage.Width * imageScale);
			int zoomedHeight = (int)(masterImage.Height * imageScale);
			zoomedImage = new Bitmap(zoomedWidth, zoomedHeight);
			using(Graphics graphics = Graphics.FromImage(zoomedImage))
			{
				graphics.DrawImage(masterImage, new Rectangle(0, 0, zoomedWidth, zoomedHeight));
			}
			pictureBox.Image = zoomedImage;

			this.ResumeLayout();
		}

		private void HandleError(string userMessage, Exception e)
		{
			string[] message = new string[] { userMessage, "", "Exception:", e.Message, "", "Stack Trace:", e.StackTrace };
			using(ErrorForm form = new ErrorForm("Error", message))
			{
				form.ShowDialog();
			}
		}
	}
}
