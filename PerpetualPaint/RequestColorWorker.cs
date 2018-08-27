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
		private Bitmap bitmap;
		private UpdateStatusText updateStatusTextFunc;

		public delegate void UpdateStatusText(string text);

		public bool IsBusy {
			get {
				if(worker == null)
					return false;
				return worker.IsBusy;
			}
		}

		public RequestColorWorker(Queue<ColorAtPoint> queue, Bitmap bitmap, UpdateStatusText updateStatusTextFunc, RunWorkerCompletedEventHandler completedEventHandler = null)
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

			//todo: is this the right pattern? should it be adding to an event on OneImageForm?
			this.updateStatusTextFunc = updateStatusTextFunc;

			Run(bitmap);
		}

		public void Run(Bitmap bitmap)
		{
			this.bitmap = new Bitmap(bitmap);
			Run();
		}

		private void Run()
		{
			if(worker == null)
				throw new Exception("Worker cannot be run before it exists."); //todo specific exception
			if(worker.IsBusy)
				return;
			worker.RunWorkerAsync();
			updateStatusTextFunc("Applying color...");
		}

		private void OnCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if(e.Error != null)
			{
				updateStatusTextFunc("Error occurred.");
				return;
			}
			if(queue.Count > 0)
			{
				Run();
			}
			else
			{
				updateStatusTextFunc("");
			}
		}

		//todo accept cancellation from wait form
		//todo update main form status bar with "Processing request 2 of 6..."
		//todo on cancellation, still update main form with the last successfully completed coloration

		#region Color Bitmap

		private void ColorPixel(object sender, DoWorkEventArgs e)
		{
			if(queue.Count == 0)
				return;

			ColorAtPoint request = queue.Dequeue();
			Color color = request.Color;
			Point point = request.Point;

			Color oldWhite = Color.White;
			if(!PerpetualPaintLibrary.Utilities.ColorIsGrayscale(bitmap.GetPixel(point.X, point.Y)))
			{
				oldWhite = ConvertRegionToGrayscale(point);
			}

			ConvertRegionToColor(color, point);
			e.Result = new RequestColorWorkerResult(new Bitmap(bitmap), request, request.ChangeColor(oldWhite));
		}

		private void ConvertRegionToColor(Color color, Point point)
		{
			HashSet<Point> done = new HashSet<Point>();
			List<Point> todo = new List<Point>() { point };
			while(todo.Count > 0)
			{
				Point p = todo.First();
				todo.RemoveAt(0);
				if(done.Contains(p))
				{
					continue;
				}
				done.Add(p);

				Color oldColor = bitmap.GetPixel(p.X, p.Y);
				if(PerpetualPaintLibrary.Utilities.ColorIsBlack(oldColor))
					continue;
				if(!PerpetualPaintLibrary.Utilities.ColorIsGrayscale(oldColor))
					continue;

				Color adjustedColor = PerpetualPaintLibrary.Utilities.GrayscaleToColor(oldColor, color);
				bitmap.SetPixel(p.X, p.Y, adjustedColor);

				Point left = new Point(p.X - 1, p.Y);
				Point right = new Point(p.X + 1, p.Y);
				Point up = new Point(p.X, p.Y - 1);
				Point down = new Point(p.X, p.Y + 1);
				if(PerpetualPaintLibrary.Utilities.PointInRange(bitmap, left) && !PointInList(todo, left)) todo.Add(left);
				if(PerpetualPaintLibrary.Utilities.PointInRange(bitmap, right) && !PointInList(todo, right)) todo.Add(right);
				if(PerpetualPaintLibrary.Utilities.PointInRange(bitmap, up) && !PointInList(todo, up)) todo.Add(up);
				if(PerpetualPaintLibrary.Utilities.PointInRange(bitmap, down) && !PointInList(todo, down)) todo.Add(down);
			}
		}

		/// <summary>
		/// Convert region from color to grayscale.
		/// </summary>
		/// <param name="point">Any point in a black-bounded region.</param>
		/// <returns>The previous "white" color.</returns>
		private Color ConvertRegionToGrayscale(Point point)
		{
			HashSet<ColorAtPoint> inRegion = PerpetualPaintLibrary.Utilities.FindRegion(bitmap, point);
			Color pureColor = PerpetualPaintLibrary.Utilities.FindPalestColor(inRegion);
			foreach(ColorAtPoint p in inRegion)
			{
				Color adjustedColor = PerpetualPaintLibrary.Utilities.ColorToGrayscale(p.Color, pureColor);
				bitmap.SetPixel(p.Point.X, p.Point.Y, adjustedColor);
			}
			return pureColor;
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
