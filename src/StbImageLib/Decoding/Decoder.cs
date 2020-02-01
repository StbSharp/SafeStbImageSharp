using System;
using System.Runtime.InteropServices;

namespace StbImageLib.Decoding
{
	public unsafe class Decoder
	{
		public const int STBI__SCAN_load = 0;
		public const int STBI__SCAN_type = 1;
		public const int STBI__SCAN_header = 2;

		[StructLayout(LayoutKind.Sequential)]
		public struct stbi__result_info
		{
			public int bits_per_channel;
			public int num_channels;
			public int channel_order;
		}

		public uint img_x = 0;
		public uint img_y = 0;
		public int img_n = 0;
		public int img_out_n = 0;
		public void* io_user_data;
		public int read_from_callbacks = 0;
		public int buflen = 0;
		public byte* buffer_start = (byte*)CRuntime.malloc(128);
		public byte* img_buffer;
		public byte* img_buffer_end;
		public byte* img_buffer_original;
		public byte* img_buffer_original_end;

		protected uint stbi__get32be()
		{

		}

		protected ushort stbi__get16be()
		{

		}

		protected byte stbi__get8()
		{
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

		internal static void stbi__err(string message)
		{
			throw new Exception(message);
		}
	}
}
