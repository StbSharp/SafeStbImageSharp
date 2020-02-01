using System;
using System.Runtime.InteropServices;

namespace StbImageLib
{
	unsafe partial class StbImage
	{
		public const int STBI_default = 0;
		public const int STBI_grey = 1;
		public const int STBI_grey_alpha = 2;
		public const int STBI_rgb = 3;
		public const int STBI_rgb_alpha = 4;

		public const int STBI_ORDER_RGB = 0;
		public const int STBI_ORDER_BGR = 1;

		[StructLayout(LayoutKind.Sequential)]
		public struct stbi__bmp_data
		{
			public int bpp;
			public int offset;
			public int hsz;
			public uint mr;
			public uint mg;
			public uint mb;
			public uint ma;
			public uint all_a;
		}

		public static void stbi__start_mem(stbi__context s, byte* buffer, int len)
		{
			s.io.read = (null);
			s.read_from_callbacks = (int)(0);
			s.img_buffer = s.img_buffer_original = buffer;
			s.img_buffer_end = s.img_buffer_original_end = buffer + len;
		}

		public static void stbi__start_callbacks(stbi__context s, stbi_io_callbacks c, void* user)
		{
			s.io = (stbi_io_callbacks)(c);
			s.io_user_data = user;
			s.buflen = (int)(128);
			s.read_from_callbacks = (int)(1);
			s.img_buffer_original = s.buffer_start;
			stbi__refill_buffer(s);
			s.img_buffer_original_end = s.img_buffer_end;
		}

		public static void stbi__rewind(stbi__context s)
		{
			s.img_buffer = s.img_buffer_original;
			s.img_buffer_end = s.img_buffer_original_end;
		}

		public static void stbi_set_flip_vertically_on_load(int flag_true_if_should_flip)
		{
			stbi__vertically_flip_on_load = (int)(flag_true_if_should_flip);
		}

		public static void* stbi__load_main(stbi__context s, int* x, int* y, int* comp, int req_comp, stbi__result_info* ri, int bpc)
		{
			ri->bits_per_channel = (int)(8);
			ri->channel_order = (int)(STBI_ORDER_RGB);
			ri->num_channels = (int)(0);
			if ((stbi__jpeg_test(s)) != 0)
				return stbi__jpeg_load(s, x, y, comp, (int)(req_comp), ri);
			if ((stbi__png_test(s)) != 0)
				return stbi__png_load(s, x, y, comp, (int)(req_comp), ri);
			if ((stbi__bmp_test(s)) != 0)
				return stbi__bmp_load(s, x, y, comp, (int)(req_comp), ri);
			if ((stbi__gif_test(s)) != 0)
				return stbi__gif_load(s, x, y, comp, (int)(req_comp), ri);
			if ((stbi__psd_test(s)) != 0)
				return stbi__psd_load(s, x, y, comp, (int)(req_comp), ri, (int)(bpc));
			if ((stbi__tga_test(s)) != 0)
				return stbi__tga_load(s, x, y, comp, (int)(req_comp), ri);
			return ((byte*)((ulong)((stbi__err("unknown image type")) != 0 ? ((byte*)null) : (null))));
		}

		public static byte* stbi__convert_16_to_8(ushort* orig, int w, int h, int channels)
		{
			int i = 0;
			int img_len = (int)(w * h * channels);
			byte* reduced;
			reduced = (byte*)(stbi__malloc((ulong)(img_len)));
			if ((reduced) == (null))
				return ((byte*)((ulong)((stbi__err("outofmem")) != 0 ? ((byte*)null) : (null))));
			for (i = (int)(0); (i) < (img_len); ++i)
			{
				reduced[i] = ((byte)((orig[i] >> 8) & 0xFF));
			}
			CRuntime.free(orig);
			return reduced;
		}

		public static ushort* stbi__convert_8_to_16(byte* orig, int w, int h, int channels)
		{
			int i = 0;
			int img_len = (int)(w * h * channels);
			ushort* enlarged;
			enlarged = (ushort*)(stbi__malloc((ulong)(img_len * 2)));
			if ((enlarged) == (null))
				return (ushort*)((byte*)((ulong)((stbi__err("outofmem")) != 0 ? ((byte*)null) : (null))));
			for (i = (int)(0); (i) < (img_len); ++i)
			{
				enlarged[i] = ((ushort)((orig[i] << 8) + orig[i]));
			}
			CRuntime.free(orig);
			return enlarged;
		}

