using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsExtensions;
using WithoutHaste.Drawing.ColorPalette;

namespace PerpetualPaint
{
	public class OneImageForm : Form
	{
		private ToolStrip toolStrip;
		private Panel palettePanel;
		private Panel swatchPanel;
		private Panel scrollPanel;
		private PictureBox pictureBox;

		private const int SCALE_FIT = -1;
		private const int MAX_IMAGE_DIMENSION = 9000;
		private const int DEFAULT_SWATCHES_PER_ROW = 3;
		private const int MIN_SWATCHES_PER_ROW = 3;
		private const int MAX_SWATCHES_PER_ROW = 12;
		
		private readonly Image IMAGE_SELECTED_COLOR = Image.FromFile("resources/icons/icon_selector.png");

		private string saveImageFullFilename;
		private Bitmap masterImage;
		private Bitmap zoomedImage;
		private double imageScale = 1; //0.5 means zoomedImage width is half that of masterImage
		private double zoomUnits = 0.2; //this is the percentage of change

		private int SwatchesPerRow {
			get {
				return Properties.Settings.Default.SwatchesPerRow;
			}
			set {
				Properties.Settings.Default.SwatchesPerRow = value;
				Properties.Settings.Default.Save();
			}
		}

		private int swatchWidth = 25;
		private int palettePadding = 15;
		private Color? selectedColor;

		private string saveColorPaletteFullFilename = "resources/palettes/Bright-colors.aco";
		private ColorPalette colorPalette;

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
			InitPalette();
			InitImage();

			LoadPalette(saveColorPaletteFullFilename);
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

		//todo: possibly move palettePanel into its own Panel class with all behavior
		private void InitPalette()
		{
			int scrollBarBuffer = System.Windows.Forms.SystemInformation.VerticalScrollBarWidth + 5;
			int swatchesWidth = (swatchWidth * SwatchesPerRow) + scrollBarBuffer;
			int paletteWidth =  swatchesWidth + (2 * palettePadding);

			palettePanel = new Panel();
			palettePanel.Location = LayoutHelper.PlaceBelow(toolStrip);
			palettePanel.Size = LayoutHelper.FillBelow(this, toolStrip, paletteWidth);
			palettePanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;

			Button narrowPaletteButton = new Button();
			narrowPaletteButton.Text = "<<";
			narrowPaletteButton.Size = new Size(swatchWidth, swatchWidth);
			narrowPaletteButton.Location = LayoutHelper.PlaceBottomLeft(palettePanel, narrowPaletteButton, palettePadding);
			narrowPaletteButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
			narrowPaletteButton.Click += new EventHandler(Form_OnNarrowPalette);

			Button widenPaletteButton = new Button();
			widenPaletteButton.Text = ">>";
			widenPaletteButton.Size = new Size(swatchesWidth - narrowPaletteButton.Width, swatchWidth);
			widenPaletteButton.Location = LayoutHelper.PlaceBottomRight(palettePanel, widenPaletteButton, palettePadding);
			widenPaletteButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			widenPaletteButton.Click += new EventHandler(Form_OnWidenPalette);

			//todo: some formalizatio of layout helper options
			//like how to handle swatchPanel as a FillAbove(narrowPaletteButton)
			swatchPanel = new Panel();
			swatchPanel.AutoScroll = true;
			swatchPanel.Location = new Point(palettePadding, 0);
			swatchPanel.Size = new Size(swatchesWidth, narrowPaletteButton.Location.Y - palettePadding);
			swatchPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
			swatchPanel.BorderStyle = BorderStyle.Fixed3D;

			palettePanel.Controls.Add(swatchPanel);
			palettePanel.Controls.Add(narrowPaletteButton);
			palettePanel.Controls.Add(widenPaletteButton);
			this.Controls.Add(palettePanel);
		}

		private void InitImage()
		{
			scrollPanel = new Panel();
			scrollPanel.AutoScroll = true;
			scrollPanel.Location = LayoutHelper.PlaceRight(palettePanel);
			scrollPanel.Size = LayoutHelper.FillFromLocation(this, scrollPanel.Location);
			scrollPanel.Anchor = LayoutHelper.AnchorAll;
			scrollPanel.BorderStyle = BorderStyle.Fixed3D;

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

		private void Form_OnNarrowPalette(object sender, EventArgs e)
		{
			if(SwatchesPerRow == MIN_SWATCHES_PER_ROW) return;

			SwatchesPerRow--;
			ArrangePalette();
		}

		private void Form_OnWidenPalette(object sender, EventArgs e)
		{
			if(SwatchesPerRow == MAX_SWATCHES_PER_ROW) return;

			SwatchesPerRow++;
			ArrangePalette();
		}

		private void Form_OnClickColor(object sender, EventArgs e)
		{
			Panel colorPanel = sender as Panel;
			selectedColor = colorPanel.BackColor;

			foreach(Control child in swatchPanel.Controls)
			{
				if(child.BackColor == selectedColor)
				{
					child.BackgroundImage = IMAGE_SELECTED_COLOR;
					child.BackgroundImageLayout = ImageLayout.Stretch;
				}
				else
				{
					child.BackgroundImage = null;
				}
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
			saveImageFullFilename = fullFilename;
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

		private void LoadPalette(string fullFilename)
		{
			colorPalette = API.LoadACO(saveColorPaletteFullFilename);
			ArrangePalette();
		}

		private void ArrangePalette()
		{
			this.SuspendLayout();

			//todo: duplicate code
			int scrollBarBuffer = System.Windows.Forms.SystemInformation.VerticalScrollBarWidth + 5;
			int paletteWidth = (SwatchesPerRow * swatchWidth) + (2 * palettePadding) + scrollBarBuffer;

			palettePanel.Size = new Size(paletteWidth, palettePanel.Size.Height);
			swatchPanel.Size = new Size((SwatchesPerRow * swatchWidth) + scrollBarBuffer, swatchPanel.Size.Height);

			swatchPanel.Controls.Clear();
			int rowCount = 0;
			int colCount = 0;
			foreach(Color color in colorPalette.Colors)
			{
				Panel colorPanel = new Panel();
				colorPanel.Location = new Point(rowCount * swatchWidth, colCount * swatchWidth);
				colorPanel.Size = new Size(swatchWidth, swatchWidth);
				colorPanel.BackColor = color;
				if(color == selectedColor)
				{
					//todo: duplicate code
					colorPanel.BackgroundImage = IMAGE_SELECTED_COLOR;
					colorPanel.BackgroundImageLayout = ImageLayout.Stretch;
				}
				colorPanel.Cursor = Cursors.Hand;
				colorPanel.Click += new EventHandler(Form_OnClickColor);
				swatchPanel.Controls.Add(colorPanel);

				rowCount++;
				if(rowCount >= SwatchesPerRow)
				{
					rowCount = 0;
					colCount++;
				}
			}

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
