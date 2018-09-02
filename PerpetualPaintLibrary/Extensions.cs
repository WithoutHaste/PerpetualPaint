using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerpetualPaintLibrary
{
	public static class Extensions
	{
		public static Point Left(this Point point)
		{
			return new Point(point.X - 1, point.Y);
		}

		public static Point Right(this Point point)
		{
			return new Point(point.X + 1, point.Y);
		}

		public static Point Up(this Point point)
		{
			return new Point(point.X, point.Y - 1);
		}

		public static Point Down(this Point point)
		{
			return new Point(point.X, point.Y + 1);
		}

		public static bool InRange(this Bitmap bitmap, Point point)
		{
			return (point.X >= 0 && point.X < bitmap.Width && point.Y >= 0 && point.Y < bitmap.Height);
		}
	}
}
