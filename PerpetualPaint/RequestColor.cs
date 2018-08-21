using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerpetualPaint
{
	public struct RequestColor
	{
		public Color Color;
		public Point Point;

		public RequestColor(Color color, Point point)
		{
			Color = color;
			Point = point;
		}
	}
}
