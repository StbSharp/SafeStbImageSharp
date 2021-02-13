using NUnit.Framework;
using StbImageSharp.Tests.Utility;
using System.IO;
using System.Reflection;

namespace StbImageSharp.Tests
{
	[TestFixture]
	public class Tests
	{
		private static readonly Assembly _assembly = typeof(Tests).Assembly;

		[TestCase("DockPanes.jpg", 2000, 609, 406, ColorComponents.RedGreenBlue, false)]
		public void Info(string filename, int headerSize, int width, int height, ColorComponents colorComponents, bool is16bit)
		{
			ImageInfo? result;

			var data = new byte[headerSize];
			using (var stream = _assembly.OpenResourceStream(filename))
			{
				stream.Read(data, 0, data.Length);
			}

			using (var stream = new MemoryStream(data))
			{
				result = ImageInfo.FromStream(stream);
			}

			Assert.IsNotNull(result);

			var info = result.Value;
			Assert.AreEqual(info.Width, width);
			Assert.AreEqual(info.Height, height);
			Assert.AreEqual(info.ColorComponents, colorComponents);
			Assert.AreEqual(info.BitsPerChannel, is16bit ? 16 : 8);
		}
	}
}