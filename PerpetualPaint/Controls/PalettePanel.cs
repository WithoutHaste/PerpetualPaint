using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WithoutHaste.Windows.GUI;
using WithoutHaste.Drawing.Colors;

namespace PerpetualPaint
{
	/// <summary>
	/// Reusable panel to display palette, with scroll and resize.
	/// </summary>
	public class PalettePanel : Panel
	{
		/// <summary>
		/// Triggers when an application-setting is changed.
		/// </summary>
		public event EventHandler SettingChanged;

		public ColorPalette ColorPalette { get; protected set; }

		public Color? SelectedColor {
			get { return colorPalettePanel.SelectedColor; }
			set { colorPalettePanel.SelectedColor = value; }
		}

		public string PaletteFileName { get; protected set; }

		public int SwatchesPerRow { get; protected set; }
		
		private const int MIN_SWATCHES_PER_ROW = 3;
		private const int MAX_SWATCHES_PER_ROW = 12;
		private int palettePadding = 15;
		private ColorPalettePanel colorPalettePanel;

		public PalettePanel(int swatchesPerRow = 0)
		{
			SwatchesPerRow = (swatchesPerRow <= 0) ? 4 : swatchesPerRow;

			InitControls();
		}

		public void InitControls()
		{
			int scrollBarBuffer = System.Windows.Forms.SystemInformation.VerticalScrollBarWidth + 5;
			int swatchesWidth = (ColorPalettePanel.SWATCH_WIDTH * SwatchesPerRow) + scrollBarBuffer;
			int paletteWidth = swatchesWidth + (2 * palettePadding);

			this.Width = paletteWidth;
			this.Height = 300;

			Button narrowPaletteButton = new Button();
			narrowPaletteButton.Text = "<<";
			LayoutHelper.Bottom(this, palettePadding).Left(this, palettePadding).Width(ColorPalettePanel.SWATCH_WIDTH).Height(ColorPalettePanel.SWATCH_WIDTH).Apply(narrowPaletteButton);
			narrowPaletteButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
			narrowPaletteButton.Click += new EventHandler(Form_OnNarrowPalette);
			this.Controls.Add(narrowPaletteButton);

			Button widenPaletteButton = new Button();
			widenPaletteButton.Text = ">>";
			LayoutHelper.Bottom(this, palettePadding).Right(this, palettePadding).Width(ColorPalettePanel.SWATCH_WIDTH).Height(ColorPalettePanel.SWATCH_WIDTH).Apply(widenPaletteButton);
			widenPaletteButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			widenPaletteButton.Click += new EventHandler(Form_OnWidenPalette);
			this.Controls.Add(widenPaletteButton);

			colorPalettePanel = new ColorPalettePanel();
			LayoutHelper.Top(this).MatchLeft(narrowPaletteButton).MatchRight(widenPaletteButton).Above(narrowPaletteButton, palettePadding).Apply(colorPalettePanel);
			colorPalettePanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
			this.Controls.Add(colorPalettePanel);
		}

		private void Form_OnNarrowPalette(object sender, EventArgs e)
		{
			if(SwatchesPerRow == MIN_SWATCHES_PER_ROW) return;

			SwatchesPerRow--;
			TriggerSettingChanged();
			DisplayPalette();
		}

		private void Form_OnWidenPalette(object sender, EventArgs e)
		{
			if(SwatchesPerRow == MAX_SWATCHES_PER_ROW) return;

			SwatchesPerRow++;
			TriggerSettingChanged();
			DisplayPalette();
		}

		private void TriggerSettingChanged()
		{
			if(SettingChanged == null) return;
			SettingChanged(this, new EventArgs());
		}

		public void Set(ColorPalette colorPalette)
		{
			ColorPalette = colorPalette;
			PaletteFileName = null;
			TriggerSettingChanged();
			DisplayPalette();
		}

		public void Set(string paletteFileName)
		{
			if(paletteFileName == null)
				ColorPalette = new ColorPalette();
			else
				ColorPalette = WithoutHaste.Drawing.Colors.ColorPalette.Load(paletteFileName);
			PaletteFileName = paletteFileName;
			TriggerSettingChanged();
			DisplayPalette();
		}

		private void DisplayPalette()
		{
			int paletteWidth = (SwatchesPerRow * ColorPalettePanel.SWATCH_WIDTH) + ColorPalettePanel.SCROLLBAR_WIDTH;
			int panelWidth = paletteWidth + (2 * palettePadding);

			this.Width = panelWidth;
			colorPalettePanel.Width = paletteWidth;
			
			colorPalettePanel.DisplayColors(ColorPalette);
		}

	}
}
