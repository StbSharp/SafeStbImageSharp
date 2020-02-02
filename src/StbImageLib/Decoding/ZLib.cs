using StbImageLib.Utility;
using System;

namespace StbImageLib.Decoding
{
	public unsafe class ZLib
	{
		private class stbi__zhuffman
		{
			public ushort[] fast = new ushort[1 << 9];
			public ushort[] firstcode = new ushort[16];
			public int[] maxcode = new int[17];
			public ushort[] firstsymbol = new ushort[16];
			public byte[] size = new byte[288];
			public ushort[] value = new ushort[288];
		}

		private static readonly int[] stbi__zlength_base = { 3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31, 35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258, 0, 0 };
		private static readonly int[] stbi__zlength_extra = { 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0, 0, 0 };
		private static readonly int[] stbi__zdist_base = { 1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193, 257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145, 8193, 12289, 16385, 24577, 0, 0 };
		private static readonly int[] stbi__zdist_extra = { 0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13 };
		private static readonly byte[] stbi__zdefault_length = { 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8, 8, 8, 8 };
		private static readonly byte[] stbi__zdefault_distance = { 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5 };

		private byte* zbuffer;
		private byte* zbuffer_end;
		private int num_bits;
		private uint code_buffer;
		private sbyte* zout;
		private sbyte* zout_start;
		private sbyte* zout_end;
		private int z_expandable;
		private stbi__zhuffman z_length = new stbi__zhuffman();
		private stbi__zhuffman z_distance = new stbi__zhuffman();

		private byte stbi__zget8()
		{
			if (zbuffer >= zbuffer_end)
				return 0;
			return *zbuffer++;
		}

		private void stbi__fill_bits()
		{
			do
			{
				code_buffer |= (uint)stbi__zget8() << num_bits;
				num_bits += 8;
			}
			while (num_bits <= 24);
		}

		private uint stbi__zreceive(int n)
		{
			uint k = 0;
			if ((num_bits) < (n))
				stbi__fill_bits();
			k = (uint)(code_buffer & ((1 << n) - 1));
			code_buffer >>= n;
			num_bits -= (int)(n);
			return (uint)(k);
		}

		private int stbi__zhuffman_decode_slowpath(stbi__zhuffman z)
		{
			int b = 0;
			int s = 0;
			int k = 0;
			k = (int)(Memory.stbi__bit_reverse((int)(code_buffer), (int)(16)));
			for (s = (int)(9 + 1); ; ++s)
			{
				if ((k) < (z.maxcode[s]))
					break;
			}
			if ((s) == (16))
				return (int)(-1);
			b = (int)((k >> (16 - s)) - z.firstcode[s] + z.firstsymbol[s]);
			code_buffer >>= s;
			num_bits -= (int)(s);
			return (int)(z.value[b]);
		}

		private int stbi__zhuffman_decode(stbi__zhuffman z)
		{
			int b = 0;
			int s = 0;
			if ((num_bits) < (16))
				stbi__fill_bits();
			b = (int)(z.fast[code_buffer & ((1 << 9) - 1)]);
			if ((b) != 0)
			{
				s = (int)(b >> 9);
				code_buffer >>= s;
				num_bits -= (int)(s);
				return (int)(b & 511);
			}

			return (int)(stbi__zhuffman_decode_slowpath(z));
		}

		private int stbi__zexpand(sbyte* zout, int n)
		{
			sbyte* q;
			int cur = 0;
			int limit = 0;
			int old_limit = 0;
			this.zout = zout;
			if (z_expandable == 0)
				Decoder.stbi__err("output buffer limit");
			cur = ((int)(zout - zout_start));
			limit = (int)(old_limit = ((int)(zout_end - zout_start)));
			while ((cur + n) > (limit))
			{
				limit *= (int)(2);
			}
			q = (sbyte*)(CRuntime.realloc(zout_start, (ulong)(limit)));
			zout_start = q;
			zout = q + cur;
			zout_end = q + limit;
			return (int)(1);
		}

