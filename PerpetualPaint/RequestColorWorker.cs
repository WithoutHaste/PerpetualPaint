using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WithoutHaste.Drawing.Colors;

namespace PerpetualPaint
{
	public class RequestColorWorker
	{
		private BackgroundWorker worker;
		private Queue<RequestColor> queue;
		private Bitmap bitmap;
		private WaitForm waitForm;

		public bool IsBusy {
			get {
				if(worker == null)
					return false;
				return worker.IsBusy;
			}
		}

		public RequestColorWorker(Queue<RequestColor> queue, Bitmap bitmap, RunWorkerCompletedEventHandler completedEventHandler = null)
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
			Run(bitmap);
		}

		public void Run(Bitmap bitmap)
		{
			this.bitmap = (Bitmap)bitmap.Clone();
			Run();
		}

		private void Run()
		{
			if(worker == null)
				throw new Exception("Worker cannot be run before it exists."); //todo specific exception
			if(worker.IsBusy)
				return;
			worker.RunWorkerAsync();
			ShowWaitMessage("Applying color..");
		}

		private void OnCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if(queue.Count > 0)
			{
				//todo cancel oncompleted
				Run();
			}
			else
			{
				CloseWaitMessage();
			}
		}

		private void ShowWaitMessage(string message)
		{
			if(waitForm == null)
			{
				waitForm = new WaitForm(message);
			}
			waitForm.ShowDialog();
		}

		private void CloseWaitMessage()
		{
			if(waitForm == null)
				return;

			//todo wait form exists but is closed already

			waitForm.Close();
		}

		//todo accept cancellation from wait form
		//todo update main form status bar instead of showing a modal popup
		//todo update main form status bar with "Processing request 2 of 6..."
		//todo on cancellation, still update main form with the last successfully completed coloration

		#region Color Bitmap

		private void ColorPixel(object sender, DoWorkEventArgs e)
		{
			if(queue.Count == 0)
				return;

			RequestColor request = queue.Dequeue();
			Color color = request.Color;
			Point point = request.Point;

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
				if(ColorIsBlack(oldColor))
					continue;
				if(!ColorIsGrayscale(oldColor))
					continue;

				if(ColorIsWhite(oldColor))
				{
					bitmap.SetPixel(p.X, p.Y, color);
				}
				else
				{
					if(ColorIsPartiallyClear(oldColor))
					{
						oldColor = ConvertPartiallyClearToGray(oldColor);
					}
					//how to apply value to color that has value of its own? in a fully reversible way?
					//todo: how to make this part easy to test?
					HSV oldHSV = ConvertColors.HSVFromColor(oldColor);
					//treating newColor is "white", adjust underlying value on range from newWhite to black
					HSV newWhite = ConvertColors.HSVFromColor(color);
					//todo: document that coloring in with black and other very dark color will destroy some of your grayscale gradient
					//todo: may need to change HSV ranges in library to ints 0-360 and 0-100, since that seems to be how online tools handle it
					float adjustedValue = oldHSV.Value * newWhite.Value * 0.5f;
					float adjustedSaturation = oldHSV.Value * newWhite.Saturation * 0.5f; //cut adjusted saturation in half to force it into the gray range
					HSV adjustedHSV = new HSV(newWhite.Hue, adjustedSaturation, adjustedValue);
					Color adjustedColor = ConvertColors.ColorFromHSV(adjustedHSV);
					bitmap.SetPixel(p.X, p.Y, adjustedColor);
				}

				Point left = new Point(p.X - 1, p.Y);
				Point right = new Point(p.X + 1, p.Y);
				Point up = new Point(p.X, p.Y - 1);
				Point down = new Point(p.X, p.Y + 1);
				if(PointInRange(left) && !PointInList(todo, left)) todo.Add(left);
				if(PointInRange(right) && !PointInList(todo, right)) todo.Add(right);
				if(PointInRange(up) && !PointInList(todo, up)) todo.Add(up);
				if(PointInRange(down) && !PointInList(todo, down)) todo.Add(down);
			}

			e.Result = bitmap;
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

		//todo: allow variable tolerance with demo of pure white/black image
		//todo: move color determination static methods to somewhere else
		public static bool ColorIsClear(Color color)
		{
			return (color.A == 0);
		}

		public static bool ColorIsPartiallyClear(Color color)
		{
			return (color.A < 255);
		}

		public static bool ColorIsBlack(Color color)
		{
			if(ColorIsPartiallyClear(color)) return false;
			return (ColorIsGrayscale(color) && color.R < 50);
		}

		public static bool ColorIsGrayscale(Color color)
		{
			if(ColorIsPartiallyClear(color)) return true;
			return (color.R == color.G && color.G == color.B);
		}

		public static bool ColorIsWhite(Color color)
		{
			if(ColorIsClear(color)) return true;
			return (ColorIsGrayscale(color) && color.R > 230);
		}

		public static Color ConvertPartiallyClearToGray(Color oldColor)
		{
			//25% solid => 75% gray
			return ConvertColors.ColorFromHSV(0, 0, (255 - oldColor.A) / 255f);
		}

		#endregion

	}
}
