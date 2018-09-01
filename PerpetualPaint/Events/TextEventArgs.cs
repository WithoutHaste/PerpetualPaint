using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PerpetualPaintLibrary;

namespace PerpetualPaint
{
	public delegate void TextEventHandler(object sender, TextEventArgs e);

	public class TextEventArgs : EventArgs
	{
		public string Text { get; set; }

		public TextEventArgs(string text) : base()
		{
			Text = text;
		}
	}
}