		public static void stbi__vertical_flip(void* image, int w, int h, int bytes_per_pixel)
		{
			int row = 0;
			ulong bytes_per_row = (ulong)(w * bytes_per_pixel);
			byte* temp = stackalloc byte[2048];
			byte* bytes = (byte*)(image);
			for (row = (int)(0); (row) < (h >> 1); row++)
			{
				byte* row0 = bytes + (ulong)row * bytes_per_row;
				byte* row1 = bytes + (ulong)(h - row - 1) * bytes_per_row;
				ulong bytes_left = (ulong)(bytes_per_row);
				while ((bytes_left) != 0)
				{
					ulong bytes_copy = (ulong)(((bytes_left) < (2048)) ? bytes_left : 2048);
					CRuntime.memcpy(temp, row0, (ulong)(bytes_copy));
					CRuntime.memcpy(row0, row1, (ulong)(bytes_copy));
					CRuntime.memcpy(row1, temp, (ulong)(bytes_copy));
					row0 += bytes_copy;
					row1 += bytes_copy;
					bytes_left -= (ulong)(bytes_copy);
				}
			}
		}

		public static void stbi__vertical_flip_slices(void* image, int w, int h, int z, int bytes_per_pixel)
		{
			int slice = 0;
			int slice_size = (int)(w * h * bytes_per_pixel);
			byte* bytes = (byte*)(image);
			for (slice = (int)(0); (slice) < (z); ++slice)
			{
				stbi__vertical_flip(bytes, (int)(w), (int)(h), (int)(bytes_per_pixel));
				bytes += slice_size;
			}
		}

		public static byte* stbi__load_and_postprocess_8bit(stbi__context s, int* x, int* y, int* comp, int req_comp)
		{
			stbi__result_info ri = new stbi__result_info();
			void* result = stbi__load_main(s, x, y, comp, (int)(req_comp), &ri, (int)(8));
			if ((result) == (null))
				return (null);
			if (ri.bits_per_channel != 8)
			{
				result = stbi__convert_16_to_8((ushort*)(result), (int)(*x), (int)(*y), (int)((req_comp) == (0) ? *comp : req_comp));
				ri.bits_per_channel = (int)(8);
			}

			if ((stbi__vertically_flip_on_load) != 0)
			{
				int channels = (int)((req_comp) != 0 ? req_comp : *comp);
				stbi__vertical_flip(result, (int)(*x), (int)(*y), (int)(channels * sizeof(byte)));
			}

			return (byte*)(result);
		}

		public static ushort* stbi__load_and_postprocess_16bit(stbi__context s, int* x, int* y, int* comp, int req_comp)
		{
			stbi__result_info ri = new stbi__result_info();
			void* result = stbi__load_main(s, x, y, comp, (int)(req_comp), &ri, (int)(16));
			if ((result) == (null))
				return (null);
			if (ri.bits_per_channel != 16)
			{
				result = stbi__convert_8_to_16((byte*)(result), (int)(*x), (int)(*y), (int)((req_comp) == (0) ? *comp : req_comp));
				ri.bits_per_channel = (int)(16);
			}

			if ((stbi__vertically_flip_on_load) != 0)
			{
				int channels = (int)((req_comp) != 0 ? req_comp : *comp);
				stbi__vertical_flip(result, (int)(*x), (int)(*y), (int)(channels * sizeof(ushort)));
			}

			return (ushort*)(result);
		}

		public static ushort* stbi_load_16_from_memory(byte* buffer, int len, int* x, int* y, int* channels_in_file, int desired_channels)
		{
			stbi__context s = new stbi__context();
			stbi__start_mem(s, buffer, (int)(len));
			return stbi__load_and_postprocess_16bit(s, x, y, channels_in_file, (int)(desired_channels));
		}

		public static ushort* stbi_load_16_from_callbacks(stbi_io_callbacks clbk, void* user, int* x, int* y, int* channels_in_file, int desired_channels)
		{
			stbi__context s = new stbi__context();
			stbi__start_callbacks(s, clbk, user);
			return stbi__load_and_postprocess_16bit(s, x, y, channels_in_file, (int)(desired_channels));
		}

		public static byte* stbi_load_from_memory(byte* buffer, int len, int* x, int* y, int* comp, int req_comp)
		{
			stbi__context s = new stbi__context();
			stbi__start_mem(s, buffer, (int)(len));
			return stbi__load_and_postprocess_8bit(s, x, y, comp, (int)(req_comp));
		}

