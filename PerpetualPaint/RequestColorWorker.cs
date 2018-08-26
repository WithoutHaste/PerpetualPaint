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
				if(PointInRange(left) && !PointInList(todo, left)) todo.Add(left);
				if(PointInRange(right) && !PointInList(todo, right)) todo.Add(right);
				if(PointInRange(up) && !PointInList(todo, up)) todo.Add(up);
				if(PointInRange(down) && !PointInList(todo, down)) todo.Add(down);
			}
		}

		/// <summary>
		/// Convert region from color to grayscale.
		/// </summary>
		/// <param name="point">Any point in a black-bounded region.</param>
		/// <returns>The previous "white" color.</returns>
		private Color ConvertRegionToGrayscale(Point point)
		{
			HashSet<ColorAtPoint> inRegion = FindRegion(point);
			Color pureColor = FindPalestColor(inRegion);
			foreach(ColorAtPoint p in inRegion)
			{
				Color adjustedColor = PerpetualPaintLibrary.Utilities.ColorToGrayscale(p.Color, pureColor);
				bitmap.SetPixel(p.Point.X, p.Point.Y, adjustedColor);
			}
			return pureColor;
		}

		/// <summary>
		/// Find region as bounded by "black".
		/// </summary>
		private HashSet<ColorAtPoint> FindRegion(Point startPoint)
		{
			HashSet<ColorAtPoint> inRegion = new HashSet<ColorAtPoint>();
			Bitmap localBitmap = new Bitmap(bitmap);

			//todo: why is this using up memory on small color change on cat?
			HashSet<ColorAtPoint> todo = new HashSet<ColorAtPoint>() { new ColorAtPoint(PerpetualPaintLibrary.Utilities.GetPixel(localBitmap, startPoint), startPoint) };
			while(todo.Count > 0)
			{
				ColorAtPoint p = todo.First();
				todo.Remove(p);
				if(inRegion.Contains(p))
					continue;

				if(PerpetualPaintLibrary.Utilities.ColorIsBlack(p.Color))
					continue;

				inRegion.Add(p);
				//todo: duplicate code
				Point left = new Point(p.Point.X - 1, p.Point.Y);
				Point right = new Point(p.Point.X + 1, p.Point.Y);
				Point up = new Point(p.Point.X, p.Point.Y - 1);
				Point down = new Point(p.Point.X, p.Point.Y + 1);
				if(PointInRange(left))
				{
					Color leftColor = PerpetualPaintLibrary.Utilities.GetPixel(localBitmap, left);
					ColorAtPoint leftCAP = new ColorAtPoint(leftColor, left);
					if(!todo.Contains(leftCAP))
					{
						todo.Add(leftCAP);
					}
				}
				if(PointInRange(right))
				{
					Color rightColor = PerpetualPaintLibrary.Utilities.GetPixel(localBitmap, right);
					ColorAtPoint rightCAP = new ColorAtPoint(rightColor, right);
					if(!todo.Contains(rightCAP))
					{
						todo.Add(rightCAP);
					}
				}
				if(PointInRange(up))
				{
					Color upColor = PerpetualPaintLibrary.Utilities.GetPixel(localBitmap, up);
					ColorAtPoint upCAP = new ColorAtPoint(upColor, up);
					if(!todo.Contains(upCAP))
					{
						todo.Add(upCAP);
					}
				}
				if(PointInRange(down))
				{
					Color downColor = PerpetualPaintLibrary.Utilities.GetPixel(localBitmap, down);
					ColorAtPoint downCAP = new ColorAtPoint(downColor, down);
					if(!todo.Contains(downCAP))
					{
						todo.Add(downCAP);
					}
				}
			}

			return inRegion;
		}

		private Color FindPalestColor(HashSet<ColorAtPoint> points)
		{
			Bitmap localBitmap = new Bitmap(bitmap);
			Color color = Color.Black;
			HSV hsv = ConvertColors.HSVFromColor(color);
			foreach(ColorAtPoint p in points)
			{
				HSV pHSV = ConvertColors.HSVFromColor(p.Color);
				if(pHSV.Value > hsv.Value)
				{
					color = p.Color;
					hsv = pHSV;
				}
			}
			return color;
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

		private bool PointInRange(Point point)
		{
			return (point.X >= 0 && point.X < bitmap.Width && point.Y >= 0 && point.Y < bitmap.Height);
		}
		
		#endregion

	}
}
