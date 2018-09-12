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
	public class MasterImage
	{
		private RequestRegionWorker regionWorker;
		private Bitmap bitmap;
		private string saveToFilename;
		private List<ImageRegion> regions = new List<ImageRegion>();

		public bool EditedSinceLastSave { get; private set; }
		private bool editedSinceLastCleanCopy = false;
		private Bitmap cleanGetCopy = null;
		/// <summary>
		/// Only GetPixel from a clean copy of bitmap to avoid locking conflicts.
		/// Only update the clean copy if a SetPixel has occurred.
		/// </summary>
		public Bitmap CleanGetCopy {
			get {
				if(cleanGetCopy == null || editedSinceLastCleanCopy)
				{
					cleanGetCopy = new Bitmap(bitmap);
					editedSinceLastCleanCopy = false;
				}
				return cleanGetCopy;
			}
		}

		public string SaveToFilename {
			get {
				return saveToFilename;
			}
			set {
				//todo: is this the expected behavior when setting SaveToFilename?
				LoadBitmap(value);
			}
		}

		public int Width { get { return CleanGetCopy.Width; } }
		public int Height { get { return CleanGetCopy.Height; } }

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

		public void LoadBitmap(string filename)
		{
			StatusChanged?.Invoke(this, new TextEventArgs("Prepping image..."));
			CancelLoad();

			bitmap = ImageHelper.SafeLoadBitmap(filename);
			editedSinceLastCleanCopy = true;
			EditedSinceLastSave = false;
			saveToFilename = filename;

			if(bitmap.Width == 0 && bitmap.Height == 0)
				throw new Exception("Cannot operate on a 0 by 0 bitmap.");
			regions.Clear();

			regionWorker = new RequestRegionWorker();
			regionWorker.Completed += new RunWorkerCompletedEventHandler(OnRequestRegionCompleted);
			regionWorker.ProgressChanged += new ProgressChangedEventHandler(Worker_OnProgressChanged);
			regionWorker.Run(bitmap);
		}

		public void Save()
		{
			SaveAs(SaveToFilename);
		}

		public void SaveAs(string filename)
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
				saveToFilename = filename;
				EditedSinceLastSave = false;
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

		private void Worker_OnProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			ProgressChanged?.Invoke(this, e);
		}

		public Color SetRegion(Point point, Color pureColor)
		{
			ImageRegion region = GetRegion(point);
			if(region == null)
				throw new Exception("Point not found in region: " + point);

			bool convertToGrayscale = (Utilities.ColorIsWhite(pureColor));
			Color oldPureColor = region.PureColor;
			//get all the colors at once so I'm not alternating between GetPixel/SetPixel
			List<ColorAtPoint> commands = new List<ColorAtPoint>();
			foreach(Point p in region.Points)
			{
				Color oldColor = GetPixel(p);
				Color adjustedColor = (convertToGrayscale ? Utilities.ColorToGrayscale(oldColor, oldPureColor) : Utilities.GrayscaleToColor(oldColor, pureColor));
				commands.Add(new ColorAtPoint(adjustedColor, p));
			}
			foreach(ColorAtPoint command in commands)
			{
				SetPixel(command.Point, command.Color);
			}
			region.PureColor = pureColor;
			return oldPureColor;
		}

		public void SetPixel(Point point, Color color)
		{
			bitmap.SetPixel(point.X, point.Y, color);
			editedSinceLastCleanCopy = true;
			EditedSinceLastSave = true;
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
