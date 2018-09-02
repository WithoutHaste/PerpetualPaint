using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerpetualPaintLibrary
{
	public class RequestRegionArgument
	{
		public Bitmap Bitmap;

		public RequestRegionArgument(Bitmap bitmap)
		{
			Bitmap = bitmap;
		}
	}
}
