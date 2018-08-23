using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerpetualPaint
{
	public struct RequestColorWorkerResult
	{
		public readonly Bitmap Bitmap;
		public readonly ColorAtPoint NewWhite;
		public readonly ColorAtPoint PreviousWhite;

		public RequestColorWorkerResult(Bitmap bitmap, ColorAtPoint newWhite, ColorAtPoint previousWhite)
		{
			Bitmap = bitmap;
			NewWhite = newWhite;
			PreviousWhite = previousWhite;
		}
	}
}
