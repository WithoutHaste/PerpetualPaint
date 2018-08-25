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

		public delegate void OnColorChange(Color color);
		public OnColorChange ColorChangeFunc;

		private TextBox hexadecimalData;
		private TextBox rgbData;
		private TextBox hsvData;

		private const string NAME_HEXADECIMAL = "hexadecimal";
		private const string NAME_RGB = "rgb";
		private const string NAME_HSV = "hsv";

		private Color COLOR_NO_ERROR = System.Drawing.Color.White;
		private Color COLOR_ERROR = System.Drawing.Color.FromArgb(255, 255, 206, 206);

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
			hexadecimalData.Name = NAME_HEXADECIMAL;
			LayoutHelper.Left(this, margin * 2).Below(hexadecimalHeader).Width(this.Width - (margin * 2)).Height(controlHeight).Apply(hexadecimalData);
			hexadecimalData.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			this.Controls.Add(hexadecimalData);

			Label rgbHeader = new Label();
			LayoutHelper.MatchLeft(hexadecimalHeader).Below(hexadecimalData, margin).Width(this.Width).Height(controlHeight).Apply(rgbHeader);
			rgbHeader.Text = "Red, Green, Blue";
			this.Controls.Add(rgbHeader);

			rgbData = GetTextBox(readOnly);
			rgbData.Name = NAME_RGB;
			LayoutHelper.Left(this, margin * 2).Below(rgbHeader).Width(this.Width - (margin * 2)).Height(controlHeight).Apply(rgbData);
			rgbData.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			this.Controls.Add(rgbData);

			Label hsvHeader = new Label();
			LayoutHelper.MatchLeft(rgbHeader).Below(rgbData, margin).Width(this.Width).Height(controlHeight).Apply(hsvHeader);
			hsvHeader.Text = "Hue, Saturation, Value";
			this.Controls.Add(hsvHeader);

			hsvData = GetTextBox(readOnly);
			hsvData.Name = NAME_HSV;
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

			//only update textbox if necessary
			//use case: intending to write "255,255,255", when you reach "255,255,2" the text gets replaced
			Color tempColor = System.Drawing.Color.Black;
			if(!ConvertColors.TryParseHexadecimal(hexadecimalData.Text, out tempColor) || tempColor != color)
			{
				SetTextWithoutEvent(hexadecimalData, ConvertColors.HexadecimalFromColor(color.Value));
			}
			if(!ConvertColors.TryParseRGB(rgbData.Text, out tempColor) || tempColor != color)
			{
				SetTextWithoutEvent(rgbData, String.Format("({0}, {1}, {2})", color.Value.R, color.Value.G, color.Value.B));
			}
			if(!ConvertColors.TryParseHSV(hsvData.Text, out tempColor) || tempColor != color)
			{
				HSV hsv = ConvertColors.HSVFromColor(color.Value);
				SetTextWithoutEvent(hsvData, String.Format("({0}, {1}, {2})", (int)hsv.Hue, (int)(hsv.Saturation * 100), (int)(hsv.Value * 100)));
			}

			if(!hexadecimalData.ReadOnly)
			{
				hexadecimalData.BackColor = COLOR_NO_ERROR;
				rgbData.BackColor = COLOR_NO_ERROR;
				hsvData.BackColor = COLOR_NO_ERROR;
			}
		}

		private void SetTextWithoutEvent(TextBox textBox, string text)
		{
			textBox.TextChanged -= new EventHandler(OnTextChanged);
			textBox.Text = text;
			if(textBox.ReadOnly)
			{
				return;
			}
			textBox.TextChanged += new EventHandler(OnTextChanged);
		}

		private TextBox GetTextBox(bool readOnly)
		{
			if(readOnly)
				return GetReadOnlyTextBox();

			TextBox textBox = new TextBox() {
				BorderStyle = BorderStyle.FixedSingle,
				BackColor = COLOR_NO_ERROR
			};
			textBox.TextChanged += new EventHandler(OnTextChanged);
			return textBox;
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

		private void OnTextChanged(object sender, EventArgs e)
		{
			TextBox textBox = sender as TextBox;
			Color newColor = System.Drawing.Color.Black;
			bool success = false;
			switch(textBox.Name)
			{
				case NAME_HEXADECIMAL:
					success = ConvertColors.TryParseHexadecimal(textBox.Text, out newColor);
					break;
				case NAME_RGB:
					success = ConvertColors.TryParseRGB(textBox.Text, out newColor);
					break;
				case NAME_HSV:
					success = ConvertColors.TryParseHSV(textBox.Text, out newColor);
					break;
			}
			if(success)
			{
				Color = newColor;
				if(ColorChangeFunc != null)
				{
					ColorChangeFunc(newColor);
				}
			}
			else
			{
				textBox.BackColor = COLOR_ERROR;
			}
		}
	}
}