		public static byte* stbi_load_from_callbacks(stbi_io_callbacks clbk, void* user, int* x, int* y, int* comp, int req_comp)
		{
			stbi__context s = new stbi__context();
			stbi__start_callbacks(s, clbk, user);
			return stbi__load_and_postprocess_8bit(s, x, y, comp, (int)(req_comp));
		}

		public static byte* stbi_load_gif_from_memory(byte* buffer, int len, int** delays, int* x, int* y, int* z, int* comp, int req_comp)
		{
			byte* result;
			stbi__context s = new stbi__context();
			stbi__start_mem(s, buffer, (int)(len));
			result = (byte*)(stbi__load_gif_main(s, delays, x, y, z, comp, (int)(req_comp)));
			if ((stbi__vertically_flip_on_load) != 0)
			{
				stbi__vertical_flip_slices(result, (int)(*x), (int)(*y), (int)(*z), (int)(*comp));
			}

			return result;
		}

		public static void stbi__refill_buffer(stbi__context s)
		{
			int n = (int)(s.io.read(s.io_user_data, (sbyte*)(s.buffer_start), (int)(s.buflen)));
			if ((n) == (0))
			{
				s.read_from_callbacks = (int)(0);
				s.img_buffer = s.buffer_start;
				s.img_buffer_end = s.buffer_start;
				s.img_buffer_end++;
				*s.img_buffer = (byte)(0);
			}
			else
			{
				s.img_buffer = s.buffer_start;
				s.img_buffer_end = s.buffer_start;
				s.img_buffer_end += n;
			}

		}

		public static byte stbi__get8(stbi__context s)
		{
			if ((s.img_buffer) < (s.img_buffer_end))
				return (byte)(*s.img_buffer++);
			if ((s.read_from_callbacks) != 0)
			{
				stbi__refill_buffer(s);
				return (byte)(*s.img_buffer++);
			}

			return (byte)(0);
		}

		public static int stbi__at_eof(stbi__context s)
		{
			if ((s.io.read) != null)
			{
				if (s.io.eof(s.io_user_data) == 0)
					return (int)(0);
				if ((s.read_from_callbacks) == (0))
					return (int)(1);
			}

			return (int)((s.img_buffer) >= (s.img_buffer_end) ? 1 : 0);
		}

		public static void stbi__skip(stbi__context s, int n)
		{
			if ((n) < (0))
			{
				s.img_buffer = s.img_buffer_end;
				return;
			}

			if ((s.io.read) != null)
			{
				int blen = (int)(s.img_buffer_end - s.img_buffer);
				if ((blen) < (n))
				{
					s.img_buffer = s.img_buffer_end;
					s.io.skip(s.io_user_data, (int)(n - blen));
					return;
				}
			}

			s.img_buffer += n;
		}

		public static int stbi__getn(stbi__context s, byte* buffer, int n)
		{
			if ((s.io.read) != null)
			{
				int blen = (int)(s.img_buffer_end - s.img_buffer);
				if ((blen) < (n))
				{
					int res = 0;
					int count = 0;
					CRuntime.memcpy(buffer, s.img_buffer, (ulong)(blen));
					count = (int)(s.io.read(s.io_user_data, (sbyte*)(buffer) + blen, (int)(n - blen)));
					res = (int)((count) == (n - blen) ? 1 : 0);
					s.img_buffer = s.img_buffer_end;
					return (int)(res);
				}
			}

			if (s.img_buffer + n <= s.img_buffer_end)
			{
				CRuntime.memcpy(buffer, s.img_buffer, (ulong)(n));
				s.img_buffer += n;
				return (int)(1);
			}
			else
				return (int)(0);
		}

		public static int stbi__get16be(stbi__context s)
		{
			int z = (int)(stbi__get8(s));
			return (int)((z << 8) + stbi__get8(s));
		}

		public static uint stbi__get32be(stbi__context s)
		{
			uint z = (uint)(stbi__get16be(s));
			return (uint)((z << 16) + stbi__get16be(s));
		}

		public static int stbi__get16le(stbi__context s)
		{
			int z = (int)(stbi__get8(s));
			return (int)(z + (stbi__get8(s) << 8));
		}

		public static uint stbi__get32le(stbi__context s)
		{
			uint z = (uint)(stbi__get16le(s));
			return (uint)(z + (stbi__get16le(s) << 16));
		}

