using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WithoutHaste.Drawing.Colors;
using WithoutHaste.Windows.GUI;

namespace PerpetualPaintLibrary
{
	/// <summary>
	/// Perpetual Paint Project: a single image, with its greyscale version, and possibly a palette.
	/// </summary>
	public class PPProject
	{
		public static readonly string PROJECT_EXTENSION = ".ppp";
		//public static readonly string PROJECT_EXTENSION_UPPERCASE = PROJECT_EXTENSION.ToUpper();

		public Bitmap GreyscaleBitmap;
		public Bitmap ColorBitmap;
		public ColorPalette ColorPalette;
		public PPConfig Config;

		public string SaveToFileName { get; protected set; }

		public bool EditedSinceLastSave { get; protected set; }

		public string LoadedFromFileName { get; protected set; }

		public PPProject()
		{
		}

		public PPProject(Bitmap greyscaleBitmap, Bitmap colorBitmap)
		{
			GreyscaleBitmap = greyscaleBitmap;
			ColorBitmap = colorBitmap;
			ColorPalette = null;
			Config = null;
		}

		public PPProject(Bitmap greyscaleBitmap, Bitmap colorBitmap, ColorPalette colorPalette)
		{
			GreyscaleBitmap = greyscaleBitmap;
			ColorBitmap = colorBitmap;
			ColorPalette = colorPalette;
			Config = null;
		}

		public PPProject(Bitmap greyscaleBitmap, Bitmap colorBitmap, PPConfig config)
		{
			GreyscaleBitmap = greyscaleBitmap;
			ColorBitmap = colorBitmap;
			ColorPalette = null;
			Config = config;
		}

		/// <summary>
		/// Create new project from file.
		/// </summary>
		public static PPProject FromProject(string fullFileName)
		{
			PPProject project = new PPProject();
			project.LoadProject(fullFileName);
			return project;
		}

		/// <summary>
		/// Load existing project from file.
		/// </summary>
		public void LoadProject(string fullFileName)
		{
			IO.LoadProject(fullFileName, this);
			EditedSinceLastSave = false;
			SaveToFileName = fullFileName;
			LoadedFromFileName = fullFileName;
		}

		/// <summary>
		/// Create new project with image loaded.
		/// </summary>
		public static PPProject FromImage(string fullFileName)
		{
			PPProject project = new PPProject();
			project.LoadImage(fullFileName);
			return project;
		}

		/// <summary>
		/// Load new project from an image file.
		/// </summary>
		/// <returns>Returns True if image is greyscale, False if image is colored.</returns>
		public bool LoadImage(string fullFileName)
		{
			Bitmap bitmap = ImageHelper.SafeLoadBitmap(fullFileName);
			if(bitmap.Width == 0 && bitmap.Height == 0)
				throw new NotSupportedException("Cannot operate on a 0-pixel by 0-pixel bitmap.");

			ColorPalette = null;
			Config = new PPConfig();
			bool imageIsGreyscale = Utilities.BitmapIsGreyscale(bitmap);
			if(imageIsGreyscale)
			{
				GreyscaleBitmap = bitmap;
				ColorBitmap = new Bitmap(bitmap);
			}
			else
			{
				GreyscaleBitmap = null; //todo: cannot save until this is set to a bitmap
				ColorBitmap = bitmap;
			}
			EditedSinceLastSave = true;
			SaveToFileName = null; //do not assume file name should be the same as the image file name
			LoadedFromFileName = fullFileName;

			return imageIsGreyscale;
		}

		public void SetFileName(string fullFileName)
		{
			SaveToFileName = fullFileName;
			EditedSinceLastSave = true;
		}

		/// <summary>
		/// Save project file as <paramref name='fullFileName'/>.
		/// </summary>
		public void SaveAs(string fullFileName)
		{
			SaveToFileName = fullFileName;
			Save();
		}

		/// <summary>
		/// Save project file.
		/// </summary>
		public void Save()
		{
			if(String.IsNullOrEmpty(SaveToFileName))
				throw new FileNotFoundException("Cannot save to an empty file name.");
			if(Path.GetExtension(SaveToFileName) != PROJECT_EXTENSION)
				SaveToFileName = Path.ChangeExtension(SaveToFileName, PROJECT_EXTENSION);
			IO.ZipProject(SaveToFileName, this);
			EditedSinceLastSave = false;
		}

		/// <summary>Returns a thumbnail of the <see cref='ColorBitmap'/>.</summary>
		/// <remarks>
		/// If <see cref='ColorBitmap'/> is not available, it will use <see cref='GreyscaleBitmap'/> instead.
		/// If neither is available, will use a blank image.
		/// </remarks>
		public Bitmap GetThumbnail(int maxWidth, int maxHeight)
		{
			Bitmap origin = (ColorBitmap ?? GreyscaleBitmap) ?? new Bitmap(100, 100);
			return ImageHelper.GetThumbnail(origin, maxWidth, maxHeight);
		}

		/// <summary>
		/// Returns true if these projects are from the same file.
		/// </summary>
		public bool FromSameFile(PPProject other)
		{
			if(!String.IsNullOrEmpty(this.LoadedFromFileName))
				return (this.LoadedFromFileName == other.LoadedFromFileName);
			return false;
		}

		/// <summary>
		/// Tells the project it has been edited.
		/// </summary>
		public void Edited()
		{
			EditedSinceLastSave = true;
		}

		/// <summary>
		/// Set palette settings and values.
		/// </summary>
		public void SetPaletteOption(PPConfig.PaletteOptions paletteOption, WithoutHaste.Drawing.Colors.ColorPalette colorPalette = null, string paletteFileName = null)
		{
			Config.PaletteOption = paletteOption;
			switch(Config.PaletteOption)
			{
				case PPConfig.PaletteOptions.SaveNothing:
					Config.PaletteFileName = null;
					ColorPalette = null;
					break;
				case PPConfig.PaletteOptions.SaveFile:
					Config.PaletteFileName = null;
					ColorPalette = colorPalette;
					break;
				case PPConfig.PaletteOptions.SaveFileName:
					Config.PaletteFileName = paletteFileName;
					ColorPalette = null;
					break;
			}
			EditedSinceLastSave = true;
		}

		public void UpdatePaletteOption(WithoutHaste.Drawing.Colors.ColorPalette colorPalette = null, string paletteFileName = null)
		{
			switch(Config.PaletteOption)
			{
				case PPConfig.PaletteOptions.SaveNothing:
					return;
				case PPConfig.PaletteOptions.SaveFile:
					ColorPalette = colorPalette;
					break;
				case PPConfig.PaletteOptions.SaveFileName:
					Config.PaletteFileName = paletteFileName;
					break;
			}
			EditedSinceLastSave = true;
		}
	}
}
