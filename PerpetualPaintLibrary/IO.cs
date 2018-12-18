using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Packaging; //from WindowsBase.dll
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using WithoutHaste.Drawing.Colors;

namespace PerpetualPaintLibrary
{
	public static class IO
	{
		private static string PART_GREYSCALE = "/greyscale.bmp";
		private static string PART_COLOR = "/color.bmp";
		private static string PART_PALETTE = "/palette.gpl";
		private static string PART_CONFIG = "/config.txt";

		/// <summary>
		/// Saved a zipped Perpetual Paint Project file.
		/// </summary>
		/// <remarks>
		/// Creates or overwrites <paramref name='zipFilename'/>.
		/// 
		/// Handles:
		/// - include entire palette
		/// - include filename of palette (in config)
		/// - include no palette information
		/// </remarks>
		public static void ZipProject(string zipFilename, PPProject project)
		{
			byte[] zippedBytes;
			using(MemoryStream zipStream = new MemoryStream())
			{
				using(Package package = Package.Open(zipStream, FileMode.Create))
				{
					PackagePart greyscaleDocument = package.CreatePart(new Uri(PART_GREYSCALE, UriKind.Relative), "");
					using(MemoryStream dataStream = new MemoryStream(project.GreyscaleBitmap.ToByteArray()))
					{
						greyscaleDocument.GetStream().WriteAll(dataStream);
					}
					PackagePart colorDocument = package.CreatePart(new Uri(PART_COLOR, UriKind.Relative), "");
					using(MemoryStream dataStream = new MemoryStream(project.ColorBitmap.ToByteArray()))
					{
						colorDocument.GetStream().WriteAll(dataStream);
					}
					if(project.ColorPalette != null)
					{
						PackagePart paletteDocument = package.CreatePart(new Uri(PART_PALETTE, UriKind.Relative), "");
						using(MemoryStream dataStream = new MemoryStream(FormatGPL.ToTextFormat(project.ColorPalette).ToByteArray()))
						{
							paletteDocument.GetStream().WriteAll(dataStream);
						}
					}
					if(project.Config != null)
					{
						PackagePart configDocument = package.CreatePart(new Uri(PART_CONFIG, UriKind.Relative), "");
						using(MemoryStream dataStream = new MemoryStream(project.Config.ToTextFormat().ToByteArray()))
						{
							configDocument.GetStream().WriteAll(dataStream);
						}
					}
				}
				zippedBytes = zipStream.ToArray();
			}
			File.WriteAllBytes(zipFilename, zippedBytes);
		}

		public static void LoadProject(string zipFilename, PPProject project)
		{
			using(Package package = Package.Open(zipFilename, FileMode.Open))
			{
				if(!package.PartExists(new Uri(PART_GREYSCALE, UriKind.Relative)))
					throw new FileFormatException("Project zip file does not include greyscale image.");
				if(!package.PartExists(new Uri(PART_COLOR, UriKind.Relative)))
					throw new FileFormatException("Project zip file does not include color image.");
				project.GreyscaleBitmap = package.GetPart(new Uri(PART_GREYSCALE, UriKind.Relative)).GetStream().StreamToByteArray().ByteArrayToBitmap();
				project.ColorBitmap = package.GetPart(new Uri(PART_COLOR, UriKind.Relative)).GetStream().StreamToByteArray().ByteArrayToBitmap();
				project.ColorPalette = null;
				if(package.PartExists(new Uri(PART_PALETTE, UriKind.Relative)))
				{
					string[] paletteLines = package.GetPart(new Uri(PART_PALETTE, UriKind.Relative)).GetStream().StreamToByteArray().ByteArrayToText();
					FormatGPL gpl = new FormatGPL(paletteLines);
					project.ColorPalette = gpl.ColorPalette;
				}
				project.Config = null;
				if(package.PartExists(new Uri(PART_CONFIG, UriKind.Relative)))
				{
					string[] configLines = package.GetPart(new Uri(PART_CONFIG, UriKind.Relative)).GetStream().StreamToByteArray().ByteArrayToText();
					project.Config = new PPPConfig(configLines);
				}
			}
		}

		public static PPProject LoadProject(string zipFilename)
		{
			PPProject project = new PPProject();
			LoadProject(zipFilename, project);
			return project;
		}

		private static void WriteAll(this Stream target, Stream source)
		{
			const int bufSize = 0x1000;
			byte[] buf = new byte[bufSize];
			int bytesRead = 0;
			while((bytesRead = source.Read(buf, 0, bufSize)) > 0)
				target.Write(buf, 0, bytesRead);
		}

		//todo: add all these convesions to code notes

		private static byte[] ToByteArray(this Bitmap bitmap)
		{
			ImageConverter converter = new ImageConverter();
			return (byte[])converter.ConvertTo(bitmap, typeof(byte[]));
		}

		private static byte[] ToByteArray(this string[] text)
		{
			byte[] data = Encoding.UTF8.GetBytes(String.Join("\n", text));
			return data;
		}

		private static byte[] ObjectToByteArray(this Object o)
		{
			BinaryFormatter formatter = new BinaryFormatter();
			using(MemoryStream stream = new MemoryStream())
			{
				formatter.Serialize(stream, o);
				return stream.ToArray();
			}
		}

		private static byte[] StreamToByteArray(this Stream input)
		{
			using(MemoryStream stream = new MemoryStream())
			{
				input.CopyTo(stream);
				return stream.ToArray();
			}
		}

		private static Object ByteArrayToObject(this byte[] data)
		{
			using(MemoryStream stream = new MemoryStream())
			{
				BinaryFormatter formatter = new BinaryFormatter();
				stream.Write(data, 0, data.Length);
				stream.Seek(0, SeekOrigin.Begin);
				Object obj = formatter.Deserialize(stream);
				return obj;
			}
		}

		private static Bitmap ByteArrayToBitmap(this byte[] data)
		{
			using(MemoryStream stream = new MemoryStream(data))
			{
				return new Bitmap(stream);
			}
		}

		private static string[] ByteArrayToText(this byte[] data)
		{
			return System.Text.Encoding.UTF8.GetString(data).Split('\n');
		}
	}
}
