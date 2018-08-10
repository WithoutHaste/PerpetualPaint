using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsExtensions;

namespace PerpetualPaint
{
	public class OneImageForm : Form
	{
		private ToolStrip toolStrip;
		private Panel scrollPanel;
		private PictureBox pictureBox;

		private const int SCALE_FIT = -1;
		private const int MAX_IMAGE_DIMENSION = 9000;

		private string saveFullFilename;
		private Bitmap masterImage;
		private Bitmap zoomedImage;
		private double imageScale = 1; //0.5 means zoomedImage width is half that of masterImage
		private double zoomUnits = 0.2; //this is the percentage of change

		private bool HasImage { get { return masterImage != null; } }

		private Point PictureBoxVisibleCenterPoint {
			get {
				return new Point(
					(0 - pictureBox.Location.X) + (scrollPanel.ClientSize.Width / 2),
					(0 - pictureBox.Location.Y) + (scrollPanel.ClientSize.Height / 2)
					);
			}
		}

		private Point MasterImageVisibleCenterPoint {
			get {
				Point centerPoint = PictureBoxVisibleCenterPoint;
				return new Point((int)(centerPoint.X / imageScale), (int)(centerPoint.Y / imageScale));
			}
		}

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
			fileMenu.MenuItems.Add("Open", new EventHandler(Form_OnOpenFile));

			this.Menu = new MainMenu();
			this.Menu.MenuItems.Add(fileMenu);

#if DEBUG
			MenuItem debugMenu = new MenuItem("Debug");
			debugMenu.MenuItems.Add("Show Error Message", new EventHandler(Debug_OnShowErrorMessage));
			this.Menu.MenuItems.Add(debugMenu);
#endif
		}

		private void InitTools()
		{
			toolStrip = new ToolStrip();
			toolStrip.Dock = DockStyle.Top;
			toolStrip.Items.Add("Fit", Image.FromFile("resources/icons/icon_fit.png"), Image_OnFit);
			toolStrip.Items.Add("Zoom In", Image.FromFile("resources/icons/icon_plus.png"), Image_OnZoomIn);
			toolStrip.Items.Add("Zoom Out", Image.FromFile("resources/icons/icon_minus.png"), Image_OnZoomOut);

			this.Controls.Add(toolStrip);
		}

		private void InitImage()
		{
			scrollPanel = new Panel();
			scrollPanel.AutoScroll = true;
			scrollPanel.Location = LayoutHelper.PlaceBelow(toolStrip);
			scrollPanel.Size = LayoutHelper.FillBelow(this, toolStrip);
			scrollPanel.Anchor = LayoutHelper.AnchorAll;

			pictureBox = new PictureBox();
			pictureBox.Dock = DockStyle.Fill;
			pictureBox.SizeMode = PictureBoxSizeMode.Zoom;

			scrollPanel.Controls.Add(pictureBox);
			this.Controls.Add(scrollPanel);
		}

		/// <summary>
		/// Start zoom based on the current zoom level of the picture box.
		/// </summary>
		private void InitZoom()
		{
			if(!this.HasImage) throw new Exception("Should not be able to InitZoom before image is loaded.");

			if(imageScale != SCALE_FIT) return;

			double widthScale = (double)pictureBox.DisplayRectangle.Width / (double)masterImage.Width;
			double heightScale = (double)pictureBox.DisplayRectangle.Height / (double)masterImage.Height;
			imageScale = Math.Min(widthScale, heightScale);
		}

		#endregion

		#region Event Handlers

		private void Form_OnOpenFile(object sender, EventArgs e)
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

		private void Image_OnFit(object sender, EventArgs e)
		{
			if(!this.HasImage) return;

			UpdateZoomedImage(SCALE_FIT);
		}

		private void Image_OnZoomIn(object sender, EventArgs e)
		{
			if(!this.HasImage) return;

			InitZoom();
			double newImageScale = imageScale * (1 + zoomUnits);
			UpdateZoomedImage(newImageScale);
		}

		private void Image_OnZoomOut(object sender, EventArgs e)
		{
			if(!this.HasImage) return;

			InitZoom();
			double newImageScale = Math.Max(zoomUnits, imageScale * (1 - zoomUnits));
			UpdateZoomedImage(newImageScale);
		}

#if DEBUG
		private void Debug_OnShowErrorMessage(object sender, EventArgs e)
		{
			try
			{
				throw new Exception("More detailed error message.");
			}
			catch(Exception exception)
			{
				HandleError("Summary for user.", exception);
			}
		}
#endif

#endregion

		private void UpdateMasterImage(string fullFilename)
		{
			saveFullFilename = fullFilename;
			masterImage = (Bitmap)Image.FromFile(fullFilename);
			UpdateZoomedImage(SCALE_FIT);
		}

		private void UpdateZoomedImage(double newImageScale)
		{
			this.SuspendLayout();

			double previousImageScale = imageScale;

			int zoomedWidth = masterImage.Width;
			int zoomedHeight = masterImage.Height;
			Point centerPoint = MasterImageVisibleCenterPoint;
			imageScale = newImageScale;
			if(imageScale == SCALE_FIT)
			{
				pictureBox.Dock = DockStyle.Fill;
				pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
			}
			else
			{
				pictureBox.Dock = DockStyle.None;
				pictureBox.SizeMode = PictureBoxSizeMode.CenterImage;

				//todo: refactor options?
				zoomedWidth = (int)(masterImage.Width * imageScale);
				zoomedHeight = (int)(masterImage.Height * imageScale);
				if(zoomedWidth > MAX_IMAGE_DIMENSION || zoomedHeight > MAX_IMAGE_DIMENSION)
				{
					double recalculateImageScale = Math.Min(MAX_IMAGE_DIMENSION / masterImage.Width, MAX_IMAGE_DIMENSION / masterImage.Height);
					imageScale = recalculateImageScale;
					zoomedWidth = (int)(masterImage.Width * imageScale);
					zoomedHeight = (int)(masterImage.Height * imageScale);
				}
			}

			try
			{
				zoomedImage = new Bitmap(zoomedWidth, zoomedHeight);
			}
			catch(ArgumentException exception)
			{
				HandleError("Error resizing image.", exception);
				UpdateZoomedImage(previousImageScale);
			}

			using(Graphics graphics = Graphics.FromImage(zoomedImage))
			{
				graphics.DrawImage(masterImage, new Rectangle(0, 0, zoomedWidth, zoomedHeight));
			}
			pictureBox.Size = new Size(zoomedImage.Width, zoomedImage.Height);
			pictureBox.Image = zoomedImage;

			if(imageScale != SCALE_FIT)
			{
				UpdateScrollBars(centerPoint);
			}

			this.ResumeLayout();
		}

		private void UpdateScrollBars(Point masterImageCenterPoint)
		{
			scrollPanel.AutoScrollPosition = new Point(
				(int)((masterImageCenterPoint.X * imageScale) - (scrollPanel.ClientSize.Width / 2)),
				(int)((masterImageCenterPoint.Y * imageScale) - (scrollPanel.ClientSize.Height / 2))
				);
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
