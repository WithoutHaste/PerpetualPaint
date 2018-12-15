using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WithoutHaste.Drawing.Colors;

namespace PerpetualPaintLibrary
{
	/// <summary>
	/// Perpetual Paint Project
	/// </summary>
	public class PPProject
	{
		public Bitmap GreyscaleBitmap;
		public Bitmap ColorBitmap;
		public ColorPalette ColorPalette;
		public PPPConfig Config;

		public PPProject(Bitmap greyscaleBitmap, Bitmap colorBitmap)
		{
			GreyscaleBitmap = greyscaleBitmap;
			ColorBitmap = colorBitmap;
			ColorPalette = null;
			Config = null;
		}

		public PPProject(Bitmap greyscaleBitmap, Bitmap colorBitmap, ColorPalette colorPalette)
		{
			GreyscaleBitmap = greyscaleBitmap;
			ColorBitmap = colorBitmap;
			ColorPalette = colorPalette;
			Config = null;
		}

		public PPProject(Bitmap greyscaleBitmap, Bitmap colorBitmap, PPPConfig config)
		{
			GreyscaleBitmap = greyscaleBitmap;
			ColorBitmap = colorBitmap;
			ColorPalette = null;
			Config = config;
		}
	}
}
