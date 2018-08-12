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
		private HashSet<Point> blackPixelSet;
		private List<HashSet<Point>> pixelSets;

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
			pictureBox.Cursor = Cursors.Hand;
			pictureBox.Click += new EventHandler(Image_OnClick);

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
			OpenFile();
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

		private void Image_OnClick(object sender, EventArgs e)
		{
			if(selectedColor == null) return;

			if(HasImage)
			{
				Point pictureBoxPoint = pictureBox.PointToClient(new Point(MousePosition.X, MousePosition.Y));
				if(pictureBox.SizeMode == PictureBoxSizeMode.Zoom)
				{
					float widthScale = (float)pictureBox.Width / (float)masterImage.Width;
					float heightScale = (float)pictureBox.Height / (float)masterImage.Height;
					float scale = Math.Min(widthScale, heightScale);
					int widthDisplay = (int)(masterImage.Width * scale);
					int heightDisplay = (int)(masterImage.Height * scale);
					Point displayOriginPoint = new Point((pictureBox.Width - widthDisplay) / 2, (pictureBox.Height - heightDisplay) / 2); //point of image relative to picturebox
					Point displayPoint = new Point(pictureBoxPoint.X - displayOriginPoint.X, pictureBoxPoint.Y - displayOriginPoint.Y);

					Point masterImagePoint = new Point((int)(displayPoint.X / scale), (int)(displayPoint.Y / scale));
					if(masterImagePoint.X < 0 || masterImagePoint.X >= masterImage.Width) return;
					if(masterImagePoint.Y < 0 || masterImagePoint.Y >= masterImage.Height) return;

					//masterImage.SetPixel(masterImagePoint.X, masterImagePoint.Y, selectedColor.Value);
					ColorPixel(masterImagePoint, selectedColor.Value);
					UpdateZoomedImage(SCALE_FIT);
				}
				else
				{
				}
			}
			else
			{
				OpenFile();
			}
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

		private void ColorPixel(Point point, Color color)
		{
			//todo: support color over color instead of just color over grayscale
			List<Point> todo = new List<Point>() { point };
			while(todo.Count > 0)
			{
				Point p = todo.First();
				todo.RemoveAt(0);
				Color oldColor = masterImage.GetPixel(p.X, p.Y);
				if(ColorIsBlack(oldColor))
					continue;
				if(!ColorIsGrayscale(oldColor))
					continue;

				if(ColorIsWhite(oldColor))
				{
					masterImage.SetPixel(p.X, p.Y, color);
				}
				else
				{
					//how to apply value to color that has value of its own? in a fully reversible way?
					//todo: how to make this part easy to test?
					HSV oldHSV = WithoutHaste.Drawing.ColorPalette.Utilities.HSVFromColor(oldColor);
					//treating newColor is "white", adjust underlying value on range from newWhite to black
					HSV newWhite = WithoutHaste.Drawing.ColorPalette.Utilities.HSVFromColor(color);
					//todo: document that coloring in with black and other very dark color will destroy some of your grayscale gradient
					//todo: may need to change HSV ranges in library to ints 0-360 and 0-100, since that seems to be how online tools handle it
					float adjustedValue = oldHSV.Value * newWhite.Value;
					HSV adjustedHSV = new HSV(newWhite.Hue, newWhite.Saturation, adjustedValue);
					Color adjustedColor = WithoutHaste.Drawing.ColorPalette.Utilities.ColorFromHSV(adjustedHSV);
					masterImage.SetPixel(p.X, p.Y, adjustedColor);
				}

				masterImage.SetPixel(p.X, p.Y, color); //todo: keep value
				Point left = new Point(p.X - 1, p.Y);
				Point right = new Point(p.X + 1, p.Y);
				Point up = new Point(p.X, p.Y - 1);
				Point down = new Point(p.X, p.Y + 1);
				if(PointInRange(left) && !PointInList(todo, left)) todo.Add(left);
				if(PointInRange(right) && !PointInList(todo, right)) todo.Add(right);
				if(PointInRange(up) && !PointInList(todo, up)) todo.Add(up);
				if(PointInRange(down) && !PointInList(todo, down)) todo.Add(down);

				if(todo.Count > 1000)
					return;
			}
		}
		private bool PointInRange(Point point)
		{
			return (point.X >= 0 && point.X < masterImage.Width && point.Y >= 0 && point.Y < masterImage.Height);
		}
		//todo: allow variable tolerance with demo of pure white/black image
		private bool ColorIsBlack(Color color)
		{
			return (ColorIsGrayscale(color) && color.R < 100);
		}
		private bool ColorIsGrayscale(Color color)
		{
			return (color.R == color.G && color.G == color.B);
		}
		private bool ColorIsWhite(Color color)
		{
			return (ColorIsGrayscale(color) && color.R > 230);
		}

		private void OpenFile()
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

		private void UpdateMasterImage(string fullFilename)
		{
			saveImageFullFilename = fullFilename;
			masterImage = (Bitmap)Image.FromFile(fullFilename);
			//CalculateValues();
			//CalculatePixelSets(); //too slow even with hashsets
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

		//private void CalculateValues()
		//{
		//	HashSet<Color> colors = new HashSet<Color>();
		//	for(int x = 0; x < masterImage.Width; x++)
		//	{
		//		for(int y = 0; y < masterImage.Height; y++)
		//		{
		//			colors.Add(masterImage.GetPixel(x, y));
		//		}
		//	}
		//}

		//		//todo: move pixel sets into their own object?
		//		private void CalculatePixelSets()
		//		{
		//			blackPixelSet = new HashSet<Point>();
		//			HashSet<Point> otherPixelSet = new HashSet<Point>();

		//			for(int x = 0; x < masterImage.Width; x++)
		//			{
		//				for(int y = 0; y < masterImage.Height; y++)
		//				{
		//					Color pixelColor = masterImage.GetPixel(x, y);
		//					Point point = new Point(x, y);
		//					if(ColorIsBlack(pixelColor))
		//					{
		//						blackPixelSet.Add(point);
		//						continue;
		//					}
		//					otherPixelSet.Add(point);
		//					//if(PointInSets(pixelSets, point))
		//					//{
		//					//	continue;
		//					//}
		//					//pixelSets.Add(FindPixelSet(point));
		//				}
		//			}

		////			pixelSets = DividePixelSets(otherPixelSet);
		//		}

		//		private List<List<Point>> DividePixelSets(List<Point> pixels)
		//		{
		//			List<List<Point>> sets = new List<List<Point>>();
		//			foreach(Point point in pixels)
		//			{
		//				AddPixelToSet(sets, point);
		//			}
		//			return sets;
		//		}
		//		private void AddPixelToSet(List<List<Point>> sets, Point point)
		//		{
		//			foreach(List<Point> set in sets)
		//			{
		//				foreach(Point setPoint in set)
		//				{
		//					if(PointsAdjacent(setPoint, point))
		//					{
		//						set.Add(point);
		//						return;
		//					}
		//				}
		//			}
		//			sets.Add(new List<Point>() { point });
		//		}
		//		private bool PointsAdjacent(Point a, Point b)
		//		{
		//			if(a.X == b.X && (a.Y == b.Y - 1 || a.Y == b.Y + 1))
		//				return true;
		//			if(a.Y == b.Y && (a.X == b.X - 1 || a.X == b.X + 1))
		//				return true;
		//			return false;
		//		}

		//		private bool PointInSets(List<List<Point>> sets, Point point)
		//		{
		//			foreach(List<Point> set in sets)
		//			{
		//				if(PointInSet(set, point))
		//				{
		//					return true;
		//				}
		//			}
		//			return false;
		//		}

		private bool PointInList(List<Point> set, Point point)
		{
			foreach(Point p in set)
			{
				if(p.X == point.X && p.Y == point.Y)
				{
					return true;
				}
			}
			return false;
		}

			//		/// <summary>
			//		/// Find black-bounded set of pixels starting with point.
			//		/// </summary>
			//		private List<Point> FindPixelSet(Point point)
			//		{
			//			List<Point> set = new List<Point>() { point };
			//			ExtendPixelSet(set, new Point(point.X, point.Y - 1));
			//			ExtendPixelSet(set, new Point(point.X, point.Y + 1));
			//			ExtendPixelSet(set, new Point(point.X - 1, point.Y));
			//			ExtendPixelSet(set, new Point(point.X + 1, point.Y));
			//			return set;
			//		}

			//		private void ExtendPixelSet(List<Point> set, Point point)
			//		{
			//			if(point.X < 0 || point.X >= masterImage.Width) return;
			//			if(point.Y < 0 || point.Y >= masterImage.Height) return;
			//			if(masterImage.GetPixel(point.X, point.Y) == Color.Black) return;
			//			if(PointInSet(set, point)) return;
			//			set.Add(point);
			//			ExtendPixelSet(set, new Point(point.X, point.Y - 1));
			//			ExtendPixelSet(set, new Point(point.X, point.Y + 1));
			//			ExtendPixelSet(set, new Point(point.X - 1, point.Y));
			//			ExtendPixelSet(set, new Point(point.X + 1, point.Y));
			//		}

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
				if(selectedColor == null)
				{
					selectedColor = color;
				}

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
