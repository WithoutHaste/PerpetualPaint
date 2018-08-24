using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WithoutHaste.Drawing.Colors;
using WithoutHaste.Windows.GUI;

namespace PerpetualPaint
{
	public class OneImageForm : Form
	{
		private ToolStrip toolStrip;
		private Panel palettePanel;
		private SwatchPanel swatchPanel;
		private Panel scrollPanel;
		private StatusPanel statusPanel;
		private PixelPictureBox pictureBox;

		private History history;

		private const int SCALE_FIT = -1;
		private const int MAX_IMAGE_DIMENSION = 9000;
		private const int DEFAULT_SWATCHES_PER_ROW = 3;
		private const int MIN_SWATCHES_PER_ROW = 3;
		private const int MAX_SWATCHES_PER_ROW = 12;

		private IconManager iconManager = IconManager.Singleton;

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
		private int palettePadding = 15;
		private Color? selectedColor;

		private Queue<ColorAtPoint> requestColorQueue = new Queue<ColorAtPoint>();
		private RequestColorWorker requestColorWorker;

		private string PaletteFullFilename {
			get {
				return Properties.Settings.Default.PaletteFullFilename;
			}
			set {
				Properties.Settings.Default.PaletteFullFilename = value;
				Properties.Settings.Default.Save();
			}
		}

		private WithoutHaste.Drawing.Colors.ColorPalette colorPalette;

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

		//--------------------------------------------------

		public OneImageForm()
		{
			this.Text = "Perpetual Paint";
			this.Width = 800;
			this.Height = 600;

			InitMenus();
			InitTools();
			InitPalette();
			InitStatusPanel();
			InitImage();

			InitHistory();

			LoadPalette();
		}

		#region Init

		private void InitMenus()
		{
			MenuItem fileMenu = new MenuItem("File");
			fileMenu.MenuItems.Add("Open Image", new EventHandler(Form_OnOpenFile));
			fileMenu.MenuItems.Add("Save Image As", new EventHandler(Form_OnSaveAs));

			MenuItem editMenu = new MenuItem("Edit");
			editMenu.MenuItems.Add("Undo", new EventHandler(Form_OnUndo));
			editMenu.MenuItems.Add("Redo", new EventHandler(Form_OnRedo));

			MenuItem paletteMenu = new MenuItem("Palette");
			paletteMenu.MenuItems.Add("New", new EventHandler(Form_OnNewPalette));
			paletteMenu.MenuItems.Add("Load", new EventHandler(Form_OnLoadPalette));
			paletteMenu.MenuItems.Add("Edit", new EventHandler(Form_OnEditPalette));

			this.Menu = new MainMenu();
			this.Menu.MenuItems.Add(fileMenu);
			this.Menu.MenuItems.Add(editMenu);
			this.Menu.MenuItems.Add(paletteMenu);

#if DEBUG
			MenuItem debugMenu = new MenuItem("Debug");
			debugMenu.MenuItems.Add("Show Error Message", new EventHandler(Debug_OnShowErrorMessage));
			debugMenu.MenuItems.Add("Show Nested Error Message", new EventHandler(Debug_OnShowNestedErrorMessage));
			debugMenu.MenuItems.Add("Show Wait Message", new EventHandler(Debug_OnShowWaitMessage));
			this.Menu.MenuItems.Add(debugMenu);
#endif
		}

		private void InitTools()
		{
			toolStrip = new ToolStrip();
			toolStrip.Dock = DockStyle.Top;
			toolStrip.Items.Add("Fit", iconManager.ICON_ZOOM_FIT, Image_OnFit);
			toolStrip.Items.Add("Zoom In", iconManager.ICON_ZOOM_IN, Image_OnZoomIn);
			toolStrip.Items.Add("Zoom Out", iconManager.ICON_ZOOM_OUT, Image_OnZoomOut);
			toolStrip.Items.Add("100%", iconManager.ICON_ZOOM_100, Image_OnZoom1);
			toolStrip.Items.Add(new ToolStripSeparator());
			toolStrip.Items.Add("Undo", iconManager.ICON_UNDO, Form_OnUndo);
			toolStrip.Items.Add("Redo", iconManager.ICON_REDO, Form_OnRedo);

			this.Controls.Add(toolStrip);
		}

