using StbImageLib.Decoding;
using System.IO;

namespace StbImageLib
{
#if !STBSHARP_INTERNAL
	public
#else
	internal
#endif
	unsafe class ImageResult
	{
		public int Width { get; set; }
		public int Height { get; set; }
		public ColorComponents ColorComponents { get; set; }
		public ColorComponents SourceComponents { get; set; }

		/// <summary>
		/// Either 8 or 16
		/// </summary>
		public int BitsPerChannel { get; set; }
		public byte[] Data { get; set; }

		public static ImageResult FromStream(Stream stream, ColorComponents? requiredComponents = null)
		{
			if (JpgDecoder.Test(stream))
				return JpgDecoder.Decode(stream, requiredComponents);
			if (PngDecoder.Test(stream))
				return PngDecoder.Decode(stream, requiredComponents);
			if (BmpDecoder.Test(stream))
				return BmpDecoder.Decode(stream, requiredComponents);
			if (GifDecoder.Test(stream))
				return GifDecoder.Decode(stream, requiredComponents);
			if (PsdDecoder.Test(stream))
				return PsdDecoder.Decode(stream, requiredComponents);
			if (TgaDecoder.Test(stream))
				return TgaDecoder.Decode(stream, requiredComponents);

			Decoder.stbi__err("unknown image type");
			return null;
		}
	}
}