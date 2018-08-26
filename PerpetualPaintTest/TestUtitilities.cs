using System;
using System.Collections.Generic;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PerpetualPaintLibrary;
using WithoutHaste.Drawing.Colors;

namespace PerpetualPaintTest
{
	[TestClass]
	public class TestUtitilities
	{
		[TestMethod]
		public void GenerateGradiantSamples()
		{
			List<Color> colors = new List<Color>() {
				//100% saturation, 100% value
				ConvertColors.ColorFromHSV(0, 1, 1), //red
				ConvertColors.ColorFromHSV(30, 1, 1), //orange
				ConvertColors.ColorFromHSV(60, 1, 1), //yellow
				ConvertColors.ColorFromHSV(90, 1, 1), //green
				ConvertColors.ColorFromHSV(120, 1, 1), //green also
				ConvertColors.ColorFromHSV(150, 1, 1), //aqua
				ConvertColors.ColorFromHSV(180, 1, 1), //cyan
				ConvertColors.ColorFromHSV(210, 1, 1), //blue
				ConvertColors.ColorFromHSV(240, 1, 1), //blue darker
				ConvertColors.ColorFromHSV(270, 1, 1), //purple
				ConvertColors.ColorFromHSV(300, 1, 1), //magenta
				ConvertColors.ColorFromHSV(330, 1, 1), //fushia
				//100% saturation, 50% value
				ConvertColors.ColorFromHSV(0, 1, 0.5f), //red
				ConvertColors.ColorFromHSV(30, 1, 0.5f), //orange
				ConvertColors.ColorFromHSV(60, 1, 0.5f), //yellow
				ConvertColors.ColorFromHSV(90, 1, 0.5f), //green
				ConvertColors.ColorFromHSV(120, 1, 0.5f), //green also
				ConvertColors.ColorFromHSV(150, 1, 0.5f), //aqua
				ConvertColors.ColorFromHSV(180, 1, 0.5f), //cyan
				ConvertColors.ColorFromHSV(210, 1, 0.5f), //blue
				ConvertColors.ColorFromHSV(240, 1, 0.5f), //blue darker
				ConvertColors.ColorFromHSV(270, 1, 0.5f), //purple
				ConvertColors.ColorFromHSV(300, 1, 0.5f), //magenta
				ConvertColors.ColorFromHSV(330, 1, 0.5f), //fushia
				//100% saturation, 25% value
				ConvertColors.ColorFromHSV(0, 1, 0.25f), //red
				ConvertColors.ColorFromHSV(30, 1, 0.25f), //orange
				ConvertColors.ColorFromHSV(60, 1, 0.25f), //yellow
				ConvertColors.ColorFromHSV(90, 1, 0.25f), //green
				ConvertColors.ColorFromHSV(120, 1, 0.25f), //green also
				ConvertColors.ColorFromHSV(150, 1, 0.25f), //aqua
				ConvertColors.ColorFromHSV(180, 1, 0.25f), //cyan
				ConvertColors.ColorFromHSV(210, 1, 0.25f), //blue
				ConvertColors.ColorFromHSV(240, 1, 0.25f), //blue darker
				ConvertColors.ColorFromHSV(270, 1, 0.25f), //purple
				ConvertColors.ColorFromHSV(300, 1, 0.25f), //magenta
				ConvertColors.ColorFromHSV(330, 1, 0.25f), //fushia
				//50% saturation, 100% value
				ConvertColors.ColorFromHSV(0, 0.5f, 1), //red
				ConvertColors.ColorFromHSV(30, 0.5f, 1), //orange
				ConvertColors.ColorFromHSV(60, 0.5f, 1), //yellow
				ConvertColors.ColorFromHSV(90, 0.5f, 1), //green
				ConvertColors.ColorFromHSV(120, 0.5f, 1), //green also
				ConvertColors.ColorFromHSV(150, 0.5f, 1), //aqua
				ConvertColors.ColorFromHSV(180, 0.5f, 1), //cyan
				ConvertColors.ColorFromHSV(210, 0.5f, 1), //blue
				ConvertColors.ColorFromHSV(240, 0.5f, 1), //blue darker
				ConvertColors.ColorFromHSV(270, 0.5f, 1), //purple
				ConvertColors.ColorFromHSV(300, 0.5f, 1), //magenta
				ConvertColors.ColorFromHSV(330, 0.5f, 1), //fushia
				//50% saturation, 50% value
				ConvertColors.ColorFromHSV(0, 0.5f, 0.5f), //red
				ConvertColors.ColorFromHSV(30, 0.5f, 0.5f), //orange
				ConvertColors.ColorFromHSV(60, 0.5f, 0.5f), //yellow
				ConvertColors.ColorFromHSV(90, 0.5f, 0.5f), //green
				ConvertColors.ColorFromHSV(120, 0.5f, 0.5f), //green also
				ConvertColors.ColorFromHSV(150, 0.5f, 0.5f), //aqua
				ConvertColors.ColorFromHSV(180, 0.5f, 0.5f), //cyan
				ConvertColors.ColorFromHSV(210, 0.5f, 0.5f), //blue
				ConvertColors.ColorFromHSV(240, 0.5f, 0.5f), //blue darker
				ConvertColors.ColorFromHSV(270, 0.5f, 0.5f), //purple
				ConvertColors.ColorFromHSV(300, 0.5f, 0.5f), //magenta
				ConvertColors.ColorFromHSV(330, 0.5f, 0.5f), //fushia
				//50% saturation, 50% value
				ConvertColors.ColorFromHSV(0, 0.5f, 0.25f), //red
				ConvertColors.ColorFromHSV(30, 0.5f, 0.25f), //orange
				ConvertColors.ColorFromHSV(60, 0.5f, 0.25f), //yellow
				ConvertColors.ColorFromHSV(90, 0.5f, 0.25f), //green
				ConvertColors.ColorFromHSV(120, 0.5f, 0.25f), //green also
				ConvertColors.ColorFromHSV(150, 0.5f, 0.25f), //aqua
				ConvertColors.ColorFromHSV(180, 0.5f, 0.25f), //cyan
				ConvertColors.ColorFromHSV(210, 0.5f, 0.25f), //blue
				ConvertColors.ColorFromHSV(240, 0.5f, 0.25f), //blue darker
				ConvertColors.ColorFromHSV(270, 0.5f, 0.25f), //purple
				ConvertColors.ColorFromHSV(300, 0.5f, 0.25f), //magenta
				ConvertColors.ColorFromHSV(330, 0.5f, 0.25f), //fushia
				//25% saturation, 100% value
				ConvertColors.ColorFromHSV(0, 0.25f, 1), //red
				ConvertColors.ColorFromHSV(30, 0.25f, 1), //orange
				ConvertColors.ColorFromHSV(60, 0.25f, 1), //yellow
				ConvertColors.ColorFromHSV(90, 0.25f, 1), //green
				ConvertColors.ColorFromHSV(120, 0.25f, 1), //green also
				ConvertColors.ColorFromHSV(150, 0.25f, 1), //aqua
				ConvertColors.ColorFromHSV(180, 0.25f, 1), //cyan
				ConvertColors.ColorFromHSV(210, 0.25f, 1), //blue
				ConvertColors.ColorFromHSV(240, 0.25f, 1), //blue darker
				ConvertColors.ColorFromHSV(270, 0.25f, 1), //purple
				ConvertColors.ColorFromHSV(300, 0.25f, 1), //magenta
				ConvertColors.ColorFromHSV(330, 0.25f, 1), //fushia
				//25% saturation, 75% value
				ConvertColors.ColorFromHSV(0, 0.25f, 0.75f), //red
				ConvertColors.ColorFromHSV(30, 0.25f, 0.75f), //orange
				ConvertColors.ColorFromHSV(60, 0.25f, 0.75f), //yellow
				ConvertColors.ColorFromHSV(90, 0.25f, 0.75f), //green
				ConvertColors.ColorFromHSV(120, 0.25f, 0.75f), //green also
				ConvertColors.ColorFromHSV(150, 0.25f, 0.75f), //aqua
				ConvertColors.ColorFromHSV(180, 0.25f, 0.75f), //cyan
				ConvertColors.ColorFromHSV(210, 0.25f, 0.75f), //blue
				ConvertColors.ColorFromHSV(240, 0.25f, 0.75f), //blue darker
				ConvertColors.ColorFromHSV(270, 0.25f, 0.75f), //purple
				ConvertColors.ColorFromHSV(300, 0.25f, 0.75f), //magenta
				ConvertColors.ColorFromHSV(330, 0.25f, 0.75f), //fushia
				//25% saturation, 50% value
				ConvertColors.ColorFromHSV(0, 0.25f, 0.5f), //red
				ConvertColors.ColorFromHSV(30, 0.25f, 0.5f), //orange
				ConvertColors.ColorFromHSV(60, 0.25f, 0.5f), //yellow
				ConvertColors.ColorFromHSV(90, 0.25f, 0.5f), //green
				ConvertColors.ColorFromHSV(120, 0.25f, 0.5f), //green also
				ConvertColors.ColorFromHSV(150, 0.25f, 0.5f), //aqua
				ConvertColors.ColorFromHSV(180, 0.25f, 0.5f), //cyan
				ConvertColors.ColorFromHSV(210, 0.25f, 0.5f), //blue
				ConvertColors.ColorFromHSV(240, 0.25f, 0.5f), //blue darker
				ConvertColors.ColorFromHSV(270, 0.25f, 0.5f), //purple
				ConvertColors.ColorFromHSV(300, 0.25f, 0.5f), //magenta
				ConvertColors.ColorFromHSV(330, 0.25f, 0.5f), //fushia
				//25% saturation, 25% value
				ConvertColors.ColorFromHSV(0, 0.25f, 0.25f), //red
				ConvertColors.ColorFromHSV(30, 0.25f, 0.25f), //orange
				ConvertColors.ColorFromHSV(60, 0.25f, 0.25f), //yellow
				ConvertColors.ColorFromHSV(90, 0.25f, 0.25f), //green
				ConvertColors.ColorFromHSV(120, 0.25f, 0.25f), //green also
				ConvertColors.ColorFromHSV(150, 0.25f, 0.25f), //aqua
				ConvertColors.ColorFromHSV(180, 0.25f, 0.25f), //cyan
				ConvertColors.ColorFromHSV(210, 0.25f, 0.25f), //blue
				ConvertColors.ColorFromHSV(240, 0.25f, 0.25f), //blue darker
				ConvertColors.ColorFromHSV(270, 0.25f, 0.25f), //purple
				ConvertColors.ColorFromHSV(300, 0.25f, 0.25f), //magenta
				ConvertColors.ColorFromHSV(330, 0.25f, 0.25f), //fushia
			};
			int width = 50;
			foreach(Color color in colors)
			{
				Bitmap revertBitmap = GetRevertedGradient(width, color);
				Bitmap colorBitmap = GetColoredGradient(width, color);
				Bitmap grayBitmap = GetGrayscaleGradient(width);
				Bitmap bitmap = new Bitmap(width * 3, revertBitmap.Height);
				using(Graphics graphics = Graphics.FromImage(bitmap))
				{
					graphics.DrawImage(revertBitmap, new Point(0, 0));
					graphics.DrawImage(colorBitmap, new Point(width, 0));
					graphics.DrawImage(grayBitmap, new Point(width * 2, 0));
				}
				bitmap.Save(String.Format("GradientSamples/sample_{0:000}_{1:000}_{2:000}.png", color.R, color.G, color.B));
			}
		}

