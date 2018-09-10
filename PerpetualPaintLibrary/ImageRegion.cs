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
		private static int nextId = 0;

		public int Id { get; private set; }
		public List<Point> Points = new List<Point>();
		public Color PureColor = Color.White;
		public int Count {
			get {
				return Points.Count;
			}
		}

		public ImageRegion()
		{
			Id = nextId++;
		}

		public ImageRegion(HashSet<ColorAtPoint> region)
		{
			PureColor = Utilities.FindPalestColor(region);
			Points.AddRange(region.Select(cap => cap.Point));
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
			Points.AddRange(region.Points);
		}

		public override string ToString()
		{
			return String.Format("Region Id: {0}, Points: {1}, Color: {2}", Id, Points.Count, PureColor);
		}
	}
}
