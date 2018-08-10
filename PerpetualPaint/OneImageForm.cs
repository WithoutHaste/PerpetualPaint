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
			set {
			/*	pictureBox.Location = new Point(
					0 - (value.X - (scrollPanel.ClientSize.Width / 2)),
					0 - (value.Y - (scrollPanel.ClientSize.Height / 2))
					);*/
			}
		}

		private Point MasterImageVisibleCenterPoint {
			get {
				Point centerPoint = PictureBoxVisibleCenterPoint;
				return new Point((int)(centerPoint.X / imageScale), (int)(centerPoint.Y / imageScale));
			}
			set {
				PictureBoxVisibleCenterPoint = new Point((int)(value.X * imageScale), (int)(value.Y * imageScale));
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

			if(pictureBox.SizeMode != PictureBoxSizeMode.Zoom) return;

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

			Fit();
			UpdateZoomedImage();
		}
		private void Fit()
		{
			imageScale = 1;
			pictureBox.Dock = DockStyle.Fill;
			pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
		}

		private void Image_OnZoomIn(object sender, EventArgs e)
		{
			if(!this.HasImage) return;

			InitZoom();
			Point centerPoint = MasterImageVisibleCenterPoint;
			imageScale *= (1 + zoomUnits);
			pictureBox.Dock = DockStyle.None;
			pictureBox.SizeMode = PictureBoxSizeMode.CenterImage;
			UpdateZoomedImage(centerPoint);
		}

		private void Image_OnZoomOut(object sender, EventArgs e)
		{
			if(!this.HasImage) return;

			InitZoom();
			Point centerPoint = MasterImageVisibleCenterPoint;
			imageScale = Math.Max(zoomUnits, imageScale * (1 - zoomUnits));
			pictureBox.Dock = DockStyle.None;
			pictureBox.SizeMode = PictureBoxSizeMode.CenterImage;
			UpdateZoomedImage(centerPoint);
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
			Fit();
			UpdateZoomedImage();
		}

		private void UpdateZoomedImage(Point? centerPoint = null)
		{
			this.SuspendLayout();

			int zoomedWidth = (int)(masterImage.Width * imageScale);
			int zoomedHeight = (int)(masterImage.Height * imageScale);
			zoomedImage = new Bitmap(zoomedWidth, zoomedHeight);
			using(Graphics graphics = Graphics.FromImage(zoomedImage))
			{
				graphics.DrawImage(masterImage, new Rectangle(0, 0, zoomedWidth, zoomedHeight));
			}
			pictureBox.Size = new Size(zoomedImage.Width, zoomedImage.Height);
			pictureBox.Image = zoomedImage;

			UpdateScrollBars(centerPoint);

			this.ResumeLayout();
		}

		private void UpdateScrollBars(Point? centerPoint)
		{
			if(pictureBox.SizeMode == PictureBoxSizeMode.Zoom)
			{
				return;
			}

			if(centerPoint == null) throw new ArgumentException("CenterPoint required for zoom mode.");

			scrollPanel.AutoScrollPosition = new Point(
				(int)((centerPoint.Value.X * imageScale) - (scrollPanel.ClientSize.Width / 2)),
				(int)((centerPoint.Value.Y * imageScale) - (scrollPanel.ClientSize.Height / 2))
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
