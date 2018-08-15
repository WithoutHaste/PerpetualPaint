using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PerpetualPaint
{
	public class PixelPictureBox : PictureBox
	{
		protected override void OnPaint(PaintEventArgs paintEventArgs)
		{
			if(ZoomedIn(paintEventArgs.Graphics))
			{
				paintEventArgs.Graphics.SmoothingMode = SmoothingMode.None; //allow pixelation
				paintEventArgs.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor; //stop color smoothing
				paintEventArgs.Graphics.PixelOffsetMode = PixelOffsetMode.Half; //stops image from being shifted up-left by a half-pixel
			}
			base.OnPaint(paintEventArgs);
		}

		private bool ZoomedIn(Graphics graphics)
		{
			if(this.Image == null)
				return false;
			return (this.Image.Width < graphics.ClipBounds.Width || this.Image.Height < graphics.ClipBounds.Height);
		}
	}
}
