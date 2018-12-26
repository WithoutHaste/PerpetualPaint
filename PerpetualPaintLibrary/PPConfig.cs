using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WithoutHaste.Drawing.Colors;

namespace PerpetualPaintLibrary
{
	/// <summary>
	/// Perpetual Paint Configuration, for projects and collections.
	/// </summary>
	public class PPConfig
	{
		public enum PaletteOptions : int {
			/// <summary>Don't save any information about the palette with the project.</summary>
			SaveNothing = 0,
			/// <summary>Save the filename of the palette with the project.</summary>
			SaveFileName = 1,
			/// <summary>Save the entire palette file with the project.</summary>
			SaveFile = 2,
		};

		/// <summary>What information to save about the palette.</summary>
		public PaletteOptions PaletteOption { get; set; }

		/// <summary>If PaletteOption is set to SaveFilename, this is the filename that will be saved.</summary>
		public string PaletteFileName { get; set; }

		/// <summary>
		/// Initialize configuration by specifying values.
		/// </summary>
		public PPConfig(PaletteOptions paletteOption = PaletteOptions.SaveNothing, string paletteFileName = null)
		{
			PaletteOption = paletteOption;
			PaletteFileName = paletteFileName;
		}

		/// <summary>
		/// Initialize configuration from lines from a saved file.
		/// </summary>
		public PPConfig(string[] fileLines)
		{
			foreach(string line in fileLines)
			{
				string[] fields = line.Split('=');
				if(fields.Length < 2) continue;

				if(line.StartsWith("paletteOption="))
				{
					int value = (int)PaletteOptions.SaveNothing;
					Int32.TryParse(fields[1], out value);
					PaletteOption = (PaletteOptions)value;
				}
				else if(line.StartsWith("paletteFileName="))
				{
					PaletteFileName = fields[1];
				}
			}
		}

		/// <summary>
		/// Returns the lines of save to the text-based configuration file.
		/// </summary>
		public string[] ToTextFormat()
		{
			return new string[] {
				"paletteOption=" + (int)PaletteOption,
				"paletteFileName=" + PaletteFileName
			};
		}
	}
}
