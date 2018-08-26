using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WithoutHaste.Windows.GUI;

namespace PerpetualPaint
{
	public class RemovePaletteColorAction : IHistoryAction
	{
		public event PaletteEventHandler RemoveColor;
		public event PaletteEventHandler InsertColor;

		private Color color;
		private int index;

		public RemovePaletteColorAction(Color color, int index)
		{
			this.color = color;
			this.index = index;
		}

		public void Do()
		{
			if(RemoveColor == null) return;
			RemoveColor(this, new PaletteEventArgs(color, index));
		}

		public void Undo()
		{
			if(InsertColor == null) return;
			InsertColor(this, new PaletteEventArgs(color, index));
		}
	}
}
