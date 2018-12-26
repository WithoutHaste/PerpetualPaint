using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PerpetualPaintLibrary;
using WithoutHaste.Windows.GUI;

namespace PerpetualPaint
{
	/// <summary>
	/// The image being edited and displayed.
	/// </summary>
	public class MasterImage : PPProject
	{
		private RequestRegionWorker regionWorker;
		private List<ImageRegion> regions = new List<ImageRegion>();

		private bool editedSinceLastCleanCopy = false;
		private Bitmap cleanGetCopy = null;

		//todo: this may be a place for a wrapper class
		//inherit from bitmap, override the get/set pixels
		//same behaviour, except GetPixel will work like this CleanGetCopy logic, and SetPixel will reset that flag

		/// <summary>
		/// Only GetPixel from a clean copy of bitmap to avoid locking conflicts.
		/// Only update the clean copy if a SetPixel has occurred.
		/// </summary>
		public Bitmap CleanGetCopy {
			get {
				if(cleanGetCopy == null || editedSinceLastCleanCopy)
				{
					cleanGetCopy = new Bitmap(this.ColorBitmap);
					editedSinceLastCleanCopy = false;
				}
				return cleanGetCopy;
			}
		}

		public int Width { get { return this.GreyscaleBitmap.Width; } }
		public int Height { get { return this.GreyscaleBitmap.Height; } }

		public event ProgressChangedEventHandler ProgressChanged;
		public event TextEventHandler StatusChanged;

		public bool IsBusy {
			get {
				return (regionWorker != null && regionWorker.IsBusy);
			}
		}

		//---------------------------------------------------

		public MasterImage()
		{
		}

		//---------------------------------------------------

		public void Load(string filename)
		{
			StatusChanged?.Invoke(this, new TextEventArgs("Prepping image..."));
			CancelLoad();

			bool runRegionsOnColorBitmap = false;
			if(Path.GetExtension(filename) == PROJECT_EXTENSION)
			{
				LoadProject(filename);
			}
			else
			{
				bool isGreyscaleImage = LoadImage(filename);
				runRegionsOnColorBitmap = !isGreyscaleImage;
			}
			editedSinceLastCleanCopy = true;

			regions.Clear();

			regionWorker = new RequestRegionWorker();
			regionWorker.Completed += new RunWorkerCompletedEventHandler(OnRequestRegionCompleted);
			regionWorker.ProgressChanged += new ProgressChangedEventHandler(Worker_OnProgressChanged);
			if(runRegionsOnColorBitmap)
			{
				regionWorker.Run(this.ColorBitmap);
				this.GreyscaleBitmap = Utilities.GetGreyscaleOfBitmap(this.ColorBitmap, this.regions); //todo: try may be a timing issue here, where the image is still being worked on but the user is allowed to interact with it
			}
			else
			{
				regionWorker.Run(this.GreyscaleBitmap);
			}
		}

		/// <summary>
		/// Export colored bitmap to any normal image format.
		/// </summary>
		public void ExportAs(string filename)
		{
			try
			{
				string extension = Path.GetExtension(filename).ToLower();
				ImageFormat imageFormat = ImageFormat.Bmp;
				switch(extension)
				{
					case ".bmp": imageFormat = ImageFormat.Bmp; break;
					case ".gif": imageFormat = ImageFormat.Gif; break;
					case ".jpg":
					case ".jpeg": imageFormat = ImageFormat.Jpeg; break;
					case ".png": imageFormat = ImageFormat.Png; break;
					case ".tiff": imageFormat = ImageFormat.Tiff; break;
					default: throw new Exception("File extension not supported: " + extension);
				}
				CleanGetCopy.Save(filename, imageFormat);
			}
			catch(Exception exception)
			{
				if(CleanGetCopy.Width > 65500 || CleanGetCopy.Height > 65500)
				{
					throw new Exception("Failed to save file. Image is wider or taller than maximum GDI+ can handle: 65,500 pixels.", exception);
				}
				throw exception;
			}
		}

		public void SetPaletteOption(PPConfig.PaletteOptions paletteOption, WithoutHaste.Drawing.Colors.ColorPalette colorPalette = null, string paletteFileName = null)
		{
			Config.PaletteOption = paletteOption;
			switch(Config.PaletteOption)
			{
				case PPConfig.PaletteOptions.SaveNothing:
					Config.PaletteFileName = null;
					ColorPalette = null;
					break;
				case PPConfig.PaletteOptions.SaveFile:
					Config.PaletteFileName = null;
					ColorPalette = colorPalette;
					break;
				case PPConfig.PaletteOptions.SaveFileName:
					Config.PaletteFileName = paletteFileName;
					ColorPalette = null;
					break;
			}
			EditedSinceLastSave = true;
		}

		public void UpdatePaletteOption(WithoutHaste.Drawing.Colors.ColorPalette colorPalette = null, string paletteFileName = null)
		{
			switch(Config.PaletteOption)
			{
				case PPConfig.PaletteOptions.SaveNothing:
					return;
				case PPConfig.PaletteOptions.SaveFile:
					ColorPalette = colorPalette;
					break;
				case PPConfig.PaletteOptions.SaveFileName:
					Config.PaletteFileName = paletteFileName;
					break;
			}
			EditedSinceLastSave = true;
		}

		private void Worker_OnProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			ProgressChanged?.Invoke(this, e);
		}

		//todo: is anything using the return value from SetRegion?

		/// <summary>
		/// Color in the selected region.
		/// If the point is not in any region, do nothing.
		/// </summary>
		public Color? SetRegion(Point point, Color pureColor)
		{
			ImageRegion region = GetRegion(point);
			if(region == null)
				return null; //point was not in a colorable region
			Color oldPureColor = Utilities.SetRegion(this.GreyscaleBitmap, this.ColorBitmap, region, pureColor);
			editedSinceLastCleanCopy = true;
			EditedSinceLastSave = true;
			return oldPureColor;
		}

		public Color GetPixel(Point point)
		{
			return CleanGetCopy.GetPixel(point.X, point.Y);
		}

		public bool InRange(Point point)
		{
			return (point.X >= 0 && point.X < Width && point.Y >= 0 && point.Y < Height);
		}

		public Color PureColor(Point point)
		{
			WaitTillComplete();
			ImageRegion region = GetRegion(point);
			if(region == null)
				throw new Exception("Point not in any region: " + point);
			return region.PureColor;
		}

		public ImageRegion GetRegion(Point point)
		{
			if(regionWorker != null && regionWorker.IsBusy)
				throw new Exception("Don't request regions until processing is complete.");
			return regions.FirstOrDefault(r => r.Contains(point));
		}

		private void WaitTillComplete()
		{
			if(regionWorker == null || !regionWorker.IsBusy)
				return;

			while(regionWorker.IsBusy)
			{
				Application.DoEvents();
			}
		}

		private void OnRequestRegionCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if(e.Error != null)
			{
				throw e.Error;
			}
			if(e.Cancelled)
			{
				StatusChanged?.Invoke(this, new TextEventArgs("Open image cancelled."));
				ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(100, null));
				return;
			}
			regions = (e.Result as RequestRegionResult).Regions;
			StatusChanged?.Invoke(this, new TextEventArgs("Image ready."));
			if(regions.Count == 0)
				throw new Exception("Entire image is in 'black' range.");
		}

		public void CancelLoad()
		{
			if(!IsBusy) return;
			regionWorker.CancelAsync();
		}
	}
}
