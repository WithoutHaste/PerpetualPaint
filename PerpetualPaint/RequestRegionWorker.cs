using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PerpetualPaintLibrary;
using WithoutHaste.Windows.GUI;

namespace PerpetualPaint
{
	public class RequestRegionWorker
	{
		private BackgroundWorker worker;

		public event RunWorkerCompletedEventHandler Completed;
		public event ProgressChangedEventHandler ProgressChanged;

		public bool IsBusy {
			get {
				return (worker != null && worker.IsBusy);
			}
		}

		//---------------------------------------------------

		public RequestRegionWorker()
		{
		}

		//---------------------------------------------------

		public void Run(Bitmap bitmap)
		{
			worker = new BackgroundWorker();
			worker.DoWork += new DoWorkEventHandler(FindRegionsA);
			//worker.DoWork += new DoWorkEventHandler(FindRegionsB);
			worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnCompleted);
			worker.ProgressChanged += ProgressChanged;
			worker.WorkerReportsProgress = true;
			worker.WorkerSupportsCancellation = true;
			worker.RunWorkerAsync(new PerpetualPaintLibrary.RequestRegionArgument(new Bitmap(bitmap)));
		}

		public void CancelAsync()
		{
			if(worker != null && worker.IsBusy)
			{
				worker.CancelAsync();
			}
		}

		//todo: starting with non-all-grayscale image, need to get pure color for each region

		private void FindRegionsA(object sender, DoWorkEventArgs e)
		{
			BackgroundWorker worker = (sender as BackgroundWorker);
			Bitmap bitmap = (e.Argument as RequestRegionArgument).Bitmap;
			int width = bitmap.Width;
			int height = bitmap.Height;

			worker.ReportProgress(0);
			float pixelsTotal = width * height;
			int progress = 0;
			List<ImageRegion> regions = new List<ImageRegion>();
			ImageRegion[] leadingEdge = new ImageRegion[width];
			for(int y = 0; y < height; y++)
			{
				for(int x = 0; x < width; x++)
				{
					if(worker.CancellationPending)
					{
						e.Cancel = true;
						worker.ReportProgress(100);
						return;
					}

					int pixelsDone = (y * height) + x - 1;
					int newProgress = Math.Min(100, (int)(100f * pixelsDone / pixelsTotal));
					if(newProgress > progress)
					{
						progress = newProgress;
						worker.ReportProgress(progress);
					}

					Point point = new Point(x, y);
					Color color = Utilities.GetPixel(bitmap, point);
					if(Utilities.ColorIsBlack(color))
					{
						leadingEdge[x] = null;
						continue;
					}

					if(CanJoinLeft(x, leadingEdge))
					{
						if(CanJoinUp(x, y, leadingEdge))
						{
							JoinLeftAndUp(regions, leadingEdge, point);
						}
						else
						{
							JoinLeftOnly(regions, leadingEdge, point);
						}
					}
					else if(CanJoinUp(x, y, leadingEdge))
					{
						JoinUpOnly(regions, leadingEdge, point);
					}
					else
					{
						ImageRegion region = new ImageRegion();
						region.Union(point);
						regions.Add(region);
						leadingEdge[x] = region;
					}
				}
			}
			worker.ReportProgress(100);
			e.Result = new RequestRegionResult(regions);
		}

		private bool CanJoinLeft(int x, ImageRegion[] leadingEdge)
		{
			return (x > 0 && leadingEdge[x - 1] != null);
		}

		private bool CanJoinUp(int x, int y, ImageRegion[] leadingEdge)
		{
			return (y > 0 && leadingEdge[x] != null);
		}

		/// <summary>
		/// Add point to region on left. Update leading edge to region on left.
		/// </summary>
		private void JoinLeftOnly(List<ImageRegion> regions, ImageRegion[] leadingEdge, Point point)
		{
			leadingEdge[point.X - 1].Union(point);
			leadingEdge[point.X] = leadingEdge[point.X - 1];
		}

		/// <summary>
		/// Add point to region above. Region above is already on leading edge.
		/// </summary>
		private void JoinUpOnly(List<ImageRegion> regions, ImageRegion[] leadingEdge, Point point)
		{
			leadingEdge[point.X].Union(point);
		}

		/// <summary>
		/// If these are already the same regions, just union in the new point.
		/// 
		/// Add point to region on left. Add region above to region on left.
		/// Update all leading edge references to "region above" to "region on left".
		/// </summary>
		private void JoinLeftAndUp(List<ImageRegion> regions, ImageRegion[] leadingEdge, Point point)
		{
			if(Object.ReferenceEquals(leadingEdge[point.X - 1], leadingEdge[point.X]))
			{
				leadingEdge[point.X - 1].Union(point);
				return;
			}

			ImageRegion discardRegion = leadingEdge[point.X];
			ImageRegion keepRegion = leadingEdge[point.X - 1];

			keepRegion.Union(point);
			keepRegion.Union(discardRegion);
			regions.Remove(discardRegion);
			for(int x = 0; x < leadingEdge.Length; x++)
			{
				if(leadingEdge[x] == discardRegion)
				{
					leadingEdge[x] = keepRegion;
				}
			}
		}

		private void OnCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			Completed?.Invoke(this, e);
		}
	}
}
