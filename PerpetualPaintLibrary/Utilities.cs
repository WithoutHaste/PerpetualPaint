using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WithoutHaste.Drawing.Colors;
using WithoutHaste.Drawing.Shapes;

namespace PerpetualPaintLibrary
{
	/// <remarks>
	/// Terminology:
	/// - Color greyscale = color from greyscale image
	/// - Color pureColor = pure "white" color applied to image
	/// - Color color = possibly grayed out color from color image
	/// </remarks>
	public static class Utilities
	{
		public static int MAX_RGB_AS_BLACK = 50;
		private static float MAX_VALUE_AS_BLACK {
			get {
				Color color = ConvertColors.RGBToColor(MAX_RGB_AS_BLACK, MAX_RGB_AS_BLACK, MAX_RGB_AS_BLACK);
				HSV hsv = ConvertColors.ToHSV(color);
				return hsv.Value;
			}
		}

		public static int MIN_RGB_AS_WHITE = 230;
		private static float MIN_VALUE_AS_WHITE {
			get {
				Color color = ConvertColors.RGBToColor(MIN_RGB_AS_WHITE, MIN_RGB_AS_WHITE, MIN_RGB_AS_WHITE);
				HSV hsv = ConvertColors.ToHSV(color);
				return hsv.Value;
			}
		}

		/// <summary>
		/// Returns the greyscale version of the <paramref name='coloredBitmap'/>.
		/// </summary>
		public static Bitmap GetGreyscaleOfBitmap(Bitmap coloredBitmap, List<ImageRegion> regions)
		{
			Bitmap greyscaleBitmap = new Bitmap(coloredBitmap);
			foreach(ImageRegion region in regions)
			{

			}
			return greyscaleBitmap;
		}

