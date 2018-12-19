using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WithoutHaste.Drawing.Colors;
using PerpetualPaintLibrary;

namespace PerpetualPaint
{
	public class RequestColorWorker
	{
		private BackgroundWorker worker;
		private Queue<ColorAtPoint> queue;
		private MasterImage masterImage;

		public event TextEventHandler UpdateStatusText;

		public bool IsBusy {
			get {
				if(worker == null)
					return false;
				return worker.IsBusy;
			}
		}

		public RequestColorWorker(Queue<ColorAtPoint> queue, MasterImage masterImageWorker, RunWorkerCompletedEventHandler completedEventHandler = null)
		{
			this.queue = queue;
			worker = new BackgroundWorker();
			worker.DoWork += new DoWorkEventHandler(ColorPixel);
			worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnCompleted);
			if(completedEventHandler != null)
			{
				worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(completedEventHandler);
			}
			worker.WorkerSupportsCancellation = true;
			this.masterImage = masterImageWorker;
		}

		public void Run(MasterImage masterImageWorker)
		{
			this.masterImage = masterImageWorker;
			Run();
		}

		public void Run()
		{
			if(worker == null)
				throw new Exception("Worker cannot be run before it exists."); //todo specific exception
			if(worker.IsBusy)
				return;
			worker.RunWorkerAsync();
			UpdateStatusText?.Invoke(this, new TextEventArgs("Applying color..."));
		}

		private void OnCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if(e.Error != null)
			{
				UpdateStatusText?.Invoke(this, new TextEventArgs("Error occurred."));
				return;
			}
			if(queue.Count > 0)
			{
				Run();
			}
			else
			{
				UpdateStatusText?.Invoke(this, new TextEventArgs(""));
			}
		}

		//todo accept cancellation from wait form
		//todo update main form status bar with "Processing request 2 of 6..."
		//todo on cancellation, still update main form with the last successfully completed coloration

		#region Color Bitmap

		/// <summary>
		/// The main worker method: colors in a region of the image.
		/// </summary>
		private void ColorPixel(object sender, DoWorkEventArgs e)
		{
			if(queue.Count == 0)
				return;

			ColorAtPoint request = queue.Dequeue();
			Color color = request.Color;
			Point point = request.Point;

			Color oldPureColor = masterImage.CleanGetCopy.GetPixel(point.X, point.Y);
			ConvertRegionToColor(color, point);
			//todo: refactor for new design - in master image keep partially updated image separate from last complete version
			//todo cont: basically, a buffered version to edit on, and a completed version that can always be displayed
			e.Result = new RequestColorWorkerResult(masterImage.CleanGetCopy, request, request.ChangeColor(oldPureColor));
		}

		private void ConvertRegionToColor(Color pureColor, Point point)
		{
			masterImage.SetRegion(point, pureColor);
		}
		
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
				
		#endregion

	}
}
