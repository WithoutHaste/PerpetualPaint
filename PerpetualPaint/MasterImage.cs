using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
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
		private List<Region> regions = new List<Region>();

		//only GetPixel from a clean copy to avoid locking conflicts
		//only update the clean copy if a SetPixel has occurred
		private bool setHasOccurred = false;
		private Bitmap cleanGetCopy = null;
		public Bitmap CleanGetCopy {
			get {
				if(cleanGetCopy == null || setHasOccurred)
				{
					cleanGetCopy = new Bitmap(bitmap);
					setHasOccurred = false;
				}
				return cleanGetCopy;
			}
		}

		public string SaveToFilename {
			get {
				return saveToFilename;
			}
			set {
				LoadBitmap(value);
			}
		}

		public int Width { get { return CleanGetCopy.Width; } }
		public int Height { get { return CleanGetCopy.Height; } }

		public event ProgressChangedEventHandler OnProgressChanged;
		public event TextEventHandler OnStatusChanged;

		public bool IsBusy {
			get {
				return (regionWorker != null && regionWorker.IsBusy);
			}
		}

		//---------------------------------------------------

		public MasterImage(string filename, ProgressChangedEventHandler progressChangedEventHandler, TextEventHandler statusChangedEventHandler)
		{
			OnProgressChanged += progressChangedEventHandler;
			OnStatusChanged += statusChangedEventHandler;
			LoadBitmap(filename);
		}

		//---------------------------------------------------

		public void LoadBitmap(string filename)
		{
			OnStatusChanged?.Invoke(this, new TextEventArgs("Prepping image..."));
			CancelLoad();

			bitmap = ImageHelper.SafeLoadBitmap(filename);
			setHasOccurred = true;
			saveToFilename = filename;

			if(bitmap.Width == 0 && bitmap.Height == 0)
				throw new Exception("Cannot operate on a 0 by 0 bitmap.");
			regions.Clear();
			regionWorker = new RequestRegionWorker(bitmap, OnRequestRegionCompleted, new ProgressChangedEventHandler(Worker_OnProgressChanged));
		}

		private void Worker_OnProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			OnProgressChanged?.Invoke(this, e);
		}

		public Color SetRegion(Point point, Color pureColor)
		{
			Region region = GetRegion(point);
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
			setHasOccurred = true;
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
			Region region = GetRegion(point);
			if(region == null)
				throw new Exception("Point not in any region: " + point);
			return region.PureColor;
		}

		public Region GetRegion(Point point)
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
				OnStatusChanged?.Invoke(this, new TextEventArgs("Open image cancelled."));
				OnProgressChanged?.Invoke(this, new ProgressChangedEventArgs(100, null));
				return;
			}
			regions = (e.Result as RequestRegionResult).Regions;
			OnStatusChanged?.Invoke(this, new TextEventArgs("Image ready."));
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