		//todo: possibly move palettePanel into its own Panel class with all behavior
		private void InitPalette()
		{
			int scrollBarBuffer = System.Windows.Forms.SystemInformation.VerticalScrollBarWidth + 5;
			int swatchesWidth = (SwatchPanel.SWATCH_WIDTH * SwatchesPerRow) + scrollBarBuffer;
			int paletteWidth =  swatchesWidth + (2 * palettePadding);

			palettePanel = new Panel();
			LayoutHelper.Below(toolStrip).Left(this).Bottom(this).Width(paletteWidth).Apply(palettePanel);
			palettePanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;

			Button narrowPaletteButton = new Button();
			narrowPaletteButton.Text = "<<";
			LayoutHelper.Bottom(palettePanel, palettePadding).Left(palettePanel, palettePadding).Width(SwatchPanel.SWATCH_WIDTH).Height(SwatchPanel.SWATCH_WIDTH).Apply(narrowPaletteButton);
			narrowPaletteButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
			narrowPaletteButton.Click += new EventHandler(Form_OnNarrowPalette);

			Button widenPaletteButton = new Button();
			widenPaletteButton.Text = ">>";
			LayoutHelper.Bottom(palettePanel, palettePadding).Right(palettePanel, palettePadding).Width(SwatchPanel.SWATCH_WIDTH).Height(SwatchPanel.SWATCH_WIDTH).Apply(widenPaletteButton);
			widenPaletteButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			widenPaletteButton.Click += new EventHandler(Form_OnWidenPalette);

			swatchPanel = new SwatchPanel(Form_OnClickColor);
			LayoutHelper.Top(palettePanel).MatchLeft(narrowPaletteButton).MatchRight(widenPaletteButton).Above(narrowPaletteButton, palettePadding).Apply(swatchPanel);
			swatchPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;

			palettePanel.Controls.Add(swatchPanel);
			palettePanel.Controls.Add(narrowPaletteButton);
			palettePanel.Controls.Add(widenPaletteButton);
			this.Controls.Add(palettePanel);
		}

		private void InitStatusPanel()
		{
			statusPanel = new StatusPanel();
			LayoutHelper.Bottom(this).RightOf(palettePanel).Right(this).Height(25).Apply(statusPanel);
			statusPanel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

			this.Controls.Add(statusPanel);
		}

		private void InitImage()
		{
			scrollPanel = new Panel();
			scrollPanel.AutoScroll = true;
			LayoutHelper.Below(toolStrip).RightOf(palettePanel).Right(this).Above(statusPanel).Apply(scrollPanel);
			scrollPanel.Anchor = LayoutHelper.AnchorAll;
			scrollPanel.BorderStyle = BorderStyle.Fixed3D;

			pictureBox = new PixelPictureBox();
			pictureBox.Dock = DockStyle.Fill;
			pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
			pictureBox.Cursor = Cursors.Hand;
			pictureBox.Click += new EventHandler(Image_OnClick);

			scrollPanel.Controls.Add(pictureBox);
			this.Controls.Add(scrollPanel);
		}

