using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WithoutHaste.Windows.GUI;

namespace PerpetualPaintTest
{
	//[TestClass]
	public class Benchmark
	{
		private const string benchmarkFilename = "benchmark.txt";
		private const string catFilename = "../../../../Disarray/resources_TeaShop/test_1.png";
		private const string rabbitFilename = "../../../PerpetualPaint/resources/images/sample_rabbit.png";

		private static StreamWriter benchmarkWriter;

		[ClassInitialize]
		public static void Benchmark_Initialize(TestContext context)
		{
			benchmarkWriter = new StreamWriter(benchmarkFilename);
		}

		private void Benchmark_CountPixels(string label, string filename)
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Reset();
			stopwatch.Start();
			Bitmap bitmap = ImageHelper.SafeLoadBitmap(filename);
			int count = CountPixels(bitmap);
			stopwatch.Stop();
			benchmarkWriter.WriteLine("CountPixels: {0}: {1} pixels in {2}", label, count, stopwatch.Elapsed);
		}

		[TestMethod]
		public void Benchmark_Cat_CountPixels()
		{
			Benchmark_CountPixels("Cat", catFilename);
		}

		[TestMethod]
		public void Benchmark_Rabbit_CountPixels()
		{
			Benchmark_CountPixels("Rabbit", rabbitFilename);
		}

		private void Benchmark_MakeOnePointPerPixel(string label, string filename)
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Reset();
			stopwatch.Start();
			Bitmap bitmap = ImageHelper.SafeLoadBitmap(filename);
			int count = MakeOnePointPerPixel(bitmap);
			stopwatch.Stop();
			benchmarkWriter.WriteLine("MakeOnePointPerPixel: {0}: {1} points in {2}", label, count, stopwatch.Elapsed);
		}

		[TestMethod]
		public void Benchmark_Cat_MakeOnePointPerPixel()
		{
			Benchmark_MakeOnePointPerPixel("Cat", catFilename);
		}

		[TestMethod]
		public void Benchmark_Rabbit_MakeOnePointPerPixel()
		{
			Benchmark_MakeOnePointPerPixel("Rabbit", rabbitFilename);
		}

		private void Benchmark_GetEachPixel(string label, string filename)
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Reset();
			stopwatch.Start();
			Bitmap bitmap = ImageHelper.SafeLoadBitmap(filename);
			int count = GetEachPixel(bitmap);
			stopwatch.Stop();
			benchmarkWriter.WriteLine("GetEachPixel: {0}: {1} colors in {2}", label, count, stopwatch.Elapsed);
		}

		[TestMethod]
		public void Benchmark_Cat_GetEachPixel()
		{
			Benchmark_GetEachPixel("Cat", catFilename);
		}

		[TestMethod]
		public void Benchmark_Rabbit_GetEachPixel()
		{
			Benchmark_GetEachPixel("Rabbit", rabbitFilename);
		}

		private void Benchmark_SetUnionEachPixel(string label, string filename)
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Reset();
			stopwatch.Start();
			Bitmap bitmap = ImageHelper.SafeLoadBitmap(filename);
			int count = SetUnionEachPixel(bitmap);
			stopwatch.Stop();
			benchmarkWriter.WriteLine("SetUnionEachPixel: {0}: {1} colors in {2}", label, count, stopwatch.Elapsed);
		}

		[TestMethod]
		public void Benchmark_Cat_SetUnionEachPixel()
		{
			Benchmark_SetUnionEachPixel("Cat", catFilename);
		}

		[TestMethod]
		public void Benchmark_Rabbit_SetUnionEachPixell()
		{
			Benchmark_SetUnionEachPixel("Rabbit", rabbitFilename);
		}

		[ClassCleanup]
		public static void Benchmark_Cleanup()
		{
			benchmarkWriter.Close();
			benchmarkWriter.Dispose();
		}

		private int CountPixels(Bitmap bitmap)
		{
			int count = 0;
			for(int x = 0; x < bitmap.Width; x++)
			{
				for(int y = 0; y < bitmap.Height; y++)
				{
					count++;
				}
			}
			return count;
		}

		private int MakeOnePointPerPixel(Bitmap bitmap)
		{
			for(int x = 0; x < bitmap.Width; x++)
			{
				for(int y = 0; y < bitmap.Height; y++)
				{
					Point p = new Point(x, y);
				}
			}
			return bitmap.Width * bitmap.Height;
		}

		private int GetEachPixel(Bitmap bitmap)
		{
			for(int x = 0; x < bitmap.Width; x++)
			{
				for(int y = 0; y < bitmap.Height; y++)
				{
					Color color = bitmap.GetPixel(x, y);
				}
			}
			return bitmap.Width * bitmap.Height;
		}

		private int SetUnionEachPixel(Bitmap bitmap)
		{
			HashSet<Point> points = new HashSet<Point>();
			for(int x = 0; x < bitmap.Width; x++)
			{
				for(int y = 0; y < bitmap.Height; y++)
				{
					Point point = new Point(x, y);
					HashSet<Point> onePoint = new HashSet<Point>() { point };
					points.UnionWith(onePoint);
				}
			}
			return bitmap.Width * bitmap.Height;
		}
	}
}