		private int stbi__parse_huffman_block()
		{
			sbyte* zout = this.zout;
			for (; ; )
			{
				int z = (int)(stbi__zhuffman_decode(z_length));
				if (z < (256))
				{
					if (z < (0))
						Decoder.stbi__err("bad huffman code");
					if ((zout) >= (zout_end))
					{
						if (stbi__zexpand(zout, (int)(1)) == 0)
							return (int)(0);
						zout = this.zout;
					}
					*zout++ = ((sbyte)(z));
				}
				else
				{
					byte* p;
					int len = 0;
					int dist = 0;
					if (z == 256)
					{
						zout = this.zout;
						return (int)(1);
					}
					z -= (int)(257);
					len = (int)(stbi__zlength_base[z]);
					if ((stbi__zlength_extra[z]) != 0)
						len += (int)(stbi__zreceive((int)(stbi__zlength_extra[z])));
					z = (int)(stbi__zhuffman_decode(z_distance));
					if (z < (0))
						Decoder.stbi__err("bad huffman code");
					dist = (int)(stbi__zdist_base[z]);
					if ((stbi__zdist_extra[z]) != 0)
						dist += (int)(stbi__zreceive((int)(stbi__zdist_extra[z])));
					if ((zout - zout_start) < (dist))
						Decoder.stbi__err("bad dist");
					if ((zout + len) > (zout_end))
					{
						if (stbi__zexpand(zout, (int)(len)) == 0)
							return (int)(0);
						zout = zout;
					}
					p = (byte*)(zout - dist);
					if ((dist) == (1))
					{
						byte v = (byte)(*p);
						if ((len) != 0)
						{
							do
								*zout++ = (sbyte)(v);
							while ((--len) != 0);
						}
					}
					else
					{
						if ((len) != 0)
						{
							do
								*zout++ = (sbyte)(*p++);
							while ((--len) != 0);
						}
					}
				}
			}
		}

		private static int stbi__zbuild_huffman(stbi__zhuffman z, byte* sizelist, int num)
		{
			int i = 0;
			int k = (int)(0);
			int code = 0;
			int* next_code = stackalloc int[16];
			int* sizes = stackalloc int[17];
			CRuntime.memset(sizes, (int)(0), (ulong)(sizeof(int)));
			Array.Clear(z.fast, 0, z.fast.Length);
			for (i = (int)(0); (i) < (num); ++i)
			{
				++sizes[sizelist[i]];
			}
			sizes[0] = (int)(0);
			for (i = (int)(1); (i) < (16); ++i)
			{
				if ((sizes[i]) > (1 << i))
					Decoder.stbi__err("bad sizes");
			}
			code = (int)(0);
			for (i = (int)(1); (i) < (16); ++i)
			{
				next_code[i] = (int)(code);
				z.firstcode[i] = ((ushort)(code));
				z.firstsymbol[i] = ((ushort)(k));
				code = (int)(code + sizes[i]);
				if ((sizes[i]) != 0)
					if ((code - 1) >= (1 << i))
						Decoder.stbi__err("bad codelengths");
				z.maxcode[i] = (int)(code << (16 - i));
				code <<= 1;
				k += (int)(sizes[i]);
			}
			z.maxcode[16] = (int)(0x10000);
			for (i = (int)(0); (i) < (num); ++i)
			{
				int s = (int)(sizelist[i]);
				if ((s) != 0)
				{
					int c = (int)(next_code[s] - z.firstcode[s] + z.firstsymbol[s]);
					ushort fastv = (ushort)((s << 9) | i);
					z.size[c] = ((byte)(s));
					z.value[c] = ((ushort)(i));
					if (s <= 9)
					{
						int j = (int)(Memory.stbi__bit_reverse((int)(next_code[s]), (int)(s)));
						while ((j) < (1 << 9))
						{
							z.fast[j] = (ushort)(fastv);
							j += (int)(1 << s);
						}
					}
					++next_code[s];
				}
			}
			return (int)(1);
		}

