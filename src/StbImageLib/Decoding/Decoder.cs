using StbImageLib.Utility;
using System;
using System.IO;

namespace StbImageLib.Decoding
{
	public unsafe class Decoder
	{
		public const int STBI__SCAN_load = 0;
		public const int STBI__SCAN_type = 1;
		public const int STBI__SCAN_header = 2;
		protected int img_x = 0;
		protected int img_y = 0;
		protected int img_n = 0;

		public Stream Stream { get; private set; }

		protected Decoder(Stream stream)
		{
			Stream = stream ?? throw new ArgumentNullException(nameof(stream));
		}

		protected uint stbi__get32be()
		{
			return Stream.stbi__get32be();
		}

		protected int stbi__get16be()
		{
			return Stream.stbi__get16be();
		}

		protected uint stbi__get32le()
		{
			return Stream.stbi__get32le();
		}

		protected int stbi__get16le()
		{
			return Stream.stbi__get16le();
		}

		protected byte stbi__get8()
		{
			return Stream.stbi__get8();
		}

		protected bool stbi__getn(byte[] buffer, int offset, int count)
		{
			var read = Stream.Read(buffer, offset, count);

			return read == count;
		}

		protected void stbi__skip(int count)
		{
			Stream.stbi__skip(count);
		}

		protected bool stbi__at_eof()
		{
			return Stream.Position == Stream.Length;
		}

		internal static void stbi__err(string message)
		{
			throw new Exception(message);
		}

		public static ImageResult Decode(Stream stream)
		{
			if (JpgDecoder.Test(stream))
				return JpgDecoder.Decode(stream);
			if (PngDecoder.Test(stream))
				return PngDecoder.Decode(stream);
			if (BmpDecoder.Test(stream))
				return BmpDecoder.Decode(stream);
			if (GifDecoder.Test(stream))
				return GifDecoder.Decode(stream);
			if (PsdDecoder.Test(stream))
				return PsdDecoder.Decode(stream);
			if (TgaDecoder.Test(stream))
				return TgaDecoder.Decode(stream);

			stbi__err("unknown image type");
			return null;
		}

		public static ImageInfo? Info(Stream stream)
		{
			var info = JpgDecoder.Info(stream);
			if (info != null)
			{
				return info;
			}

			info = PngDecoder.Info(stream);
			if (info != null)
			{
				return info;
			}

			info = GifDecoder.Info(stream);
			if (info != null)
			{
				return info;
			}

			info = BmpDecoder.Info(stream);
			if (info != null)
			{
				return info;
			}

			info = PsdDecoder.Info(stream);
			if (info != null)
			{
				return info;
			}

			info = TgaDecoder.Info(stream);
			if (info != null)
			{
				return info;
			}

			return null;
		}
	}
}