		/// <summary>
		/// Returns true if the entire image is in greyscale.
		/// </summary>
		public static bool BitmapIsGreyscale(Bitmap bitmap)
		{
			for(int x = 0; x < bitmap.Width; x++)
			{
				for(int y = 0; y < bitmap.Height; y++)
				{
					if(!ColorIsGreyscale(bitmap.GetPixel(x, y)))
						return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Convert a region of the image from to the selected color, using greying fill.
		/// </summary>
		/// <returns>Returns the previous pure color of the region.</returns>
		public static Color SetRegion(Bitmap greyscaleBitmap, Bitmap colorBitmap, ImageRegion region, Color pureColor)
		{
			Color oldPureColor = region.PureColor;
			List<ColorAtPoint> commands = new List<ColorAtPoint>();
			foreach(Point p in region.Points) //get all the colors at once so I'm not alternating between GetPixel/SetPixel
			{
				Color greyscaleColor = GetPixel(greyscaleBitmap, p);
				Color adjustedColor = Utilities.GreyscaleToColor(greyscaleColor, pureColor);
				commands.Add(new ColorAtPoint(adjustedColor, p));
			}
			foreach(ColorAtPoint command in commands)
			{
				SetPixel(colorBitmap, command.Point, command.Color);
			}
			region.PureColor = pureColor;
			return oldPureColor;
		}

		/// <summary>
		/// Converts a neutral grey color to a hued color with the same level of "grey".
		/// </summary>
		public static Color GreyscaleToColor(Color greyscale, Color pureColor)
		{
			if(ColorIsPartiallyClear(greyscale))
			{
				greyscale = ConvertPartiallyClearToGrey(greyscale);
			}
			if(ColorIsBlack(greyscale))
			{
				return greyscale;
			}
			if(ColorIsWhite(greyscale))
			{
				return pureColor;
			}
			HSV greyscaleHSV = ConvertColors.ToHSV(greyscale);
			HSV pureColorHSV = ConvertColors.ToHSV(pureColor);

			Range fullRange = new Range(MAX_VALUE_AS_BLACK, 1);
			Range newRange = new Range(MAX_VALUE_AS_BLACK, pureColorHSV.Value);
			float adjustedValue = (float)Range.ConvertValue(fullRange, newRange, greyscaleHSV.Value);

			float adjustedSaturation = greyscaleHSV.Value * pureColorHSV.Saturation;

			HSV adjustedHSV = new HSV(pureColorHSV.Hue, adjustedSaturation, adjustedValue);
			Color adjustedColor = ConvertColors.ToColor(adjustedHSV);
			return adjustedColor;
		}

		/// <summary>
		/// Converts a hued color to its nuetral grey equivalent, based on the pure form of the hue.
		/// </summary>
		public static Color ColorToGreyscale(Color color, Color pureColor)
		{
			if(ColorIsGreyscale(color))
			{
				return color;
			}
			if(color == pureColor)
			{
				return Color.White;
			}
			HSV colorHSV = ConvertColors.ToHSV(color);
			HSV pureColorHSV = ConvertColors.ToHSV(pureColor);

			Range fullRange = new Range(MAX_VALUE_AS_BLACK, 1);
			Range newRange = new Range(MAX_VALUE_AS_BLACK, pureColorHSV.Value);
			float adjustedValue = (float)Range.ConvertValue(newRange, fullRange, colorHSV.Value);

			HSV adjustedHSV = new HSV(0, 0, adjustedValue);
			Color adjustedColor = ConvertColors.ToColor(adjustedHSV);
			return adjustedColor;
		}

		/// <summary>
		/// Returns the color of the specified pixel, with translucents converted to opaque greyscale.
		/// </summary>
		public static Color GetPixel(Bitmap bitmap, System.Drawing.Point point)
		{
			Color color = bitmap.GetPixel(point.X, point.Y);
			if(ColorIsPartiallyClear(color))
			{
				color = ConvertPartiallyClearToGrey(color);
			}
			return color;
		}

		/// <summary>
		/// Sets the pixel to the color.
		/// </summary>
		public static void SetPixel(Bitmap bitmap, System.Drawing.Point point, Color color)
		{
			bitmap.SetPixel(point.X, point.Y, color);
		}

		/// <summary>
		/// Returns true if the color is totally transparent.
		/// </summary>
		private static bool ColorIsClear(Color color)
		{
			return (color.A == 0);
		}

		/// <summary>
		/// Returns true if the color is not totally opaque.
		/// </summary>
		private static bool ColorIsPartiallyClear(Color color)
		{
			return (color.A < 255);
		}

		/// <summary>
		/// Returns true for greys in the "black" range.
		/// Returns false for all transparent colors.
		/// </summary>
		public static bool ColorIsBlack(Color color)
		{
			if(ColorIsPartiallyClear(color)) return false;
			return (ColorIsGreyscale(color) && color.R < MAX_RGB_AS_BLACK);
		}

		/// <summary>
		/// Returns true for White/Grey/Black or partially transluscent colors.
		/// </summary>
		public static bool ColorIsGreyscale(Color color)
		{
			if(ColorIsPartiallyClear(color)) return true;
			return (color.R == color.G && color.G == color.B);
		}

		/// <summary>
		/// Returns true for totally clear colors and greys in the "white" range.
		/// </summary>
		public static bool ColorIsWhite(Color color)
		{
			if(ColorIsClear(color)) return true;
			return (ColorIsGreyscale(color) && color.R > MIN_RGB_AS_WHITE);
		}

		/// <summary>
		/// Converts translucent colors to a greyscale equivalent.
		/// </summary>
		private static Color ConvertPartiallyClearToGrey(Color oldColor)
		{
			//25% solid => 75% gray
			return ConvertColors.HSVToColor(0, 0, (255 - oldColor.A) / 255f);
		}

		private static float ConvertRange(Range largeRange, Range smallRange, float value)
		{
			if(largeRange.Start != smallRange.Start)
				throw new NotImplementedException("Not implemented: ConvertRange when ranges have different minimum values.");

			double scale = smallRange.Span / largeRange.Span;
			return (float)(((value - largeRange.Start) * scale) + largeRange.Start);
		}

		/// <summary>
		/// Returns the palest (closest to white) color in the set.
		/// </summary>
		public static Color FindPalestColor(HashSet<ColorAtPoint> points)
		{
			if(ColorIsGreyscale(points.First().Color))
				return Color.White;

			Color color = Color.Black;
			HSV hsv = ConvertColors.ToHSV(color);
			foreach(ColorAtPoint p in points)
			{
				HSV pHSV = ConvertColors.ToHSV(p.Color);
				if(pHSV.Value > hsv.Value)
				{
					color = p.Color;
					hsv = pHSV;
				}
			}
			return color;
		}

	}
}