		private int stbi__compute_huffman_codes()
		{
			byte* length_dezigzag = stackalloc byte[19];
			length_dezigzag[0] = (byte)(16);
			length_dezigzag[1] = (byte)(17);
			length_dezigzag[2] = (byte)(18);
			length_dezigzag[3] = (byte)(0);
			length_dezigzag[4] = (byte)(8);
			length_dezigzag[5] = (byte)(7);
			length_dezigzag[6] = (byte)(9);
			length_dezigzag[7] = (byte)(6);
			length_dezigzag[8] = (byte)(10);
			length_dezigzag[9] = (byte)(5);
			length_dezigzag[10] = (byte)(11);
			length_dezigzag[11] = (byte)(4);
			length_dezigzag[12] = (byte)(12);
			length_dezigzag[13] = (byte)(3);
			length_dezigzag[14] = (byte)(13);
			length_dezigzag[15] = (byte)(2);
			length_dezigzag[16] = (byte)(14);
			length_dezigzag[17] = (byte)(1);
			length_dezigzag[18] = (byte)(15);

			stbi__zhuffman z_codelength = new stbi__zhuffman();
			byte* lencodes = stackalloc byte[286 + 32 + 137];
			byte* codelength_sizes = stackalloc byte[19];
			int i = 0;
			int n = 0;
			int hlit = (int)(stbi__zreceive((int)(5)) + 257);
			int hdist = (int)(stbi__zreceive((int)(5)) + 1);
			int hclen = (int)(stbi__zreceive((int)(4)) + 4);
			int ntot = (int)(hlit + hdist);
			CRuntime.memset(((byte*)(codelength_sizes)), (int)(0), (ulong)(19 * sizeof(byte)));
			for (i = (int)(0); (i) < (hclen); ++i)
			{
				int s = (int)(stbi__zreceive((int)(3)));
				codelength_sizes[length_dezigzag[i]] = ((byte)(s));
			}
			if (stbi__zbuild_huffman(z_codelength, codelength_sizes, (int)(19)) == 0)
				return (int)(0);
			n = (int)(0);
			while ((n) < (ntot))
			{
				int c = (int)(stbi__zhuffman_decode(z_codelength));
				if (((c) < (0)) || ((c) >= (19)))
					Decoder.stbi__err("bad codelengths");
				if ((c) < (16))
					lencodes[n++] = ((byte)(c));
				else
				{
					byte fill = (byte)(0);
					if ((c) == (16))
					{
						c = (int)(stbi__zreceive((int)(2)) + 3);
						if ((n) == (0))
							Decoder.stbi__err("bad codelengths");
						fill = (byte)(lencodes[n - 1]);
					}
					else if ((c) == (17))
						c = (int)(stbi__zreceive((int)(3)) + 3);
					else
					{
						c = (int)(stbi__zreceive((int)(7)) + 11);
					}
					if ((ntot - n) < (c))
						Decoder.stbi__err("bad codelengths");
					CRuntime.memset(lencodes + n, (int)(fill), (ulong)(c));
					n += (int)(c);
				}
			}
			if (n != ntot)
				Decoder.stbi__err("bad codelengths");
			if (stbi__zbuild_huffman(z_length, lencodes, (int)(hlit)) == 0)
				return (int)(0);
			if (stbi__zbuild_huffman(z_distance, lencodes + hlit, (int)(hdist)) == 0)
				return (int)(0);
			return (int)(1);
		}

		private int stbi__parse_uncompressed_block()
		{
			byte* header = stackalloc byte[4];
			int len = 0;
			int nlen = 0;
			int k = 0;
			if ((num_bits & 7) != 0)
				stbi__zreceive((int)(num_bits & 7));
			k = (int)(0);
			while ((num_bits) > (0))
			{
				header[k++] = ((byte)(code_buffer & 255));
				code_buffer >>= 8;
				num_bits -= (int)(8);
			}
			while ((k) < (4))
			{
				header[k++] = (byte)(stbi__zget8());
			}
			len = (int)(header[1] * 256 + header[0]);
			nlen = (int)(header[3] * 256 + header[2]);
			if (nlen != (len ^ 0xffff))
				Decoder.stbi__err("zlib corrupt");
			if ((zbuffer + len) > (zbuffer_end))
				Decoder.stbi__err("read past buffer");
			if ((zout + len) > (zout_end))
				if (stbi__zexpand(zout, (int)(len)) == 0)
					return (int)(0);
			CRuntime.memcpy(zout, zbuffer, (ulong)(len));
			zbuffer += len;
			zout += len;
			return (int)(1);
		}

		private int stbi__parse_zlib_header()
		{
			int cmf = (int)(stbi__zget8());
			int cm = (int)(cmf & 15);
			int flg = (int)(stbi__zget8());
			if ((cmf * 256 + flg) % 31 != 0)
				Decoder.stbi__err("bad zlib header");
			if ((flg & 32) != 0)
				Decoder.stbi__err("no preset dict");
			if (cm != 8)
				Decoder.stbi__err("bad compression");
			return (int)(1);
		}