		private void InitHistory()
		{
			history = new History();
			RequestColorAction.DoFunc = new RequestColorAction.OnDo(RunColorRequest);
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

		private void Form_OnSaveAs(object sender, EventArgs e)
		{
			SaveAs();
		}

		private void Form_OnNarrowPalette(object sender, EventArgs e)
		{
			if(SwatchesPerRow == MIN_SWATCHES_PER_ROW) return;

			SwatchesPerRow--;
			DisplayPalette();
		}

		private void Form_OnWidenPalette(object sender, EventArgs e)
		{
			if(SwatchesPerRow == MAX_SWATCHES_PER_ROW) return;

			SwatchesPerRow++;
			DisplayPalette();
		}

		private void Form_OnClickColor(object sender, EventArgs e)
		{
			Panel colorPanel = sender as Panel;
			selectedColor = colorPanel.BackColor;

			foreach(Control child in swatchPanel.Controls)
			{
				if(child.BackColor == selectedColor)
				{
					child.BackgroundImage = iconManager.ICON_SELECTED_COLOR;
					child.BackgroundImageLayout = ImageLayout.Stretch;
				}
				else
				{
					child.BackgroundImage = null;
				}
			}
		}

		private void Form_OnUndo(object sender, EventArgs e)
		{
			history.Undo();
		}

		private void Form_OnRedo(object sender, EventArgs e)
		{
			history.Redo();
		}

		private void Form_OnNewPalette(object sender, EventArgs e)
		{
			using(EditPaletteDialog form = new EditPaletteDialog(null))
			{
				form.StartPosition = FormStartPosition.Manual;
				form.Location = new Point(this.Location.X + 30, this.Location.Y + 30);
				if(form.ShowDialog() == DialogResult.OK)
				{
					PaletteFullFilename = form.FullFilename;
					LoadPalette();
				}
			}
		}

		private void Form_OnLoadPalette(object sender, EventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Palette Files|*.ACO";
			openFileDialog.Title = "Select a Palette File";

			if(openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
			{
				return;
			}
			try
			{
				PaletteFullFilename = openFileDialog.FileName;
				LoadPalette();
			}
			catch(FileNotFoundException exception)
			{
				HandleError("Failed to open file.", exception);
			}
		}

		private void Form_OnEditPalette(object sender, EventArgs e)
		{
			using(EditPaletteDialog form = new EditPaletteDialog(PaletteFullFilename))
			{
				form.StartPosition = FormStartPosition.Manual;
				form.Location = new Point(this.Location.X + 30, this.Location.Y + 30);
				if(form.ShowDialog() == DialogResult.OK)
				{
					PaletteFullFilename = form.FullFilename;
					LoadPalette();
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

		private void Image_OnZoom1(object sender, EventArgs e)
		{
			if(!this.HasImage) return;

			InitZoom();
			double newImageScale = 1;
			UpdateZoomedImage(newImageScale);
		}

		private void Image_OnClick(object sender, EventArgs e)
		{
			if(!HasImage)
			{
				OpenFile();
				return;
			}

			if(selectedColor == null) return;

			Point pictureBoxPoint = pictureBox.PointToClient(new Point(MousePosition.X, MousePosition.Y));
			Point displayPoint;
			double thisScale = imageScale;
			if(pictureBox.SizeMode == PictureBoxSizeMode.Zoom)
			{
				float widthScale = (float)pictureBox.Width / (float)masterImage.Width;
				float heightScale = (float)pictureBox.Height / (float)masterImage.Height;
				thisScale = Math.Min(widthScale, heightScale);
				int widthDisplay = (int)(masterImage.Width * thisScale);
				int heightDisplay = (int)(masterImage.Height * thisScale);
				Point displayOriginPoint = new Point((pictureBox.Width - widthDisplay) / 2, (pictureBox.Height - heightDisplay) / 2); //point of image relative to picturebox
				displayPoint = new Point(pictureBoxPoint.X - displayOriginPoint.X, pictureBoxPoint.Y - displayOriginPoint.Y);
			}
			else
			{
				double hScrollPercentage = (double)scrollPanel.HorizontalScroll.Value / (double)(scrollPanel.HorizontalScroll.Maximum + 1 - scrollPanel.HorizontalScroll.LargeChange);
				double vScrollPercentage = (double)scrollPanel.VerticalScroll.Value / (double)(scrollPanel.VerticalScroll.Maximum + 1 - scrollPanel.VerticalScroll.LargeChange);
				int hOffscreen = zoomedImage.Width - pictureBox.Width;
				int vOffscreen = zoomedImage.Height - pictureBox.Height;

				//todo: scrollbar not in use

				displayPoint = new Point(pictureBoxPoint.X + (int)(hOffscreen * hScrollPercentage), pictureBoxPoint.Y + (int)(vOffscreen * vScrollPercentage));
			}
			Point masterImagePoint = new Point((int)(displayPoint.X / thisScale), (int)(displayPoint.Y / thisScale));
			if(masterImagePoint.X < 0 || masterImagePoint.X >= masterImage.Width) return;
			if(masterImagePoint.Y < 0 || masterImagePoint.Y >= masterImage.Height) return;

			Color currentColor = masterImage.GetPixel(masterImagePoint.X, masterImagePoint.Y);
			ColorAtPoint newColor = new ColorAtPoint(selectedColor.Value, masterImagePoint);
			RunColorRequest(newColor);
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

		private void Debug_OnShowNestedErrorMessage(object sender, EventArgs e)
		{
			try
			{
				throw new Exception("More detailed error message.", new Exception("Layer 1: sfjhskfjsdf", new Exception("Layer 2: 4572398573495873589")));
			}
			catch(Exception exception)
			{
				HandleError("Summary for user.", exception);
			}
		}

		private void Debug_OnShowWaitMessage(object sender, EventArgs e)
		{
			ShowWaitMessage("Processing request...");
		}
#endif
		#endregion

		private void RunColorRequest(ColorAtPoint cap)
		{
			requestColorQueue.Enqueue(cap);
			if(requestColorWorker == null)
			{
				requestColorWorker = new RequestColorWorker(requestColorQueue, masterImage, UpdateStatusText, OnRequestColorCompleted);
			}
			else if(!requestColorWorker.IsBusy)
			{
				requestColorWorker.Run(masterImage);
			}
		}

		private void OnRequestColorCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if(e.Error != null)
			{
				HandleError("Error occurred while applying color.", e.Error);
				return;
			}
			RequestColorWorkerResult result = (RequestColorWorkerResult)e.Result;
			if(!(result.NewWhite is ColorAtPoint_NoHistory) && !(result.PreviousWhite is ColorAtPoint_NoHistory))
			{
				history.Add(new RequestColorAction(result.NewWhite, result.PreviousWhite));
			}
			masterImage = result.Bitmap;
			UpdateZoomedImage(imageScale);
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

		private void SaveAs()
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog();
			saveFileDialog.Filter = "Image Files|*.BMP;*.PNG;*.JPG;*.JPEG;*.GIF;*.TIFF";
			saveFileDialog.Title = "Select an Image File";

			if(saveFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
			{
				return;
			}
			try
			{
				string extension = Path.GetExtension(saveFileDialog.FileName);
				switch(extension)
				{
					case ".bmp": masterImage.Save(saveFileDialog.FileName, ImageFormat.Bmp); break;
					case ".gif": masterImage.Save(saveFileDialog.FileName, ImageFormat.Gif); break;
					case ".jpg":
					case ".jpeg":
						masterImage.Save(saveFileDialog.FileName, ImageFormat.Jpeg); break;
					case ".png": masterImage.Save(saveFileDialog.FileName, ImageFormat.Png); break;
					case ".tiff": masterImage.Save(saveFileDialog.FileName, ImageFormat.Tiff); break;
					default: throw new Exception("File extension not supported: " + extension);
				}
			}
			catch(Exception exception)
			{
				HandleError("Failed to save file.", exception);
			}
		}

		private void UpdateMasterImage(string fullFilename)
		{
			saveImageFullFilename = fullFilename;
			masterImage = (Bitmap)Image.FromFile(fullFilename);
			history.Clear();
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
				if(imageScale != SCALE_FIT && imageScale > 1)
				{
					graphics.SmoothingMode = SmoothingMode.None;
					graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
				}
				graphics.DrawImage(masterImage, new Rectangle(0, 0, zoomedWidth, zoomedHeight));
			}
			pictureBox.Size = new Size(zoomedImage.Width, zoomedImage.Height);
			pictureBox.Image = zoomedImage;

			if(imageScale != SCALE_FIT && previousImageScale != newImageScale)
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

		private void CalculateValues()
		{
			HashSet<Color> colors = new HashSet<Color>();
			for(int x = 0; x < masterImage.Width; x++)
			{
				for(int y = 0; y < masterImage.Height; y++)
				{
					colors.Add(masterImage.GetPixel(x, y));
				}
			}
		}

		private void LoadPalette()
		{
			try
			{
				colorPalette = FormatACO.Load(PaletteFullFilename);
			}
			catch(Exception e)
			{
				HandleError("Cannot load palette: " + PaletteFullFilename, e);
				return;
			}
			DisplayPalette();
		}

		private void DisplayPalette()
		{
			int paletteWidth = (SwatchesPerRow * SwatchPanel.SWATCH_WIDTH) + (2 * palettePadding) + SwatchPanel.SCROLLBAR_WIDTH;

			palettePanel.Size = new Size(paletteWidth, palettePanel.Size.Height);
			swatchPanel.Size = new Size((SwatchesPerRow * SwatchPanel.SWATCH_WIDTH) + SwatchPanel.SCROLLBAR_WIDTH, swatchPanel.Size.Height);
			LayoutHelper.RightOf(palettePanel).Bottom(this).Right(this).Height(statusPanel.Height).Apply(statusPanel);
			LayoutHelper.RightOf(palettePanel).Below(toolStrip).Above(statusPanel).Right(this).Apply(scrollPanel);

			swatchPanel.DisplayColors(colorPalette);
		}

		private void UpdateStatusText(string text)
		{
			statusPanel.StatusText = text;
		}

		private void ShowWaitMessage(string message)
		{
			using(WaitDialog form = new WaitDialog(message))
			{
				form.ShowDialog();
			}
		}

		private void HandleError(string userMessage, Exception e)
		{
			List<string> message = new List<string>() { userMessage, "", "Exception:", e.Message, "", "Stack Trace:", e.StackTrace };
			while(e.InnerException != null)
			{
				e = e.InnerException;
				message.Add("");
				message.Add("============================");
				message.Add("Inner Exception:");
				message.Add(e.Message);
				message.Add("");
				message.Add("Stack Trace:");
				message.Add(e.StackTrace);
			}
			using(ErrorDialog form = new ErrorDialog("Error", message.ToArray()))
			{
				form.StartPosition = FormStartPosition.Manual;
				form.Location = new Point(this.Location.X + 30, this.Location.Y + 30);
				form.ShowDialog();
			}
		}
	}
}
