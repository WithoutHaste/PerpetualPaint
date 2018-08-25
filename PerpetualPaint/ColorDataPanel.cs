using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WithoutHaste.Drawing.Colors;
using WithoutHaste.Windows.GUI;

namespace PerpetualPaint
{
	public class ColorDataPanel : Panel
	{
		private Color? color;
		public Color? Color {
			set {
				color = value;
				DisplayData();
			}
		}

		private TextBox hexadecimalData;
		private TextBox rgbData;
		private TextBox hsvData;

		public ColorDataPanel(bool readOnly = true)
		{
			this.Width = 150;
			this.Height = 200;

			Init(readOnly);
		}

		private void Init(bool readOnly)
		{
			int margin = 10;
			int controlHeight = 25;

			//todo: there must be a far more succint way of saying this in the LayoutHelper

			Label hexadecimalHeader = new Label();
			LayoutHelper.Left(this, margin).Top(this, margin).Width(this.Width).Height(controlHeight).Apply(hexadecimalHeader);
			hexadecimalHeader.Text = "Hexadecimal";
			this.Controls.Add(hexadecimalHeader);

			hexadecimalData = GetTextBox(readOnly);
			LayoutHelper.Left(this, margin * 2).Below(hexadecimalHeader).Width(this.Width - (margin * 2)).Height(controlHeight).Apply(hexadecimalData);
			hexadecimalData.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			this.Controls.Add(hexadecimalData);

			Label rgbHeader = new Label();
			LayoutHelper.MatchLeft(hexadecimalHeader).Below(hexadecimalData, margin).Width(this.Width).Height(controlHeight).Apply(rgbHeader);
			rgbHeader.Text = "Red, Green, Blue";
			this.Controls.Add(rgbHeader);

			rgbData = GetTextBox(readOnly);
			LayoutHelper.Left(this, margin * 2).Below(rgbHeader).Width(this.Width - (margin * 2)).Height(controlHeight).Apply(rgbData);
			rgbData.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			this.Controls.Add(rgbData);

			Label hsvHeader = new Label();
			LayoutHelper.MatchLeft(rgbHeader).Below(rgbData, margin).Width(this.Width).Height(controlHeight).Apply(hsvHeader);
			hsvHeader.Text = "Hue, Saturation, Value";
			this.Controls.Add(hsvHeader);

			hsvData = GetTextBox(readOnly);
			LayoutHelper.Left(this, margin * 2).Below(hsvHeader).Width(this.Width - (margin * 2)).Height(controlHeight).Apply(hsvData);
			hsvData.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			this.Controls.Add(hsvData);
		}

		private void DisplayData()
		{
			if(color == null)
			{
				hexadecimalData.Text = "";
				rgbData.Text = "";
				hsvData.Text = "";
				return;
			}

			hexadecimalData.Text = ConvertColors.HexadecimalFromColor(color.Value);
			rgbData.Text = String.Format("({0}, {1}, {2})", color.Value.R, color.Value.G, color.Value.B);
			HSV hsv = ConvertColors.HSVFromColor(color.Value);
			hsvData.Text = String.Format("({0}, {1}, {2})", (int)hsv.Hue, (int)(hsv.Saturation * 100), (int)(hsv.Value * 100));
		}

		private TextBox GetTextBox(bool readOnly)
		{
			if(readOnly)
				return GetReadOnlyTextBox();

			return new TextBox() {
				BorderStyle = BorderStyle.FixedSingle
			};
		}

		private TextBox GetReadOnlyTextBox()
		{
			return new TextBox() {
				ReadOnly = true,
				BorderStyle = 0,
				BackColor = this.BackColor,
				TabStop = false
			};
		}
	}
}
