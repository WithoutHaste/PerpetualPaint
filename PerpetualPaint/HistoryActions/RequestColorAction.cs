using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WithoutHaste.Windows.GUI;

namespace PerpetualPaint
{
	public class RequestColorAction : IHistoryAction
	{
		public event ColorEventHandler Action;

		private ColorAtPoint newCap;
		private ColorAtPoint oldCap;

		public RequestColorAction(ColorAtPoint newCap, ColorAtPoint oldCap)
		{
			this.newCap = newCap;
			this.oldCap = oldCap;
		}

		public void Do()
		{
			if(Action == null) return;
			Action(this, new ColorEventArgs(newCap.ToNoHistory()));
		}

		public void Undo()
		{
			if(Action == null) return;
			Action(this, new ColorEventArgs(oldCap.ToNoHistory()));
		}
	}
}
