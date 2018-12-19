using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PerpetualPaintLibrary;
using WithoutHaste.Drawing.Colors;

namespace PerpetualPaintTest
{
	[TestClass]
	public class IOTests
	{
		[TestMethod]
		public void IO_ZipProject_GreyscaleColor()
		{
			//arrange
			Color grey = Color.Black;
			Color color = Color.Red;
			string zipFilename = "testProject.ppp";
			Bitmap greyscaleBitmap = MakeBitmap(grey);
			Bitmap colorBitmap = MakeBitmap(color);
			PPProject project = new PPProject(greyscaleBitmap, colorBitmap);
			//act
			PerpetualPaintLibrary.IO.ZipProject(zipFilename, project);
			//assert
			Assert.IsTrue(File.Exists(zipFilename));

			PPProject result = PerpetualPaintLibrary.IO.LoadProject(zipFilename);
			Assert.IsNull(result.ColorPalette);
			Assert.IsNull(result.Config);
			Assert.IsNotNull(result.GreyscaleBitmap);
			Assert.IsNotNull(result.ColorBitmap);
			Assert.IsTrue(ColorsMatch(grey, result.GreyscaleBitmap.GetPixel(0, 0)));
			Assert.IsTrue(ColorsMatch(color, result.ColorBitmap.GetPixel(0, 0)));
		}

		[TestMethod]
		public void IO_ZipProject_GreyscaleColorPalette()
		{
			//arrange
			Color grey = Color.Black;
			Color color = Color.Red;
			string zipFilename = "testProject.ppp";
			Bitmap greyscaleBitmap = MakeBitmap(grey);
			Bitmap colorBitmap = MakeBitmap(color);
			ColorPalette colorPalette = new ColorPalette(Color.Blue, Color.Yellow, Color.Orange);
			PPProject project = new PPProject(greyscaleBitmap, colorBitmap, colorPalette);
			//act
			PerpetualPaintLibrary.IO.ZipProject(zipFilename, project);
			//assert
			Assert.IsTrue(File.Exists(zipFilename));

			PPProject result = PerpetualPaintLibrary.IO.LoadProject(zipFilename);
			Assert.IsNull(result.Config);
			Assert.IsNotNull(result.GreyscaleBitmap);
			Assert.IsNotNull(result.ColorBitmap);
			Assert.IsNotNull(result.ColorPalette);
			Assert.IsTrue(ColorsMatch(grey, result.GreyscaleBitmap.GetPixel(0, 0)));
			Assert.IsTrue(ColorsMatch(color, result.ColorBitmap.GetPixel(0, 0)));
			Assert.AreEqual(colorPalette, result.ColorPalette);
		}

		[TestMethod]
		public void IO_ZipProject_GreyscaleColorConfig()
		{
			//arrange
			Color grey = Color.Black;
			Color color = Color.Red;
			string zipFilename = "testProject.ppp";
			Bitmap greyscaleBitmap = MakeBitmap(grey);
			Bitmap colorBitmap = MakeBitmap(color);
			PPPConfig config = new PPPConfig("path\\palette_filename.gpl");
			PPProject project = new PPProject(greyscaleBitmap, colorBitmap, config);
			//act
			PerpetualPaintLibrary.IO.ZipProject(zipFilename, project);
			//assert
			Assert.IsTrue(File.Exists(zipFilename));

			PPProject result = PerpetualPaintLibrary.IO.LoadProject(zipFilename);
			Assert.IsNull(result.ColorPalette);
			Assert.IsNotNull(result.GreyscaleBitmap);
			Assert.IsNotNull(result.ColorBitmap);
			Assert.IsNotNull(result.Config);
			Assert.IsTrue(ColorsMatch(grey, result.GreyscaleBitmap.GetPixel(0, 0)));
			Assert.IsTrue(ColorsMatch(color, result.ColorBitmap.GetPixel(0, 0)));
			Assert.AreEqual(config.PaletteFileName, result.Config.PaletteFileName);
		}

		private Bitmap MakeBitmap(Color color)
		{
			Bitmap bitmap = new Bitmap(10, 10);
			using(Graphics graphics = Graphics.FromImage(bitmap))
			{
				using(SolidBrush brush = new SolidBrush(color))
				{
					graphics.FillRectangle(brush, 0, 0, bitmap.Width, bitmap.Height);
				}
			}
			return bitmap;
		}

		private bool ColorsMatch(Color a, Color b)
		{
			return (a.R == b.R && a.G == b.G && a.B == b.B);
		}
	}
}
