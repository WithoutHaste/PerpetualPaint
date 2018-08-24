using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerpetualPaint
{
	public class IconManager
	{
		public static Image ICON_SELECTED_COLOR { get { return Singleton.icon_selected_color; } }
		public static Image ICON_ZOOM_100 { get { return Singleton.icon_zoom_100; } }
		public static Image ICON_ZOOM_FIT { get { return Singleton.icon_zoom_fit; } }
		public static Image ICON_ZOOM_IN { get { return Singleton.icon_zoom_in; } }
		public static Image ICON_ZOOM_OUT { get { return Singleton.icon_zoom_out; } }
		public static Image ICON_REDO { get { return Singleton.icon_redo; } }
		public static Image ICON_UNDO { get { return Singleton.icon_undo; } }

		private Image icon_selected_color;
		private Image icon_zoom_100;
		private Image icon_zoom_fit;
		private Image icon_zoom_in;
		private Image icon_zoom_out;
		private Image icon_redo;
		private Image icon_undo;

		private static IconManager singleton;
		public static IconManager Singleton {
			get {
				if(singleton == null)
				{
					singleton = new IconManager();
				}
				return singleton;
			}
		}

		private IconManager()
		{
			icon_selected_color = TryLoadIcon("resources/icons/icon_selector.png");
			icon_zoom_100 = TryLoadIcon("resources/icons/icon_100.png");
			icon_zoom_fit = TryLoadIcon("resources/icons/icon_fit.png");
			icon_zoom_in = TryLoadIcon("resources/icons/icon_plus.png");
			icon_zoom_out = TryLoadIcon("resources/icons/icon_minus.png");
			icon_redo = TryLoadIcon("resources/icons/icon_redo.png");
			icon_undo = TryLoadIcon("resources/icons/icon_undo.png");
		}

		private Image TryLoadIcon(string fullFilename)
		{
			try
			{
				return Image.FromFile(fullFilename);
			}
			catch(Exception)
			{
				return SystemIcons.Question.ToBitmap();
			}
		}
	}
}
