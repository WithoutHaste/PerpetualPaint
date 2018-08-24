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
	//todo: should this be in WithoutHaste.Windows.GUI?
	public class NewColorDialog : Form
	{
		private HuePanel huePanel;
		private SaturationValuePanel saturationValuePanel;
		private Panel selectedColorPanel;

		public Color Color { get; protected set; }

		public NewColorDialog(Color? startColor=null)
		{
			int margin = 10;
			if(startColor.HasValue)
			{
				Color = startColor.Value;
			}

			this.Width = (HuePanel.UNIT + margin + margin/*why second margin needed?*/) * 2;
			this.Height = 500 + margin;
			this.Text = "New Color";

			huePanel = new HuePanel(Color);
			huePanel.Location = new Point(margin, margin);
			huePanel.OnColorChange = Hue_OnChange;
			this.Controls.Add(huePanel);

			saturationValuePanel = new SaturationValuePanel(Color);
			saturationValuePanel.Location = new Point(margin, huePanel.Location.Y + huePanel.Height + margin);
			saturationValuePanel.OnColorChange = SaturationValue_OnChange;
			this.Controls.Add(saturationValuePanel);

			Button okButton = new Button();
			LayoutHelper.RightOf(saturationValuePanel, margin).Bottom(this, margin).Width(100).Height(25).Apply(okButton);
			okButton.Text = "Ok";
			okButton.Click += new EventHandler(Ok_OnClick);
			this.Controls.Add(okButton);

			Button cancelButton = new Button();
			LayoutHelper.RightOf(okButton, margin).Bottom(this, margin).Width(100).Height(25).Apply(cancelButton);
			cancelButton.Text = "Cancel";
			cancelButton.Click += new EventHandler(Cancel_OnClick);
			this.Controls.Add(cancelButton);

			selectedColorPanel = new Panel();
			LayoutHelper.RightOf(saturationValuePanel, margin).Below(huePanel, margin).Right(this, margin).Above(okButton, margin).Apply(selectedColorPanel);
			UpdateSelectedColor();
			this.Controls.Add(selectedColorPanel);
		}

		private void Ok_OnClick(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		private void Cancel_OnClick(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}

		private void Hue_OnChange(object sender, EventArgs e)
		{
			saturationValuePanel.Hue = huePanel.Hue;
			UpdateSelectedColor();
		}

		private void SaturationValue_OnChange(object sender, EventArgs e)
		{
			UpdateSelectedColor();
		}

		private void UpdateSelectedColor()
		{
			Color = ConvertColors.ColorFromHSV(new HSV(huePanel.Hue, saturationValuePanel.Saturation / 100f, saturationValuePanel.Value / 100f));
			selectedColorPanel.BackColor = Color;
		}
	}

	internal class HuePanel : Panel
	{
		public const int UNIT = 360;
		private const int TRACKBAR_HEIGHT = 45;

		private TrackBar trackBar;

		public int Hue {
			get {
				return trackBar.Value;
			}
			set {
				trackBar.Value = value;
			}
		}

		public EventHandler OnColorChange { get; set; }

		public HuePanel(Color? startColor=null)
		{
			this.Height = TRACKBAR_HEIGHT + 50;
			this.Width = UNIT * 2;
			this.BorderStyle = BorderStyle.Fixed3D;

			trackBar = new TrackBar();
			trackBar.Location = new Point(0, 0);
			trackBar.Size = new Size(this.Width, TRACKBAR_HEIGHT);
			trackBar.Minimum = 0;
			trackBar.Maximum = 359;
			trackBar.ValueChanged += new EventHandler(TrackBar_OnValueChanged);
			this.Controls.Add(trackBar);

			int swatchWidth = this.Width / UNIT;
			int swatchY = trackBar.Location.Y + trackBar.Height;
			for(int hue = 0; hue < 360; hue++)
			{
				Panel colorPanel = new Panel();
				colorPanel.Location = new Point(hue * swatchWidth, swatchY);
				colorPanel.Size = new Size(swatchWidth, this.Height);
				colorPanel.BackColor = ConvertColors.ColorFromHSV(new HSV(hue, 1, 1));
				colorPanel.Click += new EventHandler(Color_OnClick);
				this.Controls.Add(colorPanel);
			}

			if(startColor.HasValue)
			{
				Hue = (int)ConvertColors.HSVFromColor(startColor.Value).Hue;
			}
		}

		private void Color_OnClick(object sender, EventArgs e)
		{
			Panel panel = sender as Panel;
			HSV hsv = ConvertColors.HSVFromColor(panel.BackColor);
			Hue = (int)hsv.Hue;
		}

		private void TrackBar_OnValueChanged(object sender, EventArgs e)
		{
			if(OnColorChange == null)
				return;
			OnColorChange(this, new EventArgs());
		}
	}

	internal class SaturationValuePanel : Panel
	{
		public const int UNIT = 101;
		private const int TRACKBAR_HEIGHT = 45;

		private TrackBar saturationTrackBar;
		private TrackBar valueTrackBar;
		private SaturationValueGradientPanel gradientPanel;

		public EventHandler OnColorChange;

		public int Saturation {
			get {
				return saturationTrackBar.Value;
			}
			set {
				saturationTrackBar.Value = value;
			}
		}

		public int Value {
			get {
				return valueTrackBar.Value;
			}
			set {
				valueTrackBar.Value = value;
			}
		}

		private int hue;
		public int Hue {
			get {
				return hue;
			}
			set {
				hue = value;
				gradientPanel.Hue = hue;
			}
		}

		public SaturationValuePanel(Color? startColor=null)
		{
			int totalColorHeight = UNIT * 3;

			this.Height = TRACKBAR_HEIGHT + totalColorHeight;
			this.Width = TRACKBAR_HEIGHT + totalColorHeight;
			this.BorderStyle = BorderStyle.Fixed3D;

			saturationTrackBar = new TrackBar();
			saturationTrackBar.Location = new Point(TRACKBAR_HEIGHT, 0);
			saturationTrackBar.Size = new Size(totalColorHeight, TRACKBAR_HEIGHT);
			saturationTrackBar.Minimum = 0;
			saturationTrackBar.Maximum = 100;
			saturationTrackBar.ValueChanged += new EventHandler(Saturation_OnValueChanged);
			this.Controls.Add(saturationTrackBar);

			valueTrackBar = new TrackBar();
			valueTrackBar.Location = new Point(0, TRACKBAR_HEIGHT);
			valueTrackBar.Size = new Size(TRACKBAR_HEIGHT, totalColorHeight);
			valueTrackBar.Minimum = 0;
			valueTrackBar.Maximum = 100;
			valueTrackBar.Orientation = Orientation.Vertical;
			valueTrackBar.ValueChanged += new EventHandler(Value_OnValueChanged);
			this.Controls.Add(valueTrackBar);

			gradientPanel = new SaturationValueGradientPanel(totalColorHeight);
			gradientPanel.Location = new Point(TRACKBAR_HEIGHT, TRACKBAR_HEIGHT);
			gradientPanel.OnColorChange = Gradient_OnColorChange;
			this.Controls.Add(gradientPanel);

			if(startColor.HasValue)
			{
				HSV hsv = ConvertColors.HSVFromColor(startColor.Value);
				Hue = (int)hsv.Hue;
				Saturation = (int)(hsv.Saturation * 100);
				Value = (int)(hsv.Value * 100);
			}
		}

		private void Gradient_OnColorChange(object sender, EventArgs e)
		{
			Saturation = gradientPanel.Saturation;
			Value = gradientPanel.Value;
		}

		private void Saturation_OnValueChanged(object sender, EventArgs e)
		{
			if(OnColorChange != null)
				OnColorChange(this, new EventArgs());
		}

		private void Value_OnValueChanged(object sender, EventArgs e)
		{
			if(OnColorChange != null)
				OnColorChange(this, new EventArgs());
		}
	}

	internal class SaturationValueGradientPanel : Panel
	{
		private int hue;
		public int Hue {
			set {
				hue = value;
				this.Invalidate();
			}
		}

		private int saturation;
		public int Saturation {
			get {
				return saturation;
			}
			set {
				saturation = value;
			}
		}

		private int _value;
		public int Value {
			get {
				return _value;
			}
			set {
				_value = value;
			}
		}

		public EventHandler OnColorChange;

		private int swatchWidth;
		private Bitmap graphicsBitmap;

		public SaturationValueGradientPanel(int width)
		{
			this.Width = width;
			this.Height = width;
			this.Click += new EventHandler(Gradient_OnClick);

			swatchWidth = this.Width / 101;
			hue = 0;
			saturation = 0;
			_value = 0;
		}

		private void Gradient_OnClick(object sender, EventArgs e)
		{
			Point clickPoint = this.PointToClient(new Point(MousePosition.X, MousePosition.Y));
			Color color = graphicsBitmap.GetPixel(clickPoint.X, clickPoint.Y);
			HSV hsv = ConvertColors.HSVFromColor(color);
			saturation = (int)(100 * hsv.Saturation);
			_value = (int)(100 * hsv.Value);
			if(OnColorChange != null)
			{
				OnColorChange(this, new EventArgs());
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			graphicsBitmap = new Bitmap(this.Width, this.Height);
			using(Graphics gBitmap = Graphics.FromImage(graphicsBitmap))
			{
				for(int saturation = 0; saturation <= 100; saturation++)
				{
					for(int value = 0; value <= 100; value++)
					{
						int reverseValue = 100 - value;
						Color color = ConvertColors.ColorFromHSV(new HSV(hue, saturation / 100f, reverseValue / 100f));
						SolidBrush brush = new SolidBrush(color);
						gBitmap.FillRectangle(brush, new Rectangle(saturation * swatchWidth, value * swatchWidth, swatchWidth, swatchWidth));
					}
				}
			}

			e.Graphics.DrawImage(graphicsBitmap, 0, 0);
		}
	}
}
