﻿using System;
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
		private Bitmap bitmap;

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
			//worker.DoWork += new DoWorkEventHandler(FindRegionsA);
			worker.DoWork += new DoWorkEventHandler(FindRegionsB);
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
						leadingEdge[x - 1].Union(point);
						if(CanJoinUp(x, y, leadingEdge))
						{
							if(leadingEdge[x - 1] != leadingEdge[x])
							{
								leadingEdge[x - 1].Union(leadingEdge[x]);
								regions.Remove(leadingEdge[x]);
								leadingEdge[x] = leadingEdge[x - 1];
							}
						}
						leadingEdge[x] = leadingEdge[x - 1];
					}
					else if(CanJoinUp(x, y, leadingEdge))
					{
						leadingEdge[x].Union(point);
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
		
		private void FindRegionsB(object sender, DoWorkEventArgs e)
		{
			BackgroundWorker worker = (sender as BackgroundWorker);
			Bitmap bitmap = (e.Argument as RequestRegionArgument).Bitmap;

			worker.ReportProgress(0);
			List<ImageRegion> regions = new List<ImageRegion>();
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
					regions.Add(new ImageRegion(region));
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
			Completed?.Invoke(this, e);
		}
	}
}