		public static int stbi__bmp_info(stbi__context s, int* x, int* y, int* comp)
		{
			void* p;
			stbi__bmp_data info = new stbi__bmp_data();
			info.all_a = (uint)(255);
			p = stbi__bmp_parse_header(s, &info);
			stbi__rewind(s);
			if ((p) == (null))
				return (int)(0);
			if ((x) != null)
				*x = (int)(s.img_x);
			if ((y) != null)
				*y = (int)(s.img_y);
			if ((comp) != null)
				*comp = (int)((info.ma) != 0 ? 4 : 3);
			return (int)(1);
		}

		public static int stbi__psd_info(stbi__context s, int* x, int* y, int* comp)
		{
			int channelCount = 0;
			int dummy = 0;
			int depth = 0;
			if (x == null)
				x = &dummy;
			if (y == null)
				y = &dummy;
			if (comp == null)
				comp = &dummy;
			if (stbi__get32be(s) != 0x38425053)
			{
				stbi__rewind(s);
				return (int)(0);
			}

			if (stbi__get16be(s) != 1)
			{
				stbi__rewind(s);
				return (int)(0);
			}

			stbi__skip(s, (int)(6));
			channelCount = (int)(stbi__get16be(s));
			if (((channelCount) < (0)) || ((channelCount) > (16)))
			{
				stbi__rewind(s);
				return (int)(0);
			}

			*y = (int)(stbi__get32be(s));
			*x = (int)(stbi__get32be(s));
			depth = (int)(stbi__get16be(s));
			if ((depth != 8) && (depth != 16))
			{
				stbi__rewind(s);
				return (int)(0);
			}

			if (stbi__get16be(s) != 3)
			{
				stbi__rewind(s);
				return (int)(0);
			}

			*comp = (int)(4);
			return (int)(1);
		}

		public static int stbi__psd_is16(stbi__context s)
		{
			int channelCount = 0;
			int depth = 0;
			if (stbi__get32be(s) != 0x38425053)
			{
				stbi__rewind(s);
				return (int)(0);
			}

			if (stbi__get16be(s) != 1)
			{
				stbi__rewind(s);
				return (int)(0);
			}

			stbi__skip(s, (int)(6));
			channelCount = (int)(stbi__get16be(s));
			if (((channelCount) < (0)) || ((channelCount) > (16)))
			{
				stbi__rewind(s);
				return (int)(0);
			}

			stbi__get32be(s);
			stbi__get32be(s);
			depth = (int)(stbi__get16be(s));
			if (depth != 16)
			{
				stbi__rewind(s);
				return (int)(0);
			}

			return (int)(1);
		}

		public static int stbi__info_main(stbi__context s, int* x, int* y, int* comp)
		{
			if ((stbi__jpeg_info(s, x, y, comp)) != 0)
				return (int)(1);
			if ((stbi__png_info(s, x, y, comp)) != 0)
				return (int)(1);
			if ((stbi__gif_info(s, x, y, comp)) != 0)
				return (int)(1);
			if ((stbi__bmp_info(s, x, y, comp)) != 0)
				return (int)(1);
			if ((stbi__psd_info(s, x, y, comp)) != 0)
				return (int)(1);
			if ((stbi__tga_info(s, x, y, comp)) != 0)
				return (int)(1);
			return (int)(stbi__err("unknown image type"));
		}

		public static int stbi__is_16_main(stbi__context s)
		{
			if ((stbi__png_is16(s)) != 0)
				return (int)(1);
			if ((stbi__psd_is16(s)) != 0)
				return (int)(1);
			return (int)(0);
		}

		public static int stbi_info_from_memory(byte* buffer, int len, int* x, int* y, int* comp)
		{
			stbi__context s = new stbi__context();
			stbi__start_mem(s, buffer, (int)(len));
			return (int)(stbi__info_main(s, x, y, comp));
		}

		public static int stbi_info_from_callbacks(stbi_io_callbacks c, void* user, int* x, int* y, int* comp)
		{
			stbi__context s = new stbi__context();
			stbi__start_callbacks(s, c, user);
			return (int)(stbi__info_main(s, x, y, comp));
		}

		public static int stbi_is_16_bit_from_memory(byte* buffer, int len)
		{
			stbi__context s = new stbi__context();
			stbi__start_mem(s, buffer, (int)(len));
			return (int)(stbi__is_16_main(s));
		}

		public static int stbi_is_16_bit_from_callbacks(stbi_io_callbacks c, void* user)
		{
			stbi__context s = new stbi__context();
			stbi__start_callbacks(s, c, user);
			return (int)(stbi__is_16_main(s));
		}
	}
}
