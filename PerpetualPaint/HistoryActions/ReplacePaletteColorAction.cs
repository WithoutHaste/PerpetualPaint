using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WithoutHaste.Windows.GUI;

namespace PerpetualPaint
{
	public class ReplacePaletteColorAction : IHistoryAction
	{
		public event PaletteEventHandler ReplaceColor;

		private Color oldColor;
		private Color newColor;
		private int index;

		public ReplacePaletteColorAction(int index, Color oldColor, Color newColor)
		{
			this.oldColor = oldColor;
			this.newColor = newColor;
			this.index = index;
		}

		public void Do()
		{
			if(ReplaceColor == null) return;
			ReplaceColor(this, new PaletteEventArgs(newColor, index));
		}

		public void Undo()
		{
			if(ReplaceColor == null) return;
			ReplaceColor(this, new PaletteEventArgs(oldColor, index));
		}
	}
}
