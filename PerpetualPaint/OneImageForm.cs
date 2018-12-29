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
	public class OneImageForm : Form
	{
		private ToolStrip toolStrip;
		private PalettePanel palettePanel;
		private Panel scrollPanel;
		private StatusPanel statusPanel;
		private PixelPictureBox pictureBox;

		private History history;

		private const int SCALE_FIT = -1;
		private const int MAX_IMAGE_DIMENSION = 9000;
		private const int DEFAULT_SWATCHES_PER_ROW = 3;
		private readonly Cursor ADD_COLOR_CURSOR = Cursors.Hand;
		private readonly Cursor DROPPER_CURSOR = Cursors.Cross;

		private MasterImage masterImage;
		private Bitmap zoomedImage;
		private double imageScale = 1; //0.5 means zoomedImage width is half that of masterImage
		private double zoomUnits = 0.2; //this is the percentage of change

		private int Setting_SwatchesPerRow {
			get {
				return Properties.Settings.Default.SwatchesPerRow;
			}
			set {
				Properties.Settings.Default.SwatchesPerRow = value;
				Properties.Settings.Default.Save();
			}
		}
		private Color? SelectedColor {
			get {
				return palettePanel.SelectedColor;
			}
		}

		private bool isDropperOperation;

		private Queue<ColorAtPoint> requestColorQueue = new Queue<ColorAtPoint>();
		private RequestColorWorker requestColorWorker;

		private bool Setting_FormFullScreen {
			get {
				return Properties.Settings.Default.FormFullScreen;
			}
			set {
				Properties.Settings.Default.FormFullScreen = value;
				Properties.Settings.Default.Save();
			}
		}

		private int Setting_FormNormalWidth {
			get {
				return Properties.Settings.Default.FormNormalWidth;
			}
			set {
				Properties.Settings.Default.FormNormalWidth = value;
				Properties.Settings.Default.Save();
			}
		}

		private int Setting_FormNormalHeight {
			get {
				return Properties.Settings.Default.FormNormalHeight;
			}
			set {
				Properties.Settings.Default.FormNormalHeight = value;
				Properties.Settings.Default.Save();
			}
		}

		private string Default_PaletteFileName {
			get {
				return Properties.Settings.Default.PaletteFullFilename;
			}
			set {
				Properties.Settings.Default.PaletteFullFilename = value;
				Properties.Settings.Default.Save();
			}
		}

		private FormWindowState previousFormWindowState;

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

		private CollectionForm collectionForm = null;

		//--------------------------------------------------

		public OneImageForm()
		{
			this.Text = "Perpetual Paint";
			if(Setting_FormFullScreen)
			{
				this.WindowState = FormWindowState.Maximized;
			}
			else
			{
				this.WindowState = FormWindowState.Normal;
				this.Width = Setting_FormNormalWidth;
				this.Height = Setting_FormNormalHeight;
			}
			previousFormWindowState = this.WindowState;
			this.Resize += new EventHandler(Form_Resize);
			this.FormClosing += new FormClosingEventHandler(Form_Closing);

			Application.ThreadException += new ThreadExceptionEventHandler(OnSystemException);

			InitMenus();
			InitTools();
			InitPalette();
			InitStatusPanel();
			InitImage();

			InitHistory();
		}

		#region Init

		private void InitMenus()
		{
			MenuItem fileMenu = new MenuItem("File");
			MenuItem newCollection = new MenuItem("New Collection", new EventHandler(Form_OnNewCollection));
			MenuItem openProject = new MenuItem("Open Project, Collection, or Image", new EventHandler(Form_OnOpenFile), Shortcut.CtrlO);
			MenuItem saveProject = new MenuItem("Save Project", new EventHandler(Form_OnSave), Shortcut.CtrlS);
			MenuItem saveAsProject = new MenuItem("Save Project As", new EventHandler(Form_OnSaveAs), Shortcut.F12);
			MenuItem exportImage = new MenuItem("Export Image", new EventHandler(Form_OnExport));
			MenuItem projectOptions = new MenuItem("Project Options", new EventHandler(Form_OnEditProjectOptions));
			fileMenu.MenuItems.Add(newCollection);
			fileMenu.MenuItems.Add(openProject);
			fileMenu.MenuItems.Add(saveProject);
			fileMenu.MenuItems.Add(saveAsProject);
			fileMenu.MenuItems.Add(exportImage);
			fileMenu.MenuItems.Add(projectOptions);

			MenuItem editMenu = new MenuItem("Edit");
			MenuItem undoAction = new MenuItem("Undo", new EventHandler(Form_OnUndo), Shortcut.CtrlZ);
			MenuItem redoAction = new MenuItem("Redo", new EventHandler(Form_OnRedo), Shortcut.CtrlY);
			editMenu.MenuItems.Add(undoAction);
			editMenu.MenuItems.Add(redoAction);

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
			toolStrip.Items.Add("Fit", IconManager.ZOOM_FIT, Image_OnFit);
			toolStrip.Items.Add("Zoom In", IconManager.ZOOM_IN, Image_OnZoomIn);
			toolStrip.Items.Add("Zoom Out", IconManager.ZOOM_OUT, Image_OnZoomOut);
			toolStrip.Items.Add("100%", IconManager.ZOOM_100, Image_OnZoom1);
			toolStrip.Items.Add(new ToolStripSeparator());
			toolStrip.Items.Add("Undo", IconManager.UNDO, Form_OnUndo);
			toolStrip.Items.Add("Redo", IconManager.REDO, Form_OnRedo);
			toolStrip.Items.Add(new ToolStripSeparator());
			toolStrip.Items.Add("Dropper", IconManager.DROPPER, Form_OnDropper);

			this.Controls.Add(toolStrip);
		}

		private void InitPalette()
		{
			palettePanel = new PalettePanel(Setting_SwatchesPerRow);
			LayoutHelper.Below(toolStrip).Left(this).Bottom(this).Width(palettePanel.Width).Apply(palettePanel);
			palettePanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;

			palettePanel.SettingChanged += new EventHandler(PalettePanel_OnSettingChanged);
			palettePanel.SizeChanged += new EventHandler(PalettePanel_OnSizeChanged);

			this.Controls.Add(palettePanel);

			palettePanel.Set(Default_PaletteFileName);
		}

		private void InitStatusPanel()
		{
			statusPanel = new StatusPanel();
			LayoutHelper.Bottom(this).RightOf(palettePanel).Right(this).Height(statusPanel.Height).Apply(statusPanel);
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
			pictureBox.Cursor = ADD_COLOR_CURSOR;
			pictureBox.Click += new EventHandler(Image_OnClick);

			scrollPanel.Controls.Add(pictureBox);
			this.Controls.Add(scrollPanel);
		}

		private void InitHistory()
		{
			history = new History();
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

		private void Form_Resize(object sender, EventArgs e)
		{
			if(previousFormWindowState == this.WindowState)
			{
				if(this.WindowState == FormWindowState.Normal)
				{
					Setting_FormNormalWidth = this.Width;
					Setting_FormNormalHeight = this.Height;
				}
				return;
			}

			switch(this.WindowState)
			{
				case FormWindowState.Maximized:
					Setting_FormFullScreen = true;
					break;
				case FormWindowState.Normal:
					Setting_FormFullScreen = false;
					this.Width = Setting_FormNormalWidth;
					this.Height = Setting_FormNormalHeight;
					break;
				case FormWindowState.Minimized:
					//no action
					break;
			}
			previousFormWindowState = this.WindowState;
		}

		private void Form_OnNewCollection(object sender, EventArgs e)
		{
			OpenCollection(null);
		}

		private void Form_OnOpenFile(object sender, EventArgs e)
		{
			bool continueOperation = PossiblySaveChangesBeforeClosingImage();
			if(!continueOperation)
			{
				return;
			}

			OpenFile();
		}

		private void Form_OnSave(object sender, EventArgs e)
		{
			if(masterImage == null)
			{
				MessageBox.Show("There is no image to save.", "Error Saving", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			Save();
		}

		private void Form_OnSaveAs(object sender, EventArgs e)
		{
			if(masterImage == null)
			{
				MessageBox.Show("There is no image to save.", "Error Saving", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			SaveAs();
		}

		private void Form_OnExport(object sender, EventArgs e)
		{
			if(masterImage == null)
			{
				MessageBox.Show("There is no image to export.", "Error Exporting", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			ExportAs();
		}

		private void Form_OnEditProjectOptions(object sender, EventArgs e)
		{
			if(masterImage == null)
			{
				MessageBox.Show("There is no project to edit.", "Error Editing Options", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			EditProjectOptions();
		}

		private void Form_OnUndo(object sender, EventArgs e)
		{
			history.Undo();
		}

		private void Form_OnRedo(object sender, EventArgs e)
		{
			history.Redo();
		}

		private void Form_OnDropper(object sender, EventArgs e)
		{
			isDropperOperation = !isDropperOperation;
			UpdateOperationMode();
		}

		private void Form_OnNewPalette(object sender, EventArgs e)
		{
			using(EditPaletteDialog form = new EditPaletteDialog())
			{
				form.StartPosition = FormStartPosition.Manual;
				form.Location = new Point(this.Location.X + 30, this.Location.Y + 30);
				if(form.ShowDialog() != DialogResult.OK)
					return;
				if(form.FullFilename == null)
					return;
				palettePanel.Set(form.FullFilename);
				if(masterImage != null)
				{
					masterImage.Project.UpdatePaletteOption(palettePanel.ColorPalette, palettePanel.PaletteFileName);
				}
			}
		}

		private void Form_OnLoadPalette(object sender, EventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Palette Files|*.aco;*.gpl;*.pal";
			openFileDialog.Title = "Select a Palette File";

			if(openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
			{
				return;
			}
			try
			{
				palettePanel.Set(openFileDialog.FileName);
			}
			catch(FileNotFoundException exception)
			{
				HandleError("Failed to open file.", exception);
			}
			if(masterImage != null)
			{
				masterImage.Project.UpdatePaletteOption(palettePanel.ColorPalette, palettePanel.PaletteFileName);
			}
		}

		private void Form_OnEditPalette(object sender, EventArgs e)
		{
			EditPaletteDialog dialog;
			if(masterImage != null && masterImage.Project.Config.PaletteOption == PPConfig.PaletteOptions.SaveFile)
			{
				dialog = new EditPaletteDialog(masterImage.Project.ColorPalette);
			}
			else
			{
				dialog = new EditPaletteDialog(palettePanel.PaletteFileName);
			}
			dialog.StartPosition = FormStartPosition.Manual;
			dialog.Location = new Point(this.Location.X + 30, this.Location.Y + 30);
			if(dialog.ShowDialog() != DialogResult.OK)
				return;

			if(masterImage != null && masterImage.Project.Config.PaletteOption == PPConfig.PaletteOptions.SaveFile)
			{
				palettePanel.Set(dialog.ColorPalette);
			}
			else
			{
				palettePanel.Set(dialog.FullFilename);
			}

			if(masterImage != null)
			{
				masterImage.Project.UpdatePaletteOption(palettePanel.ColorPalette, palettePanel.PaletteFileName);
			}
		}

		private void Form_UpdateStatusText(object sender, TextEventArgs e)
		{
			UpdateStatusText(e.Text);
		}

		private void Form_Closing(object sender, FormClosingEventArgs e)
		{
			bool continueOperation = PossiblySaveChangesBeforeClosingImage();
			if(!continueOperation)
			{
				e.Cancel = true;
			}
		}

		/// <summary>
		/// Checks with user if they want to save changes to image before closing it.
		/// </summary>
		/// <returns>True for continue operation; False for cancel operation.</returns>
		private bool PossiblySaveChangesBeforeClosingImage()
		{
			if(masterImage == null)
				return true;
			if(masterImage != null && !masterImage.Project.EditedSinceLastSave)
				return true;

			DialogResult result = MessageBox.Show("You are about to lose your changes.\nDo you want to save changes before closing the image?", "Save Before Closing", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
			switch(result)
			{
				case DialogResult.Cancel: return false;
				case DialogResult.No: return true;
				case DialogResult.Yes: 
					masterImage.Project.Save();
					return true;
			}
			return true;
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
			if(masterImage.IsBusy)
			{
				ShowWaitMessage("Please wait until the image is prepped.");
				return;
			}

			Point screenPoint = new Point(MousePosition.X, MousePosition.Y);
			Point masterImagePoint = ScreenPointToMasterImagePoint(screenPoint);
			if(!masterImage.InRange(masterImagePoint)) return;

			if(isDropperOperation)
			{
				Color pureColor = masterImage.PureColor(masterImagePoint);
				palettePanel.SelectedColor = pureColor;
				isDropperOperation = false;
				UpdateOperationMode();
			}

			if(SelectedColor == null) return;
			ColorAtPoint newColor = new ColorAtPoint(SelectedColor.Value, masterImagePoint);
			RunColorRequest(newColor);
		}

		private void Image_OnCancelLoad(object sender, EventArgs e)
		{
			masterImage.CancelLoad();
			masterImage = null;
			pictureBox.Image = null;
		}

		private void Collection_OnProjectSelected(object sender, ProjectEventArgs e)
		{
			UpdateMasterImage(project:e.Project);
			//todo: possibly load palette from collection, too
		}

		private void MasterImage_OnProjectEdited(object sender, ProjectEventArgs e)
		{
			collectionForm?.UpdateProject(e.Project);
		}

		private void PalettePanel_OnSettingChanged(object sender, EventArgs e)
		{
			PalettePanel panel = (sender as PalettePanel);
			Setting_SwatchesPerRow = panel.SwatchesPerRow;
			if(panel.PaletteFileName != null)
				Default_PaletteFileName = panel.PaletteFileName;
		}

		private void PalettePanel_OnSizeChanged(object sender, EventArgs e)
		{
			int delta = palettePanel.Right - statusPanel.Left;
			statusPanel.Left += delta;
			statusPanel.Width -= delta;

			delta = palettePanel.Right - scrollPanel.Left;
			scrollPanel.Left += delta;
			scrollPanel.Width -= delta;
		}

		private void OnProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			statusPanel.StatusProgress = e.ProgressPercentage;
		}

		private void OnSystemException(object sender, ThreadExceptionEventArgs e)
		{
			HandleError("Application error occurred.", e.Exception);
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

		private Point ScreenPointToMasterImagePoint(Point screenPoint)
		{
			Point pictureBoxPoint = pictureBox.PointToClient(screenPoint);
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
			return new Point((int)(displayPoint.X / thisScale), (int)(displayPoint.Y / thisScale));
		}

		private void UpdateOperationMode()
		{
			pictureBox.Cursor = (isDropperOperation) ? DROPPER_CURSOR : ADD_COLOR_CURSOR;
		}

		private void OnHistoryColorRequest(object sender, ColorEventArgs cap)
		{
			RunColorRequest(cap.ColorAtPoint);
		}

		private void RunColorRequest(ColorAtPoint cap)
		{
			requestColorQueue.Enqueue(cap);
			if(requestColorWorker == null)
			{
				requestColorWorker = new RequestColorWorker(requestColorQueue, masterImage, OnRequestColorCompleted);
				requestColorWorker.UpdateStatusText += new TextEventHandler(Form_UpdateStatusText);
				requestColorWorker.Run();
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
				RequestColorAction r = new RequestColorAction(result.NewWhite, result.PreviousWhite);
				r.Action += new ColorEventHandler(OnHistoryColorRequest);
				history.Add(r);
			}
			UpdateZoomedImage(imageScale);
		}

		private void OpenFile()
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Supported Files|*" + PPProject.PROJECT_EXTENSION + ";*" + PPCollection.COLLECTION_EXTENSION + ";*.BMP;*.PNG;*.JPG;*.JPEG;*.GIF;*.TIFF";
			openFileDialog.Title = "Open Project, Collection, or Image";

			if(openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
			{
				return;
			}

			if(Path.GetExtension(openFileDialog.FileName) == PPCollection.COLLECTION_EXTENSION)
			{
				OpenCollection(openFileDialog.FileName);
				return;
			}

			try
			{
				statusPanel.ClearCancels();
				statusPanel.Cancel += new EventHandler(Image_OnCancelLoad);
				UpdateMasterImage(openFileDialog.FileName);
			}
			catch(FileNotFoundException exception)
			{
				HandleError("Failed to open file.", exception);
			}
			switch(masterImage.Project.Config.PaletteOption)
			{
				case PPConfig.PaletteOptions.SaveFile:
					palettePanel.Set(masterImage.Project.ColorPalette);
					break;
				case PPConfig.PaletteOptions.SaveFileName:
					palettePanel.Set(masterImage.Project.Config.PaletteFileName);
					break;
			}
		}

		private void OpenCollection(string fileName)
		{
			if(collectionForm == null || collectionForm.IsDisposed)
			{
				collectionForm = new CollectionForm(fileName);
				collectionForm.ProjectSelected += new ProjectEventHandler(Collection_OnProjectSelected);
				collectionForm.Show(this);
			}
			else
			{
				collectionForm.OpenCollection(fileName);
			}
		}

		private void ExportAs()
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog();
			saveFileDialog.Filter = "Image Files|*.BMP;*.PNG;*.JPG;*.JPEG;*.GIF;*.TIFF";
			saveFileDialog.Title = "Export Image As";
			if(masterImage.Project.SaveToFileName != null)
			{
				saveFileDialog.FileName = Path.GetFileNameWithoutExtension(masterImage.Project.SaveToFileName);
			}

			if(saveFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
			{
				return;
			}
			masterImage.ExportAs(saveFileDialog.FileName);
		}

		private void SaveAs()
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog();
			saveFileDialog.Filter = "Project Files|*" + PPProject.PROJECT_EXTENSION;
			saveFileDialog.Title = "Save Project As";
			if(saveFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
			{
				return;
			}
			if(!EditProjectOptions())
			{
				return;
			}

			masterImage.Project.SaveAs(saveFileDialog.FileName);
		}

		private void Save()
		{
			if(Path.GetExtension(masterImage.Project.SaveToFileName) != PPProject.PROJECT_EXTENSION)
			{
				SaveAs();
			}
			else
			{
				masterImage.Project.Save();
			}
		}

		/// <summary>
		/// Returns true if the operation is OK, returns false if the operation is Canceled.
		/// </summary>
		private bool EditProjectOptions()
		{
			using(ProjectOptionsDialog form = new ProjectOptionsDialog(masterImage.Project.Config))
			{
				if(form.ShowDialog(this) != DialogResult.OK)
					return false;
				masterImage.Project.SetPaletteOption(form.PaletteOption, palettePanel.ColorPalette, palettePanel.PaletteFileName);
			}
			return true;
		}

		/// <summary>
		/// Initialize master image (if needed) and update it with the new file.
		/// Can either specify a file name, or provide the full project.
		/// </summary>
		private void UpdateMasterImage(string fileName = null, PPProject project = null)
		{
			if(masterImage == null)
			{
				masterImage = new MasterImage();
				masterImage.ProgressChanged += new ProgressChangedEventHandler(OnProgressChanged);
				masterImage.StatusChanged += new TextEventHandler(Form_UpdateStatusText);
				masterImage.ProjectEdited += new ProjectEventHandler(MasterImage_OnProjectEdited);
			}

			if(fileName != null) masterImage.Load(fileName);
			else masterImage.SetProject(project);

			history.Clear();
			UpdateZoomedImage(SCALE_FIT);
		}

		private void UpdateZoomedImage(double newImageScale)
		{
			this.SuspendLayout();

			bool horizontalScrollAtMax = (scrollPanel.HorizontalScroll.Maximum == scrollPanel.HorizontalScroll.Value + scrollPanel.HorizontalScroll.LargeChange - 1);
			bool verticalScrollAtMax = (scrollPanel.VerticalScroll.Maximum == scrollPanel.VerticalScroll.Value + scrollPanel.VerticalScroll.LargeChange - 1);

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
				graphics.DrawImage(masterImage.CleanGetCopy, new Rectangle(0, 0, zoomedWidth, zoomedHeight));
			}
			pictureBox.Size = new Size(zoomedImage.Width, zoomedImage.Height);
			pictureBox.Image = zoomedImage;

			if(imageScale != SCALE_FIT && previousImageScale != newImageScale)
			{
				UpdateScrollBars(centerPoint, horizontalScrollAtMax, verticalScrollAtMax);
			}

			this.ResumeLayout();
		}

		private void UpdateScrollBars(Point masterImageCenterPoint, bool horizontalScrollAtMax, bool verticalScrollAtMax)
		{
			int x = (int)((masterImageCenterPoint.X * imageScale) - (scrollPanel.ClientSize.Width / 2));
			if(scrollPanel.HorizontalScroll.Value == 0)
			{
				x = 0;
			}
			else if(horizontalScrollAtMax)
			{
				x = pictureBox.Width;
			}

			int y = (int)((masterImageCenterPoint.Y * imageScale) - (scrollPanel.ClientSize.Height / 2));
			if(scrollPanel.VerticalScroll.Value == 0)
			{
				y = 0;
			}
			else if(verticalScrollAtMax)
			{
				y = pictureBox.Height;
			}

			scrollPanel.AutoScrollPosition = new Point(x, y);
		}

		private void UpdateStatusText(string text)
		{
			statusPanel.StatusText = text;
		}

		private void ShowWaitMessage(string message)
		{
			using(WaitDialog form = new WaitDialog(message))
			{
				form.ShowDialog(this);
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
				form.ShowDialog(this);
			}
		}
	}
}
