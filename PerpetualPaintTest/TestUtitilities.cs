using System;
using System.Collections.Generic;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PerpetualPaintLibrary;

namespace PerpetualPaintTest
{
	[TestClass]
	public class TestUtitilities
	{
		[TestMethod]
		public void GenerateGradiantSamples()
		{
			List<Color> colors = new List<Color>() {
				Color.Red,
				Color.Orange,
				Color.Yellow,
				Color.Green,
				Color.Blue,
				Color.Purple
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
