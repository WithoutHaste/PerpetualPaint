using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WithoutHaste.Drawing.Colors;
using WithoutHaste.Windows.GUI;

namespace PerpetualPaintLibrary
{
	/// <summary>
	/// Perpetual Paint Project
	/// </summary>
	public class PPProject
	{
		public static readonly string PROJECT_EXTENSION = ".ppp";

		public Bitmap GreyscaleBitmap;
		public Bitmap ColorBitmap;
		public ColorPalette ColorPalette;
		public PPPConfig Config;

		public PPProject()
		{
		}

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

		/// <summary>Returns a thumbnail of the <see cref='ColorBitmap'/>.</summary>
		/// <remarks>
		/// If <see cref='ColorBitmap'/> is not available, it will use <see cref='GreyscaleBitmap'/> instead.
		/// If neither is available, will use a blank image.
		/// </remarks>
		public Bitmap GetThumbnail(int maxWidth, int maxHeight)
		{
			Bitmap origin = (ColorBitmap ?? GreyscaleBitmap) ?? new Bitmap(100, 100);
			return ImageHelper.GetThumbnail(origin, maxWidth, maxHeight);
		}

	}
}
