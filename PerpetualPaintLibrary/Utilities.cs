using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WithoutHaste.Drawing.Colors;

namespace PerpetualPaintLibrary
{
    public static class Utilities
    {
		public static int MAX_RGB_AS_BLACK = 50;
		public static int MIN_RGB_AS_WHITE = 230;

		public static Color GrayscaleToColor(Color grayscale, Color newColor)
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
				return newColor;
			}
			HSV grayscaleHSV = ConvertColors.HSVFromColor(grayscale);
			HSV newColorHSV = ConvertColors.HSVFromColor(newColor);
			float adjustedValue = grayscaleHSV.Value * newColorHSV.Value * 0.5f;
			float adjustedSaturation = grayscaleHSV.Value * newColorHSV.Saturation * 0.5f; //cut adjusted saturation in half to force it into the gray range
			HSV adjustedHSV = new HSV(newColorHSV.Hue, adjustedSaturation, adjustedValue);
			Color adjustedColor = ConvertColors.ColorFromHSV(adjustedHSV);
			return adjustedColor;
		}

		public static Color ColorToGrayscale(Color color, Color whiteColor)
		{
			if(ColorIsGrayscale(color))
			{
				return color;
			}
			if(color == whiteColor)
			{
				return Color.White;
			}
			HSV oldHSV = ConvertColors.HSVFromColor(color);
			HSV oldWhite = ConvertColors.HSVFromColor(whiteColor);
			float adjustedValue = oldHSV.Value / oldWhite.Value / 0.5f;
			if(adjustedValue > 1)
				adjustedValue = 1;
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
	}
}
