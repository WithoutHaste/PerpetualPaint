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
		public Image ICON_SELECTED_COLOR { get; protected set; }
		public Image ICON_ZOOM_100 { get; protected set; }
		public Image ICON_ZOOM_FIT { get; protected set; }
		public Image ICON_ZOOM_IN { get; protected set; }
		public Image ICON_ZOOM_OUT { get; protected set; }
		public Image ICON_REDO { get; protected set; }
		public Image ICON_UNDO { get; protected set; }

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
			ICON_SELECTED_COLOR = TryLoadIcon("resources/icons/icon_selector.png");
			ICON_ZOOM_100 = TryLoadIcon("resources/icons/icon_100.png");
			ICON_ZOOM_FIT = TryLoadIcon("resources/icons/icon_fit.png");
			ICON_ZOOM_IN = TryLoadIcon("resources/icons/icon_plus.png");
			ICON_ZOOM_OUT = TryLoadIcon("resources/icons/icon_minus.png");
			ICON_REDO = TryLoadIcon("resources/icons/icon_redo.png");
			ICON_UNDO = TryLoadIcon("resources/icons/icon_undo.png");
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
