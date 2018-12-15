using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerpetualPaintLibrary
{
	/// <summary>
	/// Perpetual Paint Project Configuration
	/// </summary>
	public class PPPConfig
	{
		public string PaletteFilename { get; set; }

		public PPPConfig(string paletteFilename)
		{
			PaletteFilename = paletteFilename;
		}

		public PPPConfig(string[] fileLines)
		{
			foreach(string line in fileLines)
			{
				if(line.StartsWith("paletteFilename="))
				{
					PaletteFilename = line.Split('=')[1];
				}
			}
		}

		public string[] ToTextFormat()
		{
			return new string[] {
				"paletteFilename=" + PaletteFilename
			};
		}
	}
}
