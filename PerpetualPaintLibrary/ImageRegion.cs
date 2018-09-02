using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerpetualPaintLibrary
{
	public class ImageRegion
	{
		public HashSet<Point> Points = new HashSet<Point>();
		public Color PureColor = Color.White;
		public int Count {
			get {
				return Points.Count;
			}
		}

		public ImageRegion()
		{
		}

		public ImageRegion(HashSet<ColorAtPoint> region)
		{
			PureColor = Utilities.FindPalestColor(region);
			Points.UnionWith(region.Select(cap => cap.Point));
		}

		public bool Contains(Point point)
		{
			return Points.Contains(point);
		}

		public void Union(Point point)
		{
			Points.Add(point);
		}

		public void Union(ImageRegion region)
		{
			Points.UnionWith(region.Points);
		}

		public override string ToString()
		{
			return String.Format("Points: {0}, Color: {1}", Points.Count, PureColor);
		}
	}
}
