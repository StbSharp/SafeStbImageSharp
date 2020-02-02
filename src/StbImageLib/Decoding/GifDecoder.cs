using StbImageLib.Utility;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace StbImageLib.Decoding
{
	public unsafe class GifDecoder: Decoder, IDisposable
	{
		[StructLayout(LayoutKind.Sequential)]
		private struct stbi__gif_lzw
		{
			public short prefix;
			public byte first;
			public byte suffix;
		}

		private int w;
		private int h;
		private byte[] _out_;
		private byte* background;
		private byte* history;
		private int flags;
		private int bgindex;
		private int ratio;
		private int transparent;
		private int eflags;
		private int delay;
		private byte* pal;
		private byte* lpal;
		private stbi__gif_lzw* codes = (stbi__gif_lzw*)Memory.stbi__malloc(8192 * sizeof(stbi__gif_lzw));
		private byte* color_table;
		private int parse;
		private int step;
		private int lflags;
		private int start_x;
		private int start_y;
		private int max_x;
		private int max_y;
		private int cur_x;
		private int cur_y;
		private int line_size;

		private GifDecoder(Stream stream): base(stream)
		{
			pal = (byte*)Memory.stbi__malloc(256 * 4 * sizeof(byte));
			lpal = (byte*)Memory.stbi__malloc(256 * 4 * sizeof(byte));
		}

		~GifDecoder()
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

		private void stbi__gif_parse_colortable(byte* pal, int num_entries, int transp)
		{
			int i;
			for (i = 0; (i) < (num_entries); ++i)
			{
				pal[i * 4 + 2] = stbi__get8();
				pal[i * 4 + 1] = stbi__get8();
				pal[i * 4] = stbi__get8();
				pal[i * 4 + 3] = (byte)(transp == i ? 0 : 255);
			}
		}

		private int stbi__gif_header(int* comp, int is_info)
		{
			byte version = 0;
			if ((((stbi__get8() != 'G') || (stbi__get8() != 'I')) || (stbi__get8() != 'F')) || (stbi__get8() != '8'))
				stbi__err("not GIF");
			version = (byte)(stbi__get8());
			if ((version != '7') && (version != '9'))
				stbi__err("not GIF");
			if (stbi__get8() != 'a')
				stbi__err("not GIF");
			w = (int)(stbi__get16le());
			h = (int)(stbi__get16le());
			flags = (int)(stbi__get8());
			bgindex = (int)(stbi__get8());
			ratio = (int)(stbi__get8());
			transparent = (int)(-1);
			if (comp != null)
				*comp = (int)(4);
			if ((is_info) != 0)
				return (int)(1);
			if ((flags & 0x80) != 0)
				stbi__gif_parse_colortable(pal, (int)(2 << (flags & 7)), (int)(-1));
			return (int)(1);
		}

		private void stbi__out_gif_code(ushort code)
		{
			byte* c;
			int idx = 0;
			if ((codes[code].prefix) >= (0))
				stbi__out_gif_code((ushort)(codes[code].prefix));
			if ((cur_y) >= (max_y))
				return;
			idx = (int)(cur_x + cur_y);
			history[idx / 4] = (byte)(1);
			c = &color_table[codes[code].suffix * 4];
			if ((c[3]) > (128))
			{
				fixed (byte* p = &_out_[idx])
				{
					p[0] = (byte)(c[2]);
					p[1] = (byte)(c[1]);
					p[2] = (byte)(c[0]);
					p[3] = (byte)(c[3]);
				}
			}

			cur_x += (int)(4);
			if ((cur_x) >= (max_x))
			{
				cur_x = (int)(start_x);
				cur_y += (int)(step);
				while (((cur_y) >= (max_y)) && ((parse) > (0)))
				{
					step = (int)((1 << parse) * line_size);
					cur_y = (int)(start_y + (step >> 1));
					--parse;
				}
			}
		}

		private byte[] stbi__process_gif_raster()
		{
			byte lzw_cs = 0;
			int len = 0;
			int init_code = 0;
			uint first = 0;
			int codesize = 0;
			int codemask = 0;
			int avail = 0;
			int oldcode = 0;
			int bits = 0;
			int valid_bits = 0;
			int clear = 0;
			stbi__gif_lzw* p;
			lzw_cs = (byte)(stbi__get8());
			if ((lzw_cs) > (12))
				return (null);
			clear = (int)(1 << lzw_cs);
			first = (uint)(1);
			codesize = (int)(lzw_cs + 1);
			codemask = (int)((1 << codesize) - 1);
			bits = (int)(0);
			valid_bits = (int)(0);
			for (init_code = (int)(0); (init_code) < (clear); init_code++)
			{
				((stbi__gif_lzw*)(codes))[init_code].prefix = (short)(-1);
				((stbi__gif_lzw*)(codes))[init_code].first = ((byte)(init_code));
				((stbi__gif_lzw*)(codes))[init_code].suffix = ((byte)(init_code));
			}
			avail = (int)(clear + 2);
			oldcode = (int)(-1);
			len = (int)(0);
			for (; ; )
			{
				if ((valid_bits) < (codesize))
				{
					if ((len) == (0))
					{
						len = (int)(stbi__get8());
						if ((len) == (0))
							return _out_;
					}
					--len;
					bits |= (int)((int)(stbi__get8()) << valid_bits);
					valid_bits += (int)(8);
				}
				else
				{
					int code = (int)(bits & codemask);
					bits >>= codesize;
					valid_bits -= (int)(codesize);
					if ((code) == (clear))
					{
						codesize = (int)(lzw_cs + 1);
						codemask = (int)((1 << codesize) - 1);
						avail = (int)(clear + 2);
						oldcode = (int)(-1);
						first = (uint)(0);
					}
					else if ((code) == (clear + 1))
					{
						stbi__skip((int)(len));
						while ((len = (int)(stbi__get8())) > (0))
						{
							stbi__skip((int)(len));
						}
						return _out_;
					}
					else if (code <= avail)
					{
						if ((first) != 0)
						{
							stbi__err("no clear code");
						}
						if ((oldcode) >= (0))
						{
							p = (stbi__gif_lzw*)codes + avail++;
							if ((avail) > (8192))
							{
								stbi__err("too many codes");
							}
							p->prefix = ((short)(oldcode));
							p->first = (byte)(codes[oldcode].first);
							p->suffix = (byte)(((code) == (avail)) ? p->first : codes[code].first);
						}
						else if ((code) == (avail))
							stbi__err("illegal code in raster");
						stbi__out_gif_code((ushort)(code));
						if (((avail & codemask) == (0)) && (avail <= 0x0FFF))
						{
							codesize++;
							codemask = (int)((1 << codesize) - 1);
						}
						oldcode = (int)(code);
					}
					else
					{
						stbi__err("illegal code in raster");
					}
				}
			}
		}

		private byte[] stbi__gif_load_next(int* comp, byte* two_back)
		{
			int dispose = 0;
			int first_frame = 0;
			int pi = 0;
			int pcount = 0;
			first_frame = (int)(0);
			if ((_out_) == (null))
			{
				if (stbi__gif_header(comp, (int)(0)) == 0)
					return null;
				if (Memory.stbi__mad3sizes_valid((int)(4), (int)(w), (int)(h), (int)(0)) == 0)
					stbi__err("too large");
				pcount = (int)(w * h);
				_out_ = new byte[4 * pcount];
				background = (byte*)(Memory.stbi__malloc((ulong)(4 * pcount)));
				history = (byte*)(Memory.stbi__malloc((ulong)(pcount)));
				Array.Clear(_out_, 0, _out_.Length);
				CRuntime.memset(background, (int)(0x00), (ulong)(4 * pcount));
				CRuntime.memset(history, (int)(0x00), (ulong)(pcount));
				first_frame = (int)(1);
			}
			else
			{
				fixed (byte* ptr = &_out_[0])
				{
					dispose = (int)((eflags & 0x1C) >> 2);
					pcount = (int)(w * h);
					if (((dispose) == (3)) && ((two_back) == (null)))
					{
						dispose = (int)(2);
					}
					if ((dispose) == (3))
					{
						for (pi = (int)(0); (pi) < (pcount); ++pi)
						{
							if ((history[pi]) != 0)
							{
								CRuntime.memcpy(&ptr[pi * 4], &two_back[pi * 4], (ulong)(4));
							}
						}
					}
					else if ((dispose) == (2))
					{
						for (pi = (int)(0); (pi) < (pcount); ++pi)
						{
							if ((history[pi]) != 0)
							{
								CRuntime.memcpy(&ptr[pi * 4], &background[pi * 4], (ulong)(4));
							}
						}
					}
					else
					{
					}
					CRuntime.memcpy(background, ptr, (ulong)(4 * w * h));
				}
			}

			CRuntime.memset(history, (int)(0x00), (ulong)(w * h));
			for (; ; )
			{
				int tag = (int)(stbi__get8());
				switch (tag)
				{
					case 0x2C:
					{
						int x = 0;
						int y = 0;
						int w = 0;
						int h = 0;
						byte[] o;
						x = (int)(stbi__get16le());
						y = (int)(stbi__get16le());
						w = (int)(stbi__get16le());
						h = (int)(stbi__get16le());
						if (((x + w) > (w)) || ((y + h) > (h)))
							stbi__err("bad Image Descriptor");
						line_size = (int)(w * 4);
						start_x = (int)(x * 4);
						start_y = (int)(y * line_size);
						max_x = (int)(start_x + w * 4);
						max_y = (int)(start_y + h * line_size);
						cur_x = (int)(start_x);
						cur_y = (int)(start_y);
						if ((w) == (0))
							cur_y = (int)(max_y);
						lflags = (int)(stbi__get8());
						if ((lflags & 0x40) != 0)
						{
							step = (int)(8 * line_size);
							parse = (int)(3);
						}
						else
						{
							step = (int)(line_size);
							parse = (int)(0);
						}
						if ((lflags & 0x80) != 0)
						{
							stbi__gif_parse_colortable(lpal, (int)(2 << (lflags & 7)), (int)((eflags & 0x01) != 0 ? transparent : -1));
							color_table = (byte*)(lpal);
						}
						else if ((flags & 0x80) != 0)
						{
							color_table = (byte*)(pal);
						}
						else
							stbi__err("missing color table");
						o = stbi__process_gif_raster();
						if (o == null)
							return (null);
						pcount = (int)(w * h);
						if (((first_frame) != 0) && ((bgindex) > (0)))
						{
							fixed (byte* ptr = &_out_[0])
							{
								for (pi = (int)(0); (pi) < (pcount); ++pi)
								{
									if ((history[pi]) == (0))
									{
										pal[bgindex * 4 + 3] = (byte)(255);
										CRuntime.memcpy(&ptr[pi * 4], &pal[bgindex], (ulong)(4));
									}
								}
							}
						}
						return o;
					}
					case 0x21:
					{
						int len = 0;
						int ext = (int)(stbi__get8());
						if ((ext) == (0xF9))
						{
							len = (int)(stbi__get8());
							if ((len) == (4))
							{
								eflags = (int)(stbi__get8());
								delay = (int)(10 * stbi__get16le());
								if ((transparent) >= (0))
								{
									pal[transparent * 4 + 3] = (byte)(255);
								}
								if ((eflags & 0x01) != 0)
								{
									transparent = (int)(stbi__get8());
									if ((transparent) >= (0))
									{
										pal[transparent * 4 + 3] = (byte)(0);
									}
								}
								else
								{
									stbi__skip((int)(1));
									transparent = (int)(-1);
								}
							}
							else
							{
								stbi__skip((int)(len));
								break;
							}
						}
						while ((len = (int)(stbi__get8())) != 0)
						{
							stbi__skip((int)(len));
						}
						break;
					}
					case 0x3B:
						return null;
					default:
						stbi__err("unknown code");
						break;
				}
			}
		}

		/*		private void* stbi__load_gif_main(int** delays, int* x, int* y, int* z, int* comp, int req_comp)
				{
					if ((IsGif(Stream)))
					{
						int layers = (int)(0);
						byte* u = null;
						byte* _out_ = null;
						byte* two_back = null;
						int stride = 0;
						if ((delays) != null)
						{
							*delays = null;
						}
						do
						{
							u = stbi__gif_load_next(comp, (int)(req_comp), two_back);
							if ((u) != null)
							{
								*x = (int)(w);
								*y = (int)(h);
								++layers;
								stride = (int)(w * h * 4);
								if ((_out_) != null)
								{
									_out_ = (byte*)(CRuntime.realloc(_out_, (ulong)(layers * stride)));
									if ((delays) != null)
									{
										*delays = (int*)(CRuntime.realloc(*delays, (ulong)(sizeof(int) * layers)));
									}
								}
								else
								{
									_out_ = (byte*)(Utility.stbi__malloc((ulong)(layers * stride)));
									if ((delays) != null)
									{
										*delays = (int*)(Utility.stbi__malloc((ulong)(layers * sizeof(int))));
									}
								}
								CRuntime.memcpy(_out_ + ((layers - 1) * stride), u, (ulong)(stride));
								if ((layers) >= (2))
								{
									two_back = _out_ - 2 * stride;
								}
								if ((delays) != null)
								{
									(*delays)[layers - 1U] = (int)(delay);
								}
							}
						}
						while (u != null);
						CRuntime.free(_out_);
						CRuntime.free(history);
						CRuntime.free(background);
						if (((req_comp) != 0) && (req_comp != 4))
							_out_ = stbi__convert_format(_out_, (int)(4), (int)(req_comp), (uint)(layers * w), (uint)(h));
						*z = (int)(layers);
						return _out_;
					}
					else
					{
						stbi__err("not GIF");
					}

				}*/

		private ImageResult InternalDecode(ColorComponents? requiredComponents)
		{
			try
			{
				int comp;
				var u = stbi__gif_load_next(&comp, null);
				if (u == null)
				{
					throw new Exception("could not decode gif");
				}

				if (requiredComponents != null && requiredComponents.Value != ColorComponents.RedGreenBlueAlpha)
					u = Conversion.stbi__convert_format(u, (int)(4), (int)(requiredComponents.Value), (uint)(w), (uint)(h));

				return new ImageResult
				{
					Width = w,
					Height = h,
					ColorComponents = requiredComponents != null ? requiredComponents.Value : (ColorComponents)comp,
					BitsPerChannel = 8,
					Data = u
				};
			}
			finally
			{
				CRuntime.free(history);
				CRuntime.free(background);
			}
		}

		private static bool InternalTest(Stream stream)
		{
			int sz = 0;
			if ((((stream.stbi__get8() != 'G') || (stream.stbi__get8() != 'I')) || (stream.stbi__get8() != 'F')) || (stream.stbi__get8() != '8'))
				return false;
			sz = (int)(stream.stbi__get8());
			if ((sz != '9') && (sz != '7'))
				return false;
			if (stream.stbi__get8() != 'a')
				return false;
			return true;
		}

		public static bool Test(Stream stream)
		{
			var result = InternalTest(stream);
			stream.Rewind();
			return result;
		}

		public static ImageInfo? Info(Stream stream)
		{
			var decoder = new GifDecoder(stream);

			int comp;
			var r = decoder.stbi__gif_header(&comp, 1);
			stream.Rewind();
			if (r == 0)
			{
				return null;
			}

			return new ImageInfo
			{
				Width = decoder.w,
				Height = decoder.h,
				ColorComponents = (ColorComponents)comp,
				BitsPerChannel = 8
			};
		}

		public static ImageResult Decode(Stream stream, ColorComponents? requiredComponents = null)
		{
			var decoder = new GifDecoder(stream);
			return decoder.InternalDecode(requiredComponents);
		}
	}
}
