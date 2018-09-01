using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WithoutHaste.Drawing.Colors;
using WithoutHaste.Drawing.Shapes;

namespace PerpetualPaintLibrary
{
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

		/*
		 * VOCAB
		 * 
		 * Color grayscale = color from grayscale image
		 * Color pureColor = pure "white" color applied to image
		 * Color color = possibly grayed out color from color image
		 */

		public static Color GrayscaleToColor(Color grayscale, Color pureColor)
		{
			if(ColorIsPartiallyClear(grayscale))
			{
				grayscale = ConvertPartiallyClearToGray(grayscale);
			}
			if(ColorIsBlack(grayscale))
			{
				return grayscale;
			}
			if(ColorIsWhite(grayscale))
			{
				return pureColor;
			}
			HSV grayscaleHSV = ConvertColors.ToHSV(grayscale);
			HSV pureColorHSV = ConvertColors.ToHSV(pureColor);

			Range fullRange = new Range(MAX_VALUE_AS_BLACK, 1);
			Range newRange = new Range(MAX_VALUE_AS_BLACK, pureColorHSV.Value);
			float adjustedValue = (float)Range.ConvertValue(fullRange, newRange, grayscaleHSV.Value);

			float adjustedSaturation = grayscaleHSV.Value * pureColorHSV.Saturation;

			HSV adjustedHSV = new HSV(pureColorHSV.Hue, adjustedSaturation, adjustedValue);
			Color adjustedColor = ConvertColors.ToColor(adjustedHSV);
			return adjustedColor;
		}

		public static Color ColorToGrayscale(Color color, Color pureColor)
		{
			if(ColorIsGrayscale(color))
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

		public static Color GetPixel(Bitmap bitmap, System.Drawing.Point point)
		{
			Color color = bitmap.GetPixel(point.X, point.Y);
			if(ColorIsPartiallyClear(color))
			{
				color = ConvertPartiallyClearToGray(color);
			}
			return color;
		}

		private static bool ColorIsClear(Color color)
		{
			return (color.A == 0);
		}

		private static bool ColorIsPartiallyClear(Color color)
		{
			return (color.A < 255);
		}

		public static bool ColorIsBlack(Color color)
		{
			if(ColorIsPartiallyClear(color)) return false;
			return (ColorIsGrayscale(color) && color.R < MAX_RGB_AS_BLACK);
		}

		public static bool ColorIsGrayscale(Color color)
		{
			if(ColorIsPartiallyClear(color)) return true;
			return (color.R == color.G && color.G == color.B);
		}

		public static bool ColorIsWhite(Color color)
		{
			if(ColorIsClear(color)) return true;
			return (ColorIsGrayscale(color) && color.R > MIN_RGB_AS_WHITE);
		}

		private static Color ConvertPartiallyClearToGray(Color oldColor)
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

		public static Color FindPalestColor(Bitmap bitmap, System.Drawing.Point point)
		{
			HashSet<ColorAtPoint> inRegion = FindRegion(bitmap, point);
			if(inRegion.Count == 0)
			{
				return Color.White;
			}
			return FindPalestColor(inRegion);
		}

		public static Color FindPalestColor(HashSet<ColorAtPoint> points)
		{
			if(ColorIsGrayscale(points.First().Color))
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

		/// <summary>
		/// Find region as bounded by "black".
		/// 
		/// May lock portions of bitmap, so cend in a fresh copy.
		/// </summary>
		public static HashSet<ColorAtPoint> FindRegion(Bitmap localBitmap, System.Drawing.Point startPoint)
		{
			HashSet<ColorAtPoint> inRegion = new HashSet<ColorAtPoint>();
			//Bitmap localBitmap = new Bitmap(bitmap);

			//todo: why is this using up memory on small color change on cat?
			HashSet<ColorAtPoint> todo = new HashSet<ColorAtPoint>() {
				new ColorAtPoint(GetPixel(localBitmap, startPoint), startPoint)
			};
			while(todo.Count > 0)
			{
				ColorAtPoint p = todo.First();
				todo.Remove(p);
				if(inRegion.Contains(p))
					continue;

				if(PerpetualPaintLibrary.Utilities.ColorIsBlack(p.Color))
					continue;

				inRegion.Add(p);
				//todo: duplicate code
				System.Drawing.Point left = new System.Drawing.Point(p.Point.X - 1, p.Point.Y);
				System.Drawing.Point right = new System.Drawing.Point(p.Point.X + 1, p.Point.Y);
				System.Drawing.Point up = new System.Drawing.Point(p.Point.X, p.Point.Y - 1);
				System.Drawing.Point down = new System.Drawing.Point(p.Point.X, p.Point.Y + 1);
				if(PointInRange(localBitmap, left))
				{
					Color leftColor = GetPixel(localBitmap, left);
					ColorAtPoint leftCAP = new ColorAtPoint(leftColor, left);
					if(!todo.Contains(leftCAP))
					{
						todo.Add(leftCAP);
					}
				}
				if(PointInRange(localBitmap, right))
				{
					Color rightColor = GetPixel(localBitmap, right);
					ColorAtPoint rightCAP = new ColorAtPoint(rightColor, right);
					if(!todo.Contains(rightCAP))
					{
						todo.Add(rightCAP);
					}
				}
				if(PointInRange(localBitmap, up))
				{
					Color upColor = GetPixel(localBitmap, up);
					ColorAtPoint upCAP = new ColorAtPoint(upColor, up);
					if(!todo.Contains(upCAP))
					{
						todo.Add(upCAP);
					}
				}
				if(PointInRange(localBitmap, down))
				{
					Color downColor = GetPixel(localBitmap, down);
					ColorAtPoint downCAP = new ColorAtPoint(downColor, down);
					if(!todo.Contains(downCAP))
					{
						todo.Add(downCAP);
					}
				}
			}

			return inRegion;
		}

		public static bool PointInRange(Bitmap bitmap, System.Drawing.Point point)
		{
			return (point.X >= 0 && point.X < bitmap.Width && point.Y >= 0 && point.Y < bitmap.Height);
		}
	}
}
