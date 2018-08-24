using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WithoutHaste.Drawing.Colors;

namespace PerpetualPaint
{
	public class SwatchPanel : FlowLayoutPanel
	{
		public readonly int SCROLLBAR_WIDTH = System.Windows.Forms.SystemInformation.VerticalScrollBarWidth + 5;
		public const int SWATCH_WIDTH = 25;

		private Color? selectedColor = null;
		private EventHandler onClickColor;

		private readonly Image IMAGE_SELECTED_COLOR = Image.FromFile("resources/icons/icon_selector.png");

		public SwatchPanel(ColorPalette colorPalette, EventHandler onClickColor=null)
		{
			this.AutoScroll = true;
			this.BorderStyle = BorderStyle.Fixed3D;

			this.onClickColor = onClickColor;
			DisplayColors(colorPalette);
		}

		public void DisplayColors(ColorPalette colorPalette)
		{
			this.Controls.Clear();
			foreach(Color color in colorPalette.Colors)
			{
				if(selectedColor == null)
				{
					selectedColor = color;
				}

				Panel colorPanel = new Panel();
				colorPanel.Size = new Size(SWATCH_WIDTH, SWATCH_WIDTH);
				colorPanel.Padding = new Padding(0);
				colorPanel.Margin = new Padding(0);
				colorPanel.BackColor = color;
				if(color == selectedColor)
				{
					colorPanel.BackgroundImage = IMAGE_SELECTED_COLOR;
					colorPanel.BackgroundImageLayout = ImageLayout.Stretch;
				}
				colorPanel.Cursor = Cursors.Hand;
				colorPanel.Click += new EventHandler(Color_OnClick);
				if(onClickColor != null)
				{
					colorPanel.Click += new EventHandler(onClickColor);
				}
				this.Controls.Add(colorPanel);
			}
		}

		public void Color_OnClick(object sender, EventArgs e)
		{
			selectedColor = (sender as Panel).BackColor;
			foreach(Control child in this.Controls)
			{
				if(child.BackColor == selectedColor)
				{
					child.BackgroundImage = IMAGE_SELECTED_COLOR;
				}
				else
				{
					child.BackgroundImage = null;
				}
			}
		}
	}
}
