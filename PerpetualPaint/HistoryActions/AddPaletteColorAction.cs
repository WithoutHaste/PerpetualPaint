using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WithoutHaste.Windows.GUI;

namespace PerpetualPaint
{
	public class AddPaletteColorAction : IHistoryAction
	{
		public event PaletteEventHandler InsertColor;
		public event PaletteEventHandler RemoveColor;

		private Color color;
		private int index;

		public AddPaletteColorAction(Color color, int index)
		{
			this.color = color;
			this.index = index;
		}

		public void Do()
		{
			if(InsertColor == null) return;
			InsertColor(this, new PaletteEventArgs(color, index));
		}

		public void Undo()
		{
			if(RemoveColor == null) return;
			RemoveColor(this, new PaletteEventArgs(color, index));
		}
	}
}
