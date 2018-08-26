using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerpetualPaint
{
	public delegate void ColorEventHandler(object sender, ColorEventArgs e);

	public class ColorEventArgs : EventArgs
	{
		public ColorAtPoint ColorAtPoint { get; set; }

		public ColorEventArgs(ColorAtPoint colorAtPoint) : base()
		{
			ColorAtPoint = colorAtPoint;
		}
	}
}
