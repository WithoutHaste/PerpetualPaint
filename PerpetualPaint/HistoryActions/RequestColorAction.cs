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
		public delegate void OnDo(ColorAtPoint cap);
		public static OnDo DoFunc;

		private ColorAtPoint newCap;
		private ColorAtPoint oldCap;

		public RequestColorAction(ColorAtPoint newCap, ColorAtPoint oldCap)
		{
			this.newCap = newCap;
			this.oldCap = oldCap;
		}

		public void Do()
		{
			DoFunc(newCap.ToNoHistory());
		}

		public void Undo()
		{
			DoFunc(oldCap.ToNoHistory());
		}
	}
}
