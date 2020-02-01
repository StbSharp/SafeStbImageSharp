using System;
using System.Runtime.InteropServices;

namespace StbImageLib
{
#if !STBSHARP_INTERNAL
	public
#else
	internal
#endif
	static unsafe partial class StbImage
	{
		public static string LastError;

		public delegate int ReadCallback(void* user, sbyte* data, int size);

		public delegate int SkipCallback(void* user, int n);

		public delegate int EofCallback(void* user);

		[StructLayout(LayoutKind.Sequential)]
		public struct stbi__gif_lzw
		{
			public short prefix;
			public byte first;
			public byte suffix;
		}

		public class stbi__gif: IDisposable
		{
			public int w;
			public int h;
			public byte* _out_;
			public byte* background;
			public byte* history;
			public int flags;
			public int bgindex;
			public int ratio;
			public int transparent;
			public int eflags;
			public int delay;
			public byte* pal;
			public byte* lpal;
			public stbi__gif_lzw* codes = (stbi__gif_lzw*)stbi__malloc(8192 * sizeof(stbi__gif_lzw));
			public byte* color_table;
			public int parse;
			public int step;
			public int lflags;
			public int start_x;
			public int start_y;
			public int max_x;
			public int max_y;
			public int cur_x;
			public int cur_y;
			public int line_size;

			public stbi__gif()
			{
				pal = (byte*) stbi__malloc(256 * 4 * sizeof(byte));
				lpal = (byte*) stbi__malloc(256 * 4 * sizeof(byte));
			}

			~stbi__gif()
			{
				Dispose();
			}

			public void Dispose()
			{
				if (pal != null)
				{
					CRuntime.free(pal);
					pal = null;
				}

				if (lpal != null)
				{
					CRuntime.free(lpal);
					lpal = null;
				}

				if (codes != null)
				{
					CRuntime.free(codes);
					codes = null;
				}
			}
		}

		private static int stbi__err(string str)
		{
			LastError = str;
			return 0;
		}

		public static void stbi__gif_parse_colortable(stbi__context s, byte* pal, int num_entries, int transp)
		{
			int i;
			for (i = 0; (i) < (num_entries); ++i)
			{
				pal[i * 4 + 2] = stbi__get8(s);
				pal[i * 4 + 1] = stbi__get8(s);
				pal[i * 4] = stbi__get8(s);
				pal[i * 4 + 3] = (byte) (transp == i ? 0 : 255);
			}
		}
	}
}