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
		public static readonly int SCROLLBAR_WIDTH = System.Windows.Forms.SystemInformation.VerticalScrollBarWidth + 5;
		public static readonly int SWATCH_WIDTH = 25;

		private Color? selectedColor = null;
		private EventHandler onClickColor;
		private ContextMenu colorContextMenu;

		public Color? SelectedColor {
			get {
				return selectedColor;
			}
		}

		public SwatchPanel()
		{
			Init();
		}

		public SwatchPanel(EventHandler onClickColor)
		{
			Init();
			this.onClickColor = onClickColor;
		}

		public SwatchPanel(ColorPalette colorPalette, EventHandler onClickColor=null, ContextMenu colorContextMenu=null)
		{
			Init();
			this.onClickColor = onClickColor;
			this.colorContextMenu = colorContextMenu;
			DisplayColors(colorPalette);
		}

		private void Init()
		{
			this.AutoScroll = true;
			this.BorderStyle = BorderStyle.Fixed3D;
		}

		public void DisplayColors(ColorPalette colorPalette)
		{
			this.SuspendLayout();

			this.Controls.Clear();
			int count = 0;
			foreach(Color color in colorPalette.Colors)
			{
				if(selectedColor == null)
				{
					selectedColor = color;
				}

				Panel colorPanel = new Panel();
				colorPanel.TabIndex = count; //corresponds to index in color palette
				colorPanel.Size = new Size(SWATCH_WIDTH, SWATCH_WIDTH);
				colorPanel.Padding = new Padding(0);
				colorPanel.Margin = new Padding(0);
				colorPanel.BackColor = color;
				if(color == selectedColor)
				{
					colorPanel.BackgroundImage = IconManager.ICON_SELECTED_COLOR;
					colorPanel.BackgroundImageLayout = ImageLayout.Stretch;
				}
				colorPanel.Cursor = Cursors.Hand;
				colorPanel.Click += new EventHandler(Color_OnClick);
				if(colorContextMenu != null)
				{
					colorPanel.ContextMenu = colorContextMenu;
				}
				if(onClickColor != null)
				{
					colorPanel.Click += new EventHandler(onClickColor);
				}
				this.Controls.Add(colorPanel);
				count++;
			}

			this.ResumeLayout();
		}

		public void Color_OnClick(object sender, EventArgs e)
		{
			selectedColor = (sender as Panel).BackColor;
			foreach(Control child in this.Controls)
			{
				if(child.BackColor == selectedColor)
				{
					child.BackgroundImage = IconManager.ICON_SELECTED_COLOR;
				}
				else
				{
					child.BackgroundImage = null;
				}
			}
		}
	}
}
