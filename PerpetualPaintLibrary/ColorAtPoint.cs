using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerpetualPaintLibrary
{
	public class ColorAtPoint
	{
		public readonly Color Color;
		public readonly Point Point;

		public ColorAtPoint(Color color, Point point)
		{
			Color = color;
			Point = point;
		}

		public virtual ColorAtPoint ChangeColor(Color newColor)
		{
			return new ColorAtPoint(newColor, Point);
		}

		public ColorAtPoint_NoHistory ToNoHistory()
		{
			return new ColorAtPoint_NoHistory(Color, Point);
		}

		public override string ToString()
		{
			return String.Format("ARGB({0},{1},{2},{3}) at ({4},{5})", Color.A, Color.R, Color.G, Color.B, Point.X, Point.Y);
		}

		#region Equality

		public static bool operator ==(ColorAtPoint a, ColorAtPoint b)
		{
			return (a.Color == b.Color && a.Point == b.Point);
		}

		public static bool operator !=(ColorAtPoint a, ColorAtPoint b)
		{
			return (a.Color != b.Color || a.Point != b.Point);
		}

		public override bool Equals(Object b)
		{
			if(b != null && b is ColorAtPoint)
			{
				return (this == (ColorAtPoint)b);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return Color.GetHashCode() ^ Point.GetHashCode();
		}

		#endregion
	}

	public class ColorAtPoint_NoHistory : ColorAtPoint
	{
		public ColorAtPoint_NoHistory(Color color, Point point) : base(color, point)
		{
		}

		public override ColorAtPoint ChangeColor(Color newColor)
		{
			return new ColorAtPoint_NoHistory(newColor, Point);
		}
	}
}
