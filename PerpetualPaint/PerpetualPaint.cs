using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PerpetualPaint
{
	static class PerpetualPaint
	{
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
			OneImageForm form = new OneImageForm();
			Application.Run(form);
		}
	}
}
