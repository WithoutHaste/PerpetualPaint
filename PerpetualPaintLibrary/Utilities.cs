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
				Color color = ConvertColors.ColorFromRGB(MAX_RGB_AS_BLACK, MAX_RGB_AS_BLACK, MAX_RGB_AS_BLACK);
				HSV hsv = ConvertColors.HSVFromColor(color);
				return hsv.Value;
			}
		}

		public static int MIN_RGB_AS_WHITE = 230;
		private static float MIN_VALUE_AS_WHITE {
			get {
				Color color = ConvertColors.ColorFromRGB(MIN_RGB_AS_WHITE, MIN_RGB_AS_WHITE, MIN_RGB_AS_WHITE);
				HSV hsv = ConvertColors.HSVFromColor(color);
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
			HSV grayscaleHSV = ConvertColors.HSVFromColor(grayscale);
			HSV pureColorHSV = ConvertColors.HSVFromColor(pureColor);

			Range fullRange = new Range(MAX_VALUE_AS_BLACK, 1);
			Range newRange = new Range(MAX_VALUE_AS_BLACK, pureColorHSV.Value);
			float adjustedValue = (float)Range.ConvertValue(fullRange, newRange, grayscaleHSV.Value);

			float adjustedSaturation = grayscaleHSV.Value * pureColorHSV.Saturation;

			HSV adjustedHSV = new HSV(pureColorHSV.Hue, adjustedSaturation, adjustedValue);
			Color adjustedColor = ConvertColors.ColorFromHSV(adjustedHSV);
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
			HSV colorHSV = ConvertColors.HSVFromColor(color);
			HSV pureColorHSV = ConvertColors.HSVFromColor(pureColor);

			Range fullRange = new Range(MAX_VALUE_AS_BLACK, 1);
			Range newRange = new Range(MAX_VALUE_AS_BLACK, pureColorHSV.Value);
			float adjustedValue = (float)Range.ConvertValue(newRange, fullRange, colorHSV.Value);

			HSV adjustedHSV = new HSV(0, 0, adjustedValue);
			Color adjustedColor = ConvertColors.ColorFromHSV(adjustedHSV);
			return adjustedColor;
		}

		private static bool ColorIsClear(Color color)
		{
			return (color.A == 0);
		}

		private static bool ColorIsPartiallyClear(Color color)
		{
			return (color.A < 255);
		}

		private static bool ColorIsBlack(Color color)
		{
			if(ColorIsPartiallyClear(color)) return false;
			return (ColorIsGrayscale(color) && color.R < MAX_RGB_AS_BLACK);
		}

		private static bool ColorIsGrayscale(Color color)
		{
			if(ColorIsPartiallyClear(color)) return true;
			return (color.R == color.G && color.G == color.B);
		}

		private static bool ColorIsWhite(Color color)
		{
			if(ColorIsClear(color)) return true;
			return (ColorIsGrayscale(color) && color.R > MIN_RGB_AS_WHITE);
		}

		private static Color ConvertPartiallyClearToGray(Color oldColor)
		{
			//25% solid => 75% gray
			return ConvertColors.ColorFromHSV(0, 0, (255 - oldColor.A) / 255f);
		}

		private static float ConvertRange(Range largeRange, Range smallRange, float value)
		{
			if(largeRange.Start != smallRange.Start)
				throw new NotImplementedException("Not implemented: ConvertRange when ranges have different minimum values.");

			double scale = smallRange.Span / largeRange.Span;
			return (float)(((value - largeRange.Start) * scale) + largeRange.Start);
		}
	}
}