		private int stbi__parse_zlib(int parse_header)
		{
			int final = 0;
			int type = 0;
			if ((parse_header) != 0)
				if (stbi__parse_zlib_header() == 0)
					return (int)(0);
			num_bits = (int)(0);
			code_buffer = (uint)(0);
			do
			{
				final = (int)(stbi__zreceive((int)(1)));
				type = (int)(stbi__zreceive((int)(2)));
				if ((type) == (0))
				{
					if (stbi__parse_uncompressed_block() == 0)
						return (int)(0);
				}
				else if ((type) == (3))
				{
					return (int)(0);
				}
				else
				{
					if ((type) == (1))
					{
						fixed (byte* b = stbi__zdefault_length)
						{
							if (stbi__zbuild_huffman(z_length, b, (int)(288)) == 0)
								return (int)(0);
						}
						fixed (byte* b = stbi__zdefault_distance)
						{
							if (stbi__zbuild_huffman(z_distance, b, (int)(32)) == 0)
								return (int)(0);
						}
					}
					else
					{
						if (stbi__compute_huffman_codes() == 0)
							return (int)(0);
					}
					if (stbi__parse_huffman_block() == 0)
						return (int)(0);
				}
			}
			while (final == 0);
			return (int)(1);
		}

		private int stbi__do_zlib(sbyte* obuf, int olen, int exp, int parse_header)
		{
			zout_start = obuf;
			zout = obuf;
			zout_end = obuf + olen;
			z_expandable = (int)(exp);
			return (int)(stbi__parse_zlib((int)(parse_header)));
		}

		private static int stbi_zlib_decode_buffer(sbyte* obuffer, int olen, sbyte* ibuffer, int ilen)
		{
			var a = new ZLib();
			a.zbuffer = (byte*)(ibuffer);
			a.zbuffer_end = (byte*)(ibuffer) + ilen;
			if ((a.stbi__do_zlib(obuffer, (int)(olen), (int)(0), (int)(1))) != 0)
				return (int)(a.zout - a.zout_start);
			else
				return (int)(-1);
		}

		public static sbyte* stbi_zlib_decode_noheader_malloc(sbyte* buffer, int len, int* outlen)
		{
			var a = new ZLib();
			sbyte* p = (sbyte*)(Memory.stbi__malloc((ulong)(16384)));
			a.zbuffer = (byte*)(buffer);
			a.zbuffer_end = (byte*)(buffer) + len;
			if ((a.stbi__do_zlib(p, (int)(16384), (int)(1), (int)(0))) != 0)
			{
				if ((outlen) != null)
					*outlen = ((int)(a.zout - a.zout_start));
				return a.zout_start;
			}
			else
			{
				CRuntime.free(a.zout_start);
				return (null);
			}

		}

		public static int stbi_zlib_decode_noheader_buffer(sbyte* obuffer, int olen, sbyte* ibuffer, int ilen)
		{
			var a = new ZLib();
			a.zbuffer = (byte*)(ibuffer);
			a.zbuffer_end = (byte*)(ibuffer) + ilen;
			if ((a.stbi__do_zlib(obuffer, (int)(olen), (int)(0), (int)(0))) != 0)
				return (int)(a.zout - a.zout_start);
			else
				return (int)(-1);
		}

		public static sbyte* stbi_zlib_decode_malloc_guesssize(sbyte* buffer, int len, int initial_size, int* outlen)
		{
			var a = new ZLib();
			sbyte* p = (sbyte*)(Memory.stbi__malloc((ulong)(initial_size)));
			if ((p) == (null))
				return (null);
			a.zbuffer = (byte*)(buffer);
			a.zbuffer_end = (byte*)(buffer) + len;
			if ((a.stbi__do_zlib(p, (int)(initial_size), (int)(1), (int)(1))) != 0)
			{
				if ((outlen) != null)
					*outlen = ((int)(a.zout - a.zout_start));
				return a.zout_start;
			}
			else
			{
				CRuntime.free(a.zout_start);
				return (null);
			}
		}

		public static sbyte* stbi_zlib_decode_malloc(sbyte* buffer, int len, int* outlen)
		{
			return stbi_zlib_decode_malloc_guesssize(buffer, (int)(len), (int)(16384), outlen);
		}

		public static sbyte* stbi_zlib_decode_malloc_guesssize_headerflag(sbyte* buffer, int len, int initial_size, int* outlen, int parse_header)
		{
			var a = new ZLib();
			sbyte* p = (sbyte*)(Memory.stbi__malloc((ulong)(initial_size)));
			if ((p) == (null))
				return (null);
			a.zbuffer = (byte*)(buffer);
			a.zbuffer_end = (byte*)(buffer) + len;
			if ((a.stbi__do_zlib(p, (int)(initial_size), (int)(1), (int)(parse_header))) != 0)
			{
				if ((outlen) != null)
					*outlen = ((int)(a.zout - a.zout_start));
				return a.zout_start;
			}
			else
			{
				CRuntime.free(a.zout_start);
				return (null);
			}
		}
	}
}