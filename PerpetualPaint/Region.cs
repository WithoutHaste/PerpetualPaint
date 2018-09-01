using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PerpetualPaintLibrary;

namespace PerpetualPaint
{
	public class Region
	{
		public HashSet<Point> Points = new HashSet<Point>();
		public Color PureColor = Color.White;
		public int Count {
			get {
				return Points.Count;
			}
		}

		public Region()
		{
		}

		public Region(HashSet<ColorAtPoint> region)
		{
			PureColor = Utilities.FindPalestColor(region);
			Points.UnionWith(region.Select(cap => cap.Point));
		}

		public bool Contains(Point point)
		{
			return Points.Contains(point);
		}
	}
}