		private Bitmap GetRevertedGradient(int width, Color color)
		{
			Bitmap bitmap = GetColoredGradient(width, color);
			for(int x = 0; x < bitmap.Width; x++)
			{
				for(int y = 0; y < bitmap.Height; y++)
				{
					bitmap.SetPixel(x, y, Utilities.ColorToGrayscale(bitmap.GetPixel(x, y), color));
				}
			}
			return bitmap;
		}

		private Bitmap GetColoredGradient(int width, Color color)
		{
			Bitmap bitmap = GetGrayscaleGradient(width);
			for(int x = 0; x < bitmap.Width; x++)
			{
				for(int y = 0; y < bitmap.Height; y++)
				{
					bitmap.SetPixel(x, y, Utilities.GrayscaleToColor(bitmap.GetPixel(x, y), color));
				}
			}
			return bitmap;
		}

		private Bitmap GetGrayscaleGradient(int width)
		{
			List<Color> grays = new List<Color>() {
				Color.FromArgb(255, 0, 0, 0),
				Color.FromArgb(255, 15, 15, 15),
				Color.FromArgb(255, 31, 31, 31),
				Color.FromArgb(255, 47, 47, 47),
				Color.FromArgb(255, 63, 63, 63),
				Color.FromArgb(255, 79, 79, 79),
				Color.FromArgb(255, 95, 95, 95),
				Color.FromArgb(255, 111, 111, 111),
				Color.FromArgb(255, 127, 127, 127),
				Color.FromArgb(255, 143, 143, 143),
				Color.FromArgb(255, 159, 159, 159),
				Color.FromArgb(255, 175, 175, 175),
				Color.FromArgb(255, 191, 191, 191),
				Color.FromArgb(255, 207, 207, 207),
				Color.FromArgb(255, 223, 223, 223),
				Color.FromArgb(255, 239, 239, 239),
				Color.FromArgb(255, 255, 255, 255)
			};
			Bitmap bitmap = new Bitmap(width, width * grays.Count);
			using(Graphics graphics = Graphics.FromImage(bitmap))
			{
				for(int i = 0; i < grays.Count; i++)
				{
					graphics.FillRectangle(new SolidBrush(grays[i]), new Rectangle(0, i * width, width, width));
				}
			}
			return bitmap;
		}
	}
}
