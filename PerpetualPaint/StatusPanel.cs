using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WithoutHaste.Windows.GUI;

namespace PerpetualPaint
{
	public class StatusPanel : Panel
	{
		private Label label;
		
		public string StatusText {
			get {
				return label.Text;
			}
			set {
				if(label == null)
					return;
				label.Text = value;
			}
		}
		
		public StatusPanel()
		{
			label = new Label();
			LayoutHelper.Fill(this, 5).Apply(label);
			label.Anchor = LayoutHelper.AnchorAll;
			label.Text = "";

			this.Controls.Add(label);
		}
	}
}
