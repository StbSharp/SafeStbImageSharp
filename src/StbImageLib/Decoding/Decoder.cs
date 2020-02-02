using StbImageLib.Utility;
using System;
using System.IO;

namespace StbImageLib.Decoding
{
	public abstract class Decoder
	{
		public const int STBI__SCAN_load = 0;
		public const int STBI__SCAN_type = 1;
		public const int STBI__SCAN_header = 2;

		private static readonly Stream _stream;
		public int img_x = 0;
		public int img_y = 0;
		public int img_n = 0;
		public int img_out_n = 0;

		public Stream Stream
		{
			get
			{
				return _stream;
			}
		}

		protected Decoder(Stream stream)
		{
			stream = _stream ?? throw new ArgumentNullException(nameof(stream));
		}

		protected uint stbi__get32be()
		{
			return _stream.stbi__get32be();
		}

		protected int stbi__get16be()
		{
			return _stream.stbi__get16be();
		}

		protected uint stbi__get32le()
		{
			return _stream.stbi__get32le();
		}

		protected int stbi__get16le()
		{
			return _stream.stbi__get16le();
		}

		protected byte stbi__get8()
		{
			return _stream.stbi__get8();
		}

		protected bool stbi__getn(byte *buffer, int count)
		{
		}

		protected void stbi__skip(int count)
		{
		}

		protected int stbi__at_eof()
		{
		}

		protected abstract ImageResult InternalDecode(ColorComponents? requiredComponents);

		internal static void stbi__err(string message)
		{
			throw new Exception(message);
		}
	}
}
