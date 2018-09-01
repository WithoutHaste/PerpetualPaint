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
	public class RequestRegionWorker
	{
		private BackgroundWorker worker;
		private Bitmap bitmap;
		private List<Region> regions = new List<Region>();
		private RunWorkerCompletedEventHandler outerRunWorkerCompletedEventHandler;
		private ProgressChangedEventHandler progressChangedEventHandler;

		public bool IsBusy {
			get {
				return (worker != null && worker.IsBusy);
			}
		}

		//---------------------------------------------------

		public RequestRegionWorker(Bitmap bitmap, RunWorkerCompletedEventHandler completedEventHandler = null, ProgressChangedEventHandler progressChangedEventHandler = null)
		{
			this.bitmap = new Bitmap(bitmap);
			this.outerRunWorkerCompletedEventHandler = completedEventHandler;
			this.progressChangedEventHandler = progressChangedEventHandler;

			worker = new BackgroundWorker();
			worker.DoWork += new DoWorkEventHandler(FindRegions);
			worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnCompleted);
			if(outerRunWorkerCompletedEventHandler != null)
			{
				worker.RunWorkerCompleted += outerRunWorkerCompletedEventHandler;
			}
			if(progressChangedEventHandler != null)
			{
				worker.ProgressChanged += progressChangedEventHandler;
			}
			worker.WorkerReportsProgress = true;
			worker.WorkerSupportsCancellation = true;
			worker.RunWorkerAsync();
		}

		//---------------------------------------------------

		public void CancelAsync()
		{
			if(worker != null && worker.IsBusy)
			{
				worker.CancelAsync();
			}
		}

		private void FindRegions(object sender, DoWorkEventArgs e)
		{
			worker.ReportProgress(0);
			float pixelsDone = 0;
			float pixelsTotal = bitmap.Width * bitmap.Height;
			for(int x = 0; x < bitmap.Width; x++)
			{
				for(int y = 0; y < bitmap.Height; y++)
				{
					if(worker.CancellationPending)
					{
						e.Cancel = true;
						worker.ReportProgress(100);
						return;
					}

					Point p = new Point(x, y);
					if(regions.Any(r => r.Contains(p)))
						continue;
					HashSet<ColorAtPoint> region = Utilities.FindRegion(bitmap, p);
					if(region.Count == 0) //blacks
						continue;
					regions.Add(new Region(region));
					pixelsDone += region.Count;
					int progress = (int)(100f * (pixelsDone / pixelsTotal));
					worker.ReportProgress(progress);
				}
			}
			worker.ReportProgress(100);
			e.Result = new RequestRegionResult(regions);
		}

		private void OnCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
		}
	}
}
