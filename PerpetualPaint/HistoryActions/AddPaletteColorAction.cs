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
		public delegate void DoOperation(Color color, int index);
		public delegate void UndoOperation(int index);
		public static DoOperation DoFunc;
		public static UndoOperation UndoFunc;

		private Color color;
		private int index;

		public AddPaletteColorAction(Color color, int index)
		{
			this.color = color;
			this.index = index;
		}

		public void Do()
		{
			DoFunc(color, index);
		}

		public void Undo()
		{
			UndoFunc(index);
		}
	}
}
