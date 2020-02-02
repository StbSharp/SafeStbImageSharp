using StbImageLib.Utility;
using System.IO;
using System.Runtime.InteropServices;

namespace StbImageLib.Decoding
{
	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct img_comp
	{
		public int id;
		public int h, v;
		public int tq;
		public int hd, ha;
		public int dc_pred;

		public int x, y, w2, h2;
		public byte* data;
		public void* raw_data;
		public void* raw_coeff;
		public byte* linebuf;
		public short* coeff; // progressive only
		public int coeff_w, coeff_h; // number of 8x8 coefficient blocks
	}

	internal unsafe delegate void idct_block_kernel(byte* output, int out_stride, short* data);

	internal unsafe delegate void YCbCr_to_RGB_kernel(byte* output, byte* y, byte* pcb, byte* pcr, int count, int step);

	internal unsafe delegate byte* Resampler(byte* a, byte* b, byte* c, int d, int e);

	internal unsafe class stbi__resample
	{
		public Resampler resample;
		public byte* line0;
		public byte* line1;
		public int hs;
		public int vs;
		public int w_lores;
		public int ystep;
		public int ypos;
	}

	public unsafe class JpgDecoder: Decoder
	{
		private class stbi__huffman
		{
			public byte[] fast = new byte[1 << 9];
			public ushort[] code = new ushort[256];
			public byte[] values = new byte[256];
			public byte[] size = new byte[257];
			public uint[] maxcode = new uint[18];
			public int[] delta = new int[17];
		}

		public const int STBI__ZFAST_BITS = 9;

		private static readonly uint[] stbi__bmask = { 0, 1, 3, 7, 15, 31, 63, 127, 255, 511, 1023, 2047, 4095, 8191, 16383, 32767, 65535 };
		private static readonly int[] stbi__jbias = { 0, -1, -3, -7, -15, -31, -63, -127, -255, -511, -1023, -2047, -4095, -8191, -16383, -32767 };
		private static readonly byte[] stbi__jpeg_dezigzag = { 0, 1, 8, 16, 9, 2, 3, 10, 17, 24, 32, 25, 18, 11, 4, 5, 12, 19, 26, 33, 40, 48, 41, 34, 27, 20, 13, 6, 7, 14, 21, 28, 35, 42, 49, 56, 57, 50, 43, 36, 29, 22, 15, 23, 30, 37, 44, 51, 58, 59, 52, 45, 38, 31, 39, 46, 53, 60, 61, 54, 47, 55, 62, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63 };

		private readonly stbi__huffman[] huff_dc = new stbi__huffman[4];
		private readonly stbi__huffman[] huff_ac = new stbi__huffman[4];
		private readonly ushort[][] dequant;

		private readonly short[][] fast_ac;

		// sizes for components, interleaved MCUs
		private int img_h_max, img_v_max;
		private int img_mcu_x, img_mcu_y;
		private int img_mcu_w, img_mcu_h;

		// definition of jpeg image component
		private img_comp[] img_comp = new img_comp[4];

		private uint code_buffer; // jpeg entropy-coded buffer
		private int code_bits; // number of valid bits
		private byte marker; // marker seen while filling entropy buffer
		private int nomore; // flag if we saw a marker so must stop

		private int progressive;
		private int spec_start;
		private int spec_end;
		private int succ_high;
		private int succ_low;
		private int eob_run;
		private int jfif;
		private int app14_color_transform; // Adobe APP14 tag
		private int rgb;

		private int scan_n;
		private int[] order = new int[4];
		private int restart_interval, todo;

		// kernels
		private idct_block_kernel idct_block_kernel;
		private YCbCr_to_RGB_kernel YCbCr_to_RGB_kernel;
		private Resampler resample_row_hv_2_kernel;

		private JpgDecoder(Stream stream): base(stream)
		{
			for (var i = 0; i < 4; ++i)
			{
				huff_ac[i] = new stbi__huffman();
				huff_dc[i] = new stbi__huffman();
			}

			for (var i = 0; i < img_comp.Length; ++i)
			{
				img_comp[i] = new img_comp();
			}

			fast_ac = new short[4][];
			for (var i = 0; i < fast_ac.Length; ++i)
			{
				fast_ac[i] = new short[1 << STBI__ZFAST_BITS];
			}

			dequant = new ushort[4][];
			for (var i = 0; i < dequant.Length; ++i)
			{
				dequant[i] = new ushort[64];
			}
		}

		private static int stbi__build_huffman(stbi__huffman h, int* count)
		{
			int i = 0;
			int j = 0;
			int k = (int)(0);
			uint code = 0;
			for (i = (int)(0); (i) < (16); ++i)
			{
				for (j = (int)(0); (j) < (count[i]); ++j)
				{
					h.size[k++] = ((byte)(i + 1));
				}
			}
			h.size[k] = (byte)(0);
			code = (uint)(0);
			k = (int)(0);
			for (j = (int)(1); j <= 16; ++j)
			{
				h.delta[j] = (int)(k - code);
				if ((h.size[k]) == (j))
				{
					while ((h.size[k]) == (j))
					{
						h.code[k++] = ((ushort)(code++));
					}
					if ((code - 1) >= (1u << j))
						stbi__err("bad code lengths");
				}
				h.maxcode[j] = (uint)(code << (16 - j));
				code <<= 1;
			}
			h.maxcode[j] = (uint)(0xffffffff);
			CRuntime.SetArray(h.fast, (byte)255);
			for (i = (int)(0); (i) < (k); ++i)
			{
				int s = (int)(h.size[i]);
				if (s <= 9)
				{
					int c = (int)(h.code[i] << (9 - s));
					int m = (int)(1 << (9 - s));
					for (j = (int)(0); (j) < (m); ++j)
					{
						h.fast[c + j] = ((byte)(i));
					}
				}
			}
			return (int)(1);
		}

		private static void stbi__build_fast_ac(short[] fast_ac, stbi__huffman h)
		{
			int i = 0;
			for (i = (int)(0); (i) < (1 << 9); ++i)
			{
				byte fast = (byte)(h.fast[i]);
				fast_ac[i] = (short)(0);
				if ((fast) < (255))
				{
					int rs = (int)(h.values[fast]);
					int run = (int)((rs >> 4) & 15);
					int magbits = (int)(rs & 15);
					int len = (int)(h.size[fast]);
					if (((magbits) != 0) && (len + magbits <= 9))
					{
						int k = (int)(((i << len) & ((1 << 9) - 1)) >> (9 - magbits));
						int m = (int)(1 << (magbits - 1));
						if ((k) < (m))
							k += (int)((~0U << magbits) + 1);
						if (((k) >= (-128)) && (k <= 127))
							fast_ac[i] = ((short)((k * 256) + (run * 16) + (len + magbits)));
					}
				}
			}
		}

		private void stbi__grow_buffer_unsafe()
		{
			do
			{
				uint b = (uint)((nomore) != 0 ? 0 : stbi__get8());
				if ((b) == (0xff))
				{
					int c = (int)(stbi__get8());
					while ((c) == (0xff))
					{
						c = (int)(stbi__get8());
					}
					if (c != 0)
					{
						marker = ((byte)(c));
						nomore = (int)(1);
						return;
					}
				}
				code_buffer |= (uint)(b << (24 - code_bits));
				code_bits += (int)(8);
			}
			while (code_bits <= 24);
		}

		private int stbi__jpeg_huff_decode(stbi__huffman h)
		{
			uint temp = 0;
			int c = 0;
			int k = 0;
			if ((code_bits) < (16))
				stbi__grow_buffer_unsafe();
			c = (int)((code_buffer >> (32 - 9)) & ((1 << 9) - 1));
			k = (int)(h.fast[c]);
			if ((k) < (255))
			{
				int s = (int)(h.size[k]);
				if ((s) > (code_bits))
					return (int)(-1);
				code_buffer <<= s;
				code_bits -= (int)(s);
				return (int)(h.values[k]);
			}

			temp = (uint)(code_buffer >> 16);
			for (k = (int)(9 + 1); ; ++k)
			{
				if ((temp) < (h.maxcode[k]))
					break;
			}
			if ((k) == (17))
			{
				code_bits -= (int)(16);
				return (int)(-1);
			}

			if ((k) > (code_bits))
				return (int)(-1);
			c = (int)(((code_buffer >> (32 - k)) & stbi__bmask[k]) + h.delta[k]);
			code_bits -= (int)(k);
			code_buffer <<= k;
			return (int)(h.values[c]);
		}

		private int stbi__extend_receive(int n)
		{
			uint k = 0;
			int sgn = 0;
			if ((code_bits) < (n))
				stbi__grow_buffer_unsafe();
			sgn = (int)((int)code_buffer >> 31);
			k = (uint)(CRuntime._lrotl(code_buffer, (int)(n)));
			code_buffer = (uint)(k & ~stbi__bmask[n]);
			k &= (uint)(stbi__bmask[n]);
			code_bits -= (int)(n);
			return (int)(k + (stbi__jbias[n] & ~sgn));
		}

		private int stbi__jpeg_get_bits(int n)
		{
			uint k = 0;
			if ((code_bits) < (n))
				stbi__grow_buffer_unsafe();
			k = (uint)(CRuntime._lrotl(code_buffer, (int)(n)));
			code_buffer = (uint)(k & ~stbi__bmask[n]);
			k &= (uint)(stbi__bmask[n]);
			code_bits -= (int)(n);
			return (int)(k);
		}

		private int stbi__jpeg_get_bit()
		{
			uint k = 0;
			if ((code_bits) < (1))
				stbi__grow_buffer_unsafe();
			k = (uint)(code_buffer);
			code_buffer <<= 1;
			--code_bits;
			return (int)(k & 0x80000000);
		}

		private int stbi__jpeg_decode_block(short* data, stbi__huffman hdc, stbi__huffman hac, short[] fac, int b, ushort[] dequant)
		{
			int diff = 0;
			int dc = 0;
			int k = 0;
			int t = 0;
			if ((code_bits) < (16))
				stbi__grow_buffer_unsafe();
			t = (int)(stbi__jpeg_huff_decode(hdc));
			if ((t) < (0))
				stbi__err("bad huffman code");
			CRuntime.memset(data, (int)(0), (ulong)(64 * sizeof(short)));
			diff = (int)((t) != 0 ? stbi__extend_receive((int)(t)) : 0);
			dc = (int)(img_comp[b].dc_pred + diff);
			img_comp[b].dc_pred = (int)(dc);
			data[0] = ((short)(dc * dequant[0]));
			k = (int)(1);
			do
			{
				uint zig = 0;
				int c = 0;
				int r = 0;
				int s = 0;
				if ((code_bits) < (16))
					stbi__grow_buffer_unsafe();
				c = (int)((code_buffer >> (32 - 9)) & ((1 << 9) - 1));
				r = (int)(fac[c]);
				if ((r) != 0)
				{
					k += (int)((r >> 4) & 15);
					s = (int)(r & 15);
					code_buffer <<= s;
					code_bits -= (int)(s);
					zig = (uint)(stbi__jpeg_dezigzag[k++]);
					data[zig] = ((short)((r >> 8) * dequant[zig]));
				}
				else
				{
					int rs = (int)(stbi__jpeg_huff_decode(hac));
					if ((rs) < (0))
						stbi__err("bad huffman code");
					s = (int)(rs & 15);
					r = (int)(rs >> 4);
					if ((s) == (0))
					{
						if (rs != 0xf0)
							break;
						k += (int)(16);
					}
					else
					{
						k += (int)(r);
						zig = (uint)(stbi__jpeg_dezigzag[k++]);
						data[zig] = ((short)(stbi__extend_receive((int)(s)) * dequant[zig]));
					}
				}
			}
			while ((k) < (64));
			return (int)(1);
		}

		private int stbi__jpeg_decode_block_prog_dc(short* data, stbi__huffman hdc, int b)
		{
			int diff = 0;
			int dc = 0;
			int t = 0;
			if (spec_end != 0)
				stbi__err("can't merge dc and ac");
			if ((code_bits) < (16))
				stbi__grow_buffer_unsafe();
			if ((succ_high) == (0))
			{
				CRuntime.memset(data, (int)(0), (ulong)(64 * sizeof(short)));
				t = (int)(stbi__jpeg_huff_decode(hdc));
				diff = (int)((t) != 0 ? stbi__extend_receive((int)(t)) : 0);
				dc = (int)(img_comp[b].dc_pred + diff);
				img_comp[b].dc_pred = (int)(dc);
				data[0] = ((short)(dc << succ_low));
			}
			else
			{
				if ((stbi__jpeg_get_bit()) != 0)
					data[0] += ((short)(1 << succ_low));
			}

			return (int)(1);
		}

		private int stbi__jpeg_decode_block_prog_ac(short* data, stbi__huffman hac, short[] fac)
		{
			int k = 0;
			if ((spec_start) == (0))
				stbi__err("can't merge dc and ac");
			if ((succ_high) == (0))
			{
				int shift = (int)(succ_low);
				if ((eob_run) != 0)
				{
					--eob_run;
					return (int)(1);
				}
				k = (int)(spec_start);
				do
				{
					uint zig = 0;
					int c = 0;
					int r = 0;
					int s = 0;
					if ((code_bits) < (16))
						stbi__grow_buffer_unsafe();
					c = (int)((code_buffer >> (32 - 9)) & ((1 << 9) - 1));
					r = (int)(fac[c]);
					if ((r) != 0)
					{
						k += (int)((r >> 4) & 15);
						s = (int)(r & 15);
						code_buffer <<= s;
						code_bits -= (int)(s);
						zig = (uint)(stbi__jpeg_dezigzag[k++]);
						data[zig] = ((short)((r >> 8) << shift));
					}
					else
					{
						int rs = (int)(stbi__jpeg_huff_decode(hac));
						if ((rs) < (0))
							stbi__err("bad huffman code");
						s = (int)(rs & 15);
						r = (int)(rs >> 4);
						if ((s) == (0))
						{
							if ((r) < (15))
							{
								eob_run = (int)(1 << r);
								if ((r) != 0)
									eob_run += (int)(stbi__jpeg_get_bits((int)(r)));
								--eob_run;
								break;
							}
							k += (int)(16);
						}
						else
						{
							k += (int)(r);
							zig = (uint)(stbi__jpeg_dezigzag[k++]);
							data[zig] = ((short)(stbi__extend_receive((int)(s)) << shift));
						}
					}
				}
				while (k <= spec_end);
			}
			else
			{
				short bit = (short)(1 << succ_low);
				if ((eob_run) != 0)
				{
					--eob_run;
					for (k = (int)(spec_start); k <= spec_end; ++k)
					{
						short* p = &data[stbi__jpeg_dezigzag[k]];
						if (*p != 0)
							if ((stbi__jpeg_get_bit()) != 0)
								if ((*p & bit) == (0))
								{
									if ((*p) > (0))
										*p += (short)(bit);
									else
										*p -= (short)(bit);
								}
					}
				}
				else
				{
					k = (int)(spec_start);
					do
					{
						int r = 0;
						int s = 0;
						int rs = (int)(stbi__jpeg_huff_decode(hac));
						if ((rs) < (0))
							stbi__err("bad huffman code");
						s = (int)(rs & 15);
						r = (int)(rs >> 4);
						if ((s) == (0))
						{
							if ((r) < (15))
							{
								eob_run = (int)((1 << r) - 1);
								if ((r) != 0)
									eob_run += (int)(stbi__jpeg_get_bits((int)(r)));
								r = (int)(64);
							}
							else
							{
							}
						}
						else
						{
							if (s != 1)
								stbi__err("bad huffman code");
							if ((stbi__jpeg_get_bit()) != 0)
								s = (int)(bit);
							else
								s = (int)(-bit);
						}
						while (k <= spec_end)
						{
							short* p = &data[stbi__jpeg_dezigzag[k++]];
							if (*p != 0)
							{
								if ((stbi__jpeg_get_bit()) != 0)
									if ((*p & bit) == (0))
									{
										if ((*p) > (0))
											*p += (short)(bit);
										else
											*p -= (short)(bit);
									}
							}
							else
							{
								if ((r) == (0))
								{
									*p = ((short)(s));
									break;
								}
								--r;
							}
						}
					}
					while (k <= spec_end);
				}
			}

			return (int)(1);
		}

		private static byte stbi__clamp(int x)
		{
			if (((uint)(x)) > (255))
			{
				if ((x) < (0))
					return (byte)(0);
				if ((x) > (255))
					return (byte)(255);
			}

			return (byte)(x);
		}

		private static void stbi__idct_block(byte* _out_, int out_stride, short* data)
		{
			int i = 0;
			int* val = stackalloc int[64];
			int* v = val;
			byte* o;
			short* d = ((short*)data);
			for (i = (int)(0); (i) < (8); ++i, ++d, ++v)
			{
				if ((((((((d[8]) == (0)) && ((d[16]) == (0))) && ((d[24]) == (0))) && ((d[32]) == (0))) && ((d[40]) == (0))) && ((d[48]) == (0))) && ((d[56]) == (0)))
				{
					int dcterm = (int)(d[0] * 4);
					v[0] = (int)(v[8] = (int)(v[16] = (int)(v[24] = (int)(v[32] = (int)(v[40] = (int)(v[48] = (int)(v[56] = (int)(dcterm))))))));
				}
				else
				{
					int t0 = 0;
					int t1 = 0;
					int t2 = 0;
					int t3 = 0;
					int p1 = 0;
					int p2 = 0;
					int p3 = 0;
					int p4 = 0;
					int p5 = 0;
					int x0 = 0;
					int x1 = 0;
					int x2 = 0;
					int x3 = 0;
					p2 = (int)(d[16]);
					p3 = (int)(d[48]);
					p1 = (int)((p2 + p3) * ((int)((0.5411961f) * 4096 + 0.5)));
					t2 = (int)(p1 + p3 * ((int)((-1.847759065f) * 4096 + 0.5)));
					t3 = (int)(p1 + p2 * ((int)((0.765366865f) * 4096 + 0.5)));
					p2 = (int)(d[0]);
					p3 = (int)(d[32]);
					t0 = (int)((p2 + p3) * 4096);
					t1 = (int)((p2 - p3) * 4096);
					x0 = (int)(t0 + t3);
					x3 = (int)(t0 - t3);
					x1 = (int)(t1 + t2);
					x2 = (int)(t1 - t2);
					t0 = (int)(d[56]);
					t1 = (int)(d[40]);
					t2 = (int)(d[24]);
					t3 = (int)(d[8]);
					p3 = (int)(t0 + t2);
					p4 = (int)(t1 + t3);
					p1 = (int)(t0 + t3);
					p2 = (int)(t1 + t2);
					p5 = (int)((p3 + p4) * ((int)((1.175875602f) * 4096 + 0.5)));
					t0 = (int)(t0 * ((int)((0.298631336f) * 4096 + 0.5)));
					t1 = (int)(t1 * ((int)((2.053119869f) * 4096 + 0.5)));
					t2 = (int)(t2 * ((int)((3.072711026f) * 4096 + 0.5)));
					t3 = (int)(t3 * ((int)((1.501321110f) * 4096 + 0.5)));
					p1 = (int)(p5 + p1 * ((int)((-0.899976223f) * 4096 + 0.5)));
					p2 = (int)(p5 + p2 * ((int)((-2.562915447f) * 4096 + 0.5)));
					p3 = (int)(p3 * ((int)((-1.961570560f) * 4096 + 0.5)));
					p4 = (int)(p4 * ((int)((-0.390180644f) * 4096 + 0.5)));
					t3 += (int)(p1 + p4);
					t2 += (int)(p2 + p3);
					t1 += (int)(p2 + p4);
					t0 += (int)(p1 + p3);
					x0 += (int)(512);
					x1 += (int)(512);
					x2 += (int)(512);
					x3 += (int)(512);
					v[0] = (int)((x0 + t3) >> 10);
					v[56] = (int)((x0 - t3) >> 10);
					v[8] = (int)((x1 + t2) >> 10);
					v[48] = (int)((x1 - t2) >> 10);
					v[16] = (int)((x2 + t1) >> 10);
					v[40] = (int)((x2 - t1) >> 10);
					v[24] = (int)((x3 + t0) >> 10);
					v[32] = (int)((x3 - t0) >> 10);
				}
			}
			for (i = (int)(0), v = val, o = _out_; (i) < (8); ++i, v += 8, o += out_stride)
			{
				int t0 = 0;
				int t1 = 0;
				int t2 = 0;
				int t3 = 0;
				int p1 = 0;
				int p2 = 0;
				int p3 = 0;
				int p4 = 0;
				int p5 = 0;
				int x0 = 0;
				int x1 = 0;
				int x2 = 0;
				int x3 = 0;
				p2 = (int)(v[2]);
				p3 = (int)(v[6]);
				p1 = (int)((p2 + p3) * ((int)((0.5411961f) * 4096 + 0.5)));
				t2 = (int)(p1 + p3 * ((int)((-1.847759065f) * 4096 + 0.5)));
				t3 = (int)(p1 + p2 * ((int)((0.765366865f) * 4096 + 0.5)));
				p2 = (int)(v[0]);
				p3 = (int)(v[4]);
				t0 = (int)((p2 + p3) * 4096);
				t1 = (int)((p2 - p3) * 4096);
				x0 = (int)(t0 + t3);
				x3 = (int)(t0 - t3);
				x1 = (int)(t1 + t2);
				x2 = (int)(t1 - t2);
				t0 = (int)(v[7]);
				t1 = (int)(v[5]);
				t2 = (int)(v[3]);
				t3 = (int)(v[1]);
				p3 = (int)(t0 + t2);
				p4 = (int)(t1 + t3);
				p1 = (int)(t0 + t3);
				p2 = (int)(t1 + t2);
				p5 = (int)((p3 + p4) * ((int)((1.175875602f) * 4096 + 0.5)));
				t0 = (int)(t0 * ((int)((0.298631336f) * 4096 + 0.5)));
				t1 = (int)(t1 * ((int)((2.053119869f) * 4096 + 0.5)));
				t2 = (int)(t2 * ((int)((3.072711026f) * 4096 + 0.5)));
				t3 = (int)(t3 * ((int)((1.501321110f) * 4096 + 0.5)));
				p1 = (int)(p5 + p1 * ((int)((-0.899976223f) * 4096 + 0.5)));
				p2 = (int)(p5 + p2 * ((int)((-2.562915447f) * 4096 + 0.5)));
				p3 = (int)(p3 * ((int)((-1.961570560f) * 4096 + 0.5)));
				p4 = (int)(p4 * ((int)((-0.390180644f) * 4096 + 0.5)));
				t3 += (int)(p1 + p4);
				t2 += (int)(p2 + p3);
				t1 += (int)(p2 + p4);
				t0 += (int)(p1 + p3);
				x0 += (int)(65536 + (128 << 17));
				x1 += (int)(65536 + (128 << 17));
				x2 += (int)(65536 + (128 << 17));
				x3 += (int)(65536 + (128 << 17));
				o[0] = (byte)(stbi__clamp((int)((x0 + t3) >> 17)));
				o[7] = (byte)(stbi__clamp((int)((x0 - t3) >> 17)));
				o[1] = (byte)(stbi__clamp((int)((x1 + t2) >> 17)));
				o[6] = (byte)(stbi__clamp((int)((x1 - t2) >> 17)));
				o[2] = (byte)(stbi__clamp((int)((x2 + t1) >> 17)));
				o[5] = (byte)(stbi__clamp((int)((x2 - t1) >> 17)));
				o[3] = (byte)(stbi__clamp((int)((x3 + t0) >> 17)));
				o[4] = (byte)(stbi__clamp((int)((x3 - t0) >> 17)));
			}
		}

		private byte stbi__get_marker()
		{
			byte x = 0;
			if (marker != 0xff)
			{
				x = (byte)(marker);
				marker = (byte)(0xff);
				return (byte)(x);
			}

			x = (byte)(stbi__get8());
			if (x != 0xff)
				return (byte)(0xff);
			while ((x) == (0xff))
			{
				x = (byte)(stbi__get8());
			}
			return (byte)(x);
		}

		private void stbi__jpeg_reset()
		{
			code_bits = (int)(0);
			code_buffer = (uint)(0);
			nomore = (int)(0);
			img_comp[0].dc_pred = (int)(img_comp[1].dc_pred = (int)(img_comp[2].dc_pred = (int)(img_comp[3].dc_pred = (int)(0))));
			marker = (byte)(0xff);
			todo = (int)((restart_interval) != 0 ? restart_interval : 0x7fffffff);
			eob_run = (int)(0);
		}

		private int stbi__parse_entropy_coded_data()
		{
			stbi__jpeg_reset();
			if (progressive == 0)
			{
				if ((scan_n) == (1))
				{
					int i = 0;
					int j = 0;
					short* data = stackalloc short[64];
					int n = (int)(order[0]);
					int w = (int)((img_comp[n].x + 7) >> 3);
					int h = (int)((img_comp[n].y + 7) >> 3);
					for (j = (int)(0); (j) < (h); ++j)
					{
						for (i = (int)(0); (i) < (w); ++i)
						{
							int ha = (int)(img_comp[n].ha);
							if (stbi__jpeg_decode_block(data, huff_dc[img_comp[n].hd], huff_ac[ha], fast_ac[ha], (int)(n), dequant[img_comp[n].tq]) == 0)
								return (int)(0);
							idct_block_kernel(img_comp[n].data + img_comp[n].w2 * j * 8 + i * 8, (int)(img_comp[n].w2), data);
							if (--todo <= 0)
							{
								if ((code_bits) < (24))
									stbi__grow_buffer_unsafe();
								if (!(((marker) >= (0xd0)) && ((marker) <= 0xd7)))
									return (int)(1);
								stbi__jpeg_reset();
							}
						}
					}
					return (int)(1);
				}
				else
				{
					int i = 0;
					int j = 0;
					int k = 0;
					int x = 0;
					int y = 0;
					short* data = stackalloc short[64];
					for (j = (int)(0); (j) < (img_mcu_y); ++j)
					{
						for (i = (int)(0); (i) < (img_mcu_x); ++i)
						{
							for (k = (int)(0); (k) < (scan_n); ++k)
							{
								int n = (int)(order[k]);
								for (y = (int)(0); (y) < (img_comp[n].v); ++y)
								{
									for (x = (int)(0); (x) < (img_comp[n].h); ++x)
									{
										int x2 = (int)((i * img_comp[n].h + x) * 8);
										int y2 = (int)((j * img_comp[n].v + y) * 8);
										int ha = (int)(img_comp[n].ha);
										if (stbi__jpeg_decode_block(data, huff_dc[img_comp[n].hd], huff_ac[ha], fast_ac[ha], (int)(n), dequant[img_comp[n].tq]) == 0)
											return (int)(0);
										idct_block_kernel(img_comp[n].data + img_comp[n].w2 * y2 + x2, (int)(img_comp[n].w2), data);
									}
								}
							}
							if (--todo <= 0)
							{
								if ((code_bits) < (24))
									stbi__grow_buffer_unsafe();
								if (!(((marker) >= (0xd0)) && ((marker) <= 0xd7)))
									return (int)(1);
								stbi__jpeg_reset();
							}
						}
					}
					return (int)(1);
				}
			}
			else
			{
				if ((scan_n) == (1))
				{
					int i = 0;
					int j = 0;
					int n = (int)(order[0]);
					int w = (int)((img_comp[n].x + 7) >> 3);
					int h = (int)((img_comp[n].y + 7) >> 3);
					for (j = (int)(0); (j) < (h); ++j)
					{
						for (i = (int)(0); (i) < (w); ++i)
						{
							short* data = img_comp[n].coeff + 64 * (i + j * img_comp[n].coeff_w);
							if ((spec_start) == (0))
							{
								if (stbi__jpeg_decode_block_prog_dc(data, huff_dc[img_comp[n].hd], (int)(n)) == 0)
									return (int)(0);
							}
							else
							{
								int ha = (int)(img_comp[n].ha);
								if (stbi__jpeg_decode_block_prog_ac(data, huff_ac[ha], fast_ac[ha]) == 0)
									return (int)(0);
							}
							if (--todo <= 0)
							{
								if ((code_bits) < (24))
									stbi__grow_buffer_unsafe();
								if (!(((marker) >= (0xd0)) && ((marker) <= 0xd7)))
									return (int)(1);
								stbi__jpeg_reset();
							}
						}
					}
					return (int)(1);
				}
				else
				{
					int i = 0;
					int j = 0;
					int k = 0;
					int x = 0;
					int y = 0;
					for (j = (int)(0); (j) < (img_mcu_y); ++j)
					{
						for (i = (int)(0); (i) < (img_mcu_x); ++i)
						{
							for (k = (int)(0); (k) < (scan_n); ++k)
							{
								int n = (int)(order[k]);
								for (y = (int)(0); (y) < (img_comp[n].v); ++y)
								{
									for (x = (int)(0); (x) < (img_comp[n].h); ++x)
									{
										int x2 = (int)(i * img_comp[n].h + x);
										int y2 = (int)(j * img_comp[n].v + y);
										short* data = img_comp[n].coeff + 64 * (x2 + y2 * img_comp[n].coeff_w);
										if (stbi__jpeg_decode_block_prog_dc(data, huff_dc[img_comp[n].hd], (int)(n)) == 0)
											return (int)(0);
									}
								}
							}
							if (--todo <= 0)
							{
								if ((code_bits) < (24))
									stbi__grow_buffer_unsafe();
								if (!(((marker) >= (0xd0)) && ((marker) <= 0xd7)))
									return (int)(1);
								stbi__jpeg_reset();
							}
						}
					}
					return (int)(1);
				}
			}
		}

		private static void stbi__jpeg_dequantize(short* data, ushort[] dequant)
		{
			int i = 0;
			for (i = (int)(0); (i) < (64); ++i)
			{
				data[i] *= (short)(dequant[i]);
			}
		}

		private void stbi__jpeg_finish()
		{
			if ((progressive) != 0)
			{
				int i = 0;
				int j = 0;
				int n = 0;
				for (n = (int)(0); (n) < (img_n); ++n)
				{
					int w = (int)((img_comp[n].x + 7) >> 3);
					int h = (int)((img_comp[n].y + 7) >> 3);
					for (j = (int)(0); (j) < (h); ++j)
					{
						for (i = (int)(0); (i) < (w); ++i)
						{
							short* data = img_comp[n].coeff + 64 * (i + j * img_comp[n].coeff_w);
							stbi__jpeg_dequantize(data, dequant[img_comp[n].tq]);
							idct_block_kernel(img_comp[n].data + img_comp[n].w2 * j * 8 + i * 8, (int)(img_comp[n].w2), data);
						}
					}
				}
			}

		}

		private int stbi__process_marker(int m)
		{
			int L = 0;
			switch (m)
			{
				case 0xff:
					stbi__err("expected marker");
					break;
				case 0xDD:
					if (stbi__get16be() != 4)
						stbi__err("bad DRI len");
					restart_interval = (int)(stbi__get16be());
					return (int)(1);
				case 0xDB:
					L = (int)(stbi__get16be() - 2);
					while ((L) > (0))
					{
						int q = (int)(stbi__get8());
						int p = (int)(q >> 4);
						int sixteen = (p != 0) ? 1 : 0;
						int t = (int)(q & 15);
						int i = 0;
						if ((p != 0) && (p != 1))
							stbi__err("bad DQT type");
						if ((t) > (3))
							stbi__err("bad DQT table");
						for (i = (int)(0); (i) < (64); ++i)
						{
							dequant[t][stbi__jpeg_dezigzag[i]] = ((ushort)((sixteen) != 0 ? stbi__get16be() : stbi__get8()));
						}
						L -= (int)((sixteen) != 0 ? 129 : 65);
					}
					return (int)((L) == (0) ? 1 : 0);
				case 0xC4:
					L = (int)(stbi__get16be() - 2);
					while ((L) > (0))
					{
						byte[] v;
						int* sizes = stackalloc int[16];
						int i = 0;
						int n = (int)(0);
						int q = (int)(stbi__get8());
						int tc = (int)(q >> 4);
						int th = (int)(q & 15);
						if (((tc) > (1)) || ((th) > (3)))
							stbi__err("bad DHT header");
						for (i = (int)(0); (i) < (16); ++i)
						{
							sizes[i] = (int)(stbi__get8());
							n += (int)(sizes[i]);
						}
						L -= (int)(17);
						if ((tc) == (0))
						{
							if (stbi__build_huffman(huff_dc[th], sizes) == 0)
								return (int)(0);
							v = huff_dc[th].values;
						}
						else
						{
							if (stbi__build_huffman(huff_ac[th], sizes) == 0)
								return (int)(0);
							v = huff_ac[th].values;
						}
						for (i = (int)(0); (i) < (n); ++i)
						{
							v[i] = (byte)(stbi__get8());
						}
						if (tc != 0)
							stbi__build_fast_ac(fast_ac[th], huff_ac[th]);
						L -= (int)(n);
					}
					return (int)((L) == (0) ? 1 : 0);
			}

			if ((((m) >= (0xE0)) && (m <= 0xEF)) || ((m) == (0xFE)))
			{
				L = (int)(stbi__get16be());
				if ((L) < (2))
				{
					if ((m) == (0xFE))
						stbi__err("bad COM len");
					else
						stbi__err("bad APP len");
				}
				L -= (int)(2);
				if (((m) == (0xE0)) && ((L) >= (5)))
				{
					byte* tag = stackalloc byte[5];
					tag[0] = (byte)('J');
					tag[1] = (byte)('F');
					tag[2] = (byte)('I');
					tag[3] = (byte)('F');
					tag[4] = (byte)('\0');
					int ok = (int)(1);
					int i = 0;
					for (i = (int)(0); (i) < (5); ++i)
					{
						if (stbi__get8() != tag[i])
							ok = (int)(0);
					}
					L -= (int)(5);
					if ((ok) != 0)
						jfif = (int)(1);
				}
				else if (((m) == (0xEE)) && ((L) >= (12)))
				{
					byte* tag = stackalloc byte[6];
					tag[0] = (byte)('A');
					tag[1] = (byte)('d');
					tag[2] = (byte)('o');
					tag[3] = (byte)('b');
					tag[4] = (byte)('e');
					tag[5] = (byte)('\0');
					int ok = (int)(1);
					int i = 0;
					for (i = (int)(0); (i) < (6); ++i)
					{
						if (stbi__get8() != tag[i])
							ok = (int)(0);
					}
					L -= (int)(6);
					if ((ok) != 0)
					{
						stbi__get8();
						stbi__get16be();
						stbi__get16be();
						app14_color_transform = (int)(stbi__get8());
						L -= (int)(6);
					}
				}
				stbi__skip((int)(L));
				return (int)(1);
			}

			return 0;
		}

		private int stbi__process_scan_header()
		{
			int i = 0;
			int Ls = (int)(stbi__get16be());
			scan_n = (int)(stbi__get8());
			if ((((scan_n) < (1)) || ((scan_n) > (4))) || ((scan_n) > (img_n)))
				stbi__err("bad SOS component count");
			if (Ls != 6 + 2 * scan_n)
				stbi__err("bad SOS len");
			for (i = (int)(0); (i) < (scan_n); ++i)
			{
				int id = (int)(stbi__get8());
				int which = 0;
				int q = (int)(stbi__get8());
				for (which = (int)(0); (which) < (img_n); ++which)
				{
					if ((img_comp[which].id) == (id))
						break;
				}
				if ((which) == (img_n))
					return (int)(0);
				img_comp[which].hd = (int)(q >> 4);
				if ((img_comp[which].hd) > (3))
					stbi__err("bad DC huff");
				img_comp[which].ha = (int)(q & 15);
				if ((img_comp[which].ha) > (3))
					stbi__err("bad AC huff");
				order[i] = (int)(which);
			}
			{
				int aa = 0;
				spec_start = (int)(stbi__get8());
				spec_end = (int)(stbi__get8());
				aa = (int)(stbi__get8());
				succ_high = (int)(aa >> 4);
				succ_low = (int)(aa & 15);
				if ((progressive) != 0)
				{
					if ((((((spec_start) > (63)) || ((spec_end) > (63))) || ((spec_start) > (spec_end))) || ((succ_high) > (13))) || ((succ_low) > (13)))
						stbi__err("bad SOS");
				}
				else
				{
					if (spec_start != 0)
						stbi__err("bad SOS");
					if ((succ_high != 0) || (succ_low != 0))
						stbi__err("bad SOS");
					spec_end = (int)(63);
				}
			}

			return (int)(1);
		}

		private int stbi__free_jpeg_components(int ncomp, int why)
		{
			int i = 0;
			for (i = (int)(0); (i) < (ncomp); ++i)
			{
				if ((img_comp[i].raw_data) != null)
				{
					CRuntime.free(img_comp[i].raw_data);
					img_comp[i].raw_data = (null);
					img_comp[i].data = (null);
				}
				if ((img_comp[i].raw_coeff) != null)
				{
					CRuntime.free(img_comp[i].raw_coeff);
					img_comp[i].raw_coeff = null;
					img_comp[i].coeff = null;
				}
				if ((img_comp[i].linebuf) != null)
				{
					CRuntime.free(img_comp[i].linebuf);
					img_comp[i].linebuf = (null);
				}
			}
			return (int)(why);
		}

		private int stbi__process_frame_header(int scan)
		{
			int Lf = 0;
			int p = 0;
			int i = 0;
			int q = 0;
			int h_max = (int)(1);
			int v_max = (int)(1);
			int c = 0;
			Lf = (int)(stbi__get16be());
			if ((Lf) < (11))
				stbi__err("bad SOF len");
			p = (int)(stbi__get8());
			if (p != 8)
				stbi__err("only 8-bit");
			img_y = (stbi__get16be());
			if ((img_y) == (0))
				stbi__err("no header height");
			img_x = (stbi__get16be());
			if ((img_x) == (0))
				stbi__err("0 width");
			c = (int)(stbi__get8());
			if (((c != 3) && (c != 1)) && (c != 4))
				stbi__err("bad component count");
			img_n = (int)(c);
			for (i = (int)(0); (i) < (c); ++i)
			{
				img_comp[i].data = (null);
				img_comp[i].linebuf = (null);
			}
			if (Lf != 8 + 3 * img_n)
				stbi__err("bad SOF len");
			rgb = (int)(0);
			for (i = (int)(0); (i) < (img_n); ++i)
			{
				byte* rgb = stackalloc byte[3];
				rgb[0] = (byte)('R');
				rgb[1] = (byte)('G');
				rgb[2] = (byte)('B');
				img_comp[i].id = (int)(stbi__get8());
				if (((img_n) == (3)) && ((img_comp[i].id) == (rgb[i])))
					++rgb;
				q = (int)(stbi__get8());
				img_comp[i].h = (int)(q >> 4);
				if ((img_comp[i].h == 0) || ((img_comp[i].h) > (4)))
					stbi__err("bad H");
				img_comp[i].v = (int)(q & 15);
				if ((img_comp[i].v == 0) || ((img_comp[i].v) > (4)))
					stbi__err("bad V");
				img_comp[i].tq = (int)(stbi__get8());
				if ((img_comp[i].tq) > (3))
					stbi__err("bad TQ");
			}
			if (scan != STBI__SCAN_load)
				return (int)(1);
			if (Utility.stbi__mad3sizes_valid((int)(img_x), (int)(img_y), (int)(img_n), (int)(0)) == 0)
				stbi__err("too large");
			for (i = (int)(0); (i) < (img_n); ++i)
			{
				if ((img_comp[i].h) > (h_max))
					h_max = (int)(img_comp[i].h);
				if ((img_comp[i].v) > (v_max))
					v_max = (int)(img_comp[i].v);
			}
			img_h_max = (int)(h_max);
			img_v_max = (int)(v_max);
			img_mcu_w = (int)(h_max * 8);
			img_mcu_h = (int)(v_max * 8);
			img_mcu_x = (int)((img_x + img_mcu_w - 1) / img_mcu_w);
			img_mcu_y = (int)((img_y + img_mcu_h - 1) / img_mcu_h);
			for (i = (int)(0); (i) < (img_n); ++i)
			{
				img_comp[i].x = (int)((img_x * img_comp[i].h + h_max - 1) / h_max);
				img_comp[i].y = (int)((img_y * img_comp[i].v + v_max - 1) / v_max);
				img_comp[i].w2 = (int)(img_mcu_x * img_comp[i].h * 8);
				img_comp[i].h2 = (int)(img_mcu_y * img_comp[i].v * 8);
				img_comp[i].coeff = null;
				img_comp[i].raw_coeff = null;
				img_comp[i].linebuf = (null);
				img_comp[i].raw_data = Utility.stbi__malloc_mad2((int)(img_comp[i].w2), (int)(img_comp[i].h2), (int)(15));
				img_comp[i].data = (byte*)((((long)img_comp[i].raw_data + 15) & ~15));
				if ((progressive) != 0)
				{
					img_comp[i].coeff_w = (int)(img_comp[i].w2 / 8);
					img_comp[i].coeff_h = (int)(img_comp[i].h2 / 8);
					img_comp[i].raw_coeff = Utility.stbi__malloc_mad3((int)(img_comp[i].w2), (int)(img_comp[i].h2), (int)(sizeof(short)), (int)(15));
					img_comp[i].coeff = (short*)((((long)img_comp[i].raw_coeff + 15) & ~15));
				}
			}
			return (int)(1);
		}

		private bool stbi__decode_jpeg_header(int scan)
		{
			int m = 0;
			jfif = (int)(0);
			app14_color_transform = (int)(-1);
			marker = (byte)(0xff);
			m = (int)(stbi__get_marker());
			if (!((m) == (0xd8)))
				stbi__err("no SOI");
			if ((scan) == (STBI__SCAN_type))
				return true;
			m = (int)(stbi__get_marker());
			while (!((((m) == (0xc0)) || ((m) == (0xc1))) || ((m) == (0xc2))))
			{
				if (stbi__process_marker((int)(m)) == 0)
					return false;
				m = (int)(stbi__get_marker());
				while ((m) == (0xff))
				{
					if ((stbi__at_eof()) != 0)
						stbi__err("no SOF");
					m = (int)(stbi__get_marker());
				}
			}
			progressive = (int)((m) == (0xc2) ? 1 : 0);
			if (stbi__process_frame_header((int)(scan)) == 0)
				return false;
			return true;
		}

		private int stbi__decode_jpeg_image()
		{
			int m = 0;
			for (m = (int)(0); (m) < (4); m++)
			{
				img_comp[m].raw_data = (null);
				img_comp[m].raw_coeff = (null);
			}
			restart_interval = (int)(0);
			if (!stbi__decode_jpeg_header((int)(STBI__SCAN_load)))
				return (int)(0);
			m = (int)(stbi__get_marker());
			while (!((m) == (0xd9)))
			{
				if (((m) == (0xda)))
				{
					if (stbi__process_scan_header() == 0)
						return (int)(0);
					if (stbi__parse_entropy_coded_data() == 0)
						return (int)(0);
					if ((marker) == (0xff))
					{
						while (stbi__at_eof() == 0)
						{
							int x = (int)(stbi__get8());
							if ((x) == (255))
							{
								marker = (byte)(stbi__get8());
								break;
							}
						}
					}
				}
				else if (((m) == (0xdc)))
				{
					int Ld = (int)(stbi__get16be());
					uint NL = (uint)(stbi__get16be());
					if (Ld != 4)
						stbi__err("bad DNL len");
					if (NL != img_y)
						stbi__err("bad DNL height");
				}
				else
				{
					if (stbi__process_marker((int)(m)) == 0)
						return (int)(0);
				}
				m = (int)(stbi__get_marker());
			}
			if ((progressive) != 0)
				stbi__jpeg_finish();
			return (int)(1);
		}

		private static byte* resample_row_1(byte* _out_, byte* in_near, byte* in_far, int w, int hs)
		{
			return in_near;
		}

		private static byte* stbi__resample_row_v_2(byte* _out_, byte* in_near, byte* in_far, int w, int hs)
		{
			int i = 0;
			for (i = (int)(0); (i) < (w); ++i)
			{
				_out_[i] = ((byte)((3 * in_near[i] + in_far[i] + 2) >> 2));
			}
			return _out_;
		}

		private static byte* stbi__resample_row_h_2(byte* _out_, byte* in_near, byte* in_far, int w, int hs)
		{
			int i = 0;
			byte* input = in_near;
			if ((w) == (1))
			{
				_out_[0] = (byte)(_out_[1] = (byte)(input[0]));
				return _out_;
			}

			_out_[0] = (byte)(input[0]);
			_out_[1] = ((byte)((input[0] * 3 + input[1] + 2) >> 2));
			for (i = (int)(1); (i) < (w - 1); ++i)
			{
				int n = (int)(3 * input[i] + 2);
				_out_[i * 2 + 0] = ((byte)((n + input[i - 1]) >> 2));
				_out_[i * 2 + 1] = ((byte)((n + input[i + 1]) >> 2));
			}
			_out_[i * 2 + 0] = ((byte)((input[w - 2] * 3 + input[w - 1] + 2) >> 2));
			_out_[i * 2 + 1] = (byte)(input[w - 1]);
			return _out_;
		}

		private static byte* stbi__resample_row_hv_2(byte* _out_, byte* in_near, byte* in_far, int w, int hs)
		{
			int i = 0;
			int t0 = 0;
			int t1 = 0;
			if ((w) == (1))
			{
				_out_[0] = (byte)(_out_[1] = ((byte)((3 * in_near[0] + in_far[0] + 2) >> 2)));
				return _out_;
			}

			t1 = (int)(3 * in_near[0] + in_far[0]);
			_out_[0] = ((byte)((t1 + 2) >> 2));
			for (i = (int)(1); (i) < (w); ++i)
			{
				t0 = (int)(t1);
				t1 = (int)(3 * in_near[i] + in_far[i]);
				_out_[i * 2 - 1] = ((byte)((3 * t0 + t1 + 8) >> 4));
				_out_[i * 2] = ((byte)((3 * t1 + t0 + 8) >> 4));
			}
			_out_[w * 2 - 1] = ((byte)((t1 + 2) >> 2));
			return _out_;
		}

		private static byte* stbi__resample_row_generic(byte* _out_, byte* in_near, byte* in_far, int w, int hs)
		{
			int i = 0;
			int j = 0;
			for (i = (int)(0); (i) < (w); ++i)
			{
				for (j = (int)(0); (j) < (hs); ++j)
				{
					_out_[i * hs + j] = (byte)(in_near[i]);
				}
			}
			return _out_;
		}

		private static void stbi__YCbCr_to_RGB_row(byte* _out_, byte* y, byte* pcb, byte* pcr, int count, int step)
		{
			int i = 0;
			for (i = (int)(0); (i) < (count); ++i)
			{
				int y_fixed = (int)((y[i] << 20) + (1 << 19));
				int r = 0;
				int g = 0;
				int b = 0;
				int cr = (int)(pcr[i] - 128);
				int cb = (int)(pcb[i] - 128);
				r = (int)(y_fixed + cr * (((int)((1.40200f) * 4096.0f + 0.5f)) << 8));
				g = (int)(y_fixed + (cr * -(((int)((0.71414f) * 4096.0f + 0.5f)) << 8)) + ((cb * -(((int)((0.34414f) * 4096.0f + 0.5f)) << 8)) & 0xffff0000));
				b = (int)(y_fixed + cb * (((int)((1.77200f) * 4096.0f + 0.5f)) << 8));
				r >>= 20;
				g >>= 20;
				b >>= 20;
				if (((uint)(r)) > (255))
				{
					if ((r) < (0))
						r = (int)(0);
					else
						r = (int)(255);
				}
				if (((uint)(g)) > (255))
				{
					if ((g) < (0))
						g = (int)(0);
					else
						g = (int)(255);
				}
				if (((uint)(b)) > (255))
				{
					if ((b) < (0))
						b = (int)(0);
					else
						b = (int)(255);
				}
				_out_[0] = ((byte)(r));
				_out_[1] = ((byte)(g));
				_out_[2] = ((byte)(b));
				_out_[3] = (byte)(255);
				_out_ += step;
			}
		}

		private void stbi__setup_jpeg()
		{
			idct_block_kernel = stbi__idct_block;
			YCbCr_to_RGB_kernel = stbi__YCbCr_to_RGB_row;
			resample_row_hv_2_kernel = stbi__resample_row_hv_2;
		}

		private void stbi__cleanup_jpeg()
		{
			stbi__free_jpeg_components((int)(img_n), (int)(0));
		}

		private static byte stbi__blinn_8x8(byte x, byte y)
		{
			uint t = (uint)(x * y + 128);
			return (byte)((t + (t >> 8)) >> 8);
		}

		private byte* load_jpeg_image(int* out_x, int* out_y, int* comp, int req_comp)
		{
			int n = 0;
			int decode_n = 0;
			int is_rgb = 0;
			img_n = (int)(0);
			if (((req_comp) < (0)) || ((req_comp) > (4)))
				stbi__err("bad req_comp");
			if (stbi__decode_jpeg_image() == 0)
			{
				stbi__cleanup_jpeg();
				return (null);
			}

			n = (int)((req_comp) != 0 ? req_comp : (img_n) >= (3) ? 3 : 1);
			is_rgb = (int)(((img_n) == (3)) && (((rgb) == (3)) || (((app14_color_transform) == (0)) && (jfif == 0))) ? 1 : 0);
			if ((((img_n) == (3)) && ((n) < (3))) && (is_rgb == 0))
				decode_n = (int)(1);
			else
				decode_n = (int)(img_n);
			{
				int k = 0;
				uint i = 0;
				uint j = 0;
				byte* output;
				byte** coutput = stackalloc byte*[4];
				coutput[0] = (null);
				coutput[1] = (null);
				coutput[2] = (null);
				coutput[3] = (null);
				var res_comp = new stbi__resample[4];
				for (var kkk = 0; kkk < res_comp.Length; ++kkk)
					res_comp[kkk] = new stbi__resample();
				for (k = (int)(0); (k) < (decode_n); ++k)
				{
					stbi__resample r = res_comp[k];
					img_comp[k].linebuf = (byte*)(Utility.stbi__malloc((ulong)(img_x + 3)));
					r.hs = (int)(img_h_max / img_comp[k].h);
					r.vs = (int)(img_v_max / img_comp[k].v);
					r.ystep = (int)(r.vs >> 1);
					r.w_lores = (int)((img_x + r.hs - 1) / r.hs);
					r.ypos = (int)(0);
					r.line0 = r.line1 = img_comp[k].data;
					if (((r.hs) == (1)) && ((r.vs) == (1)))
						r.resample = resample_row_1;
					else if (((r.hs) == (1)) && ((r.vs) == (2)))
						r.resample = stbi__resample_row_v_2;
					else if (((r.hs) == (2)) && ((r.vs) == (1)))
						r.resample = stbi__resample_row_h_2;
					else if (((r.hs) == (2)) && ((r.vs) == (2)))
						r.resample = resample_row_hv_2_kernel;
					else
						r.resample = stbi__resample_row_generic;
				}
				output = (byte*)(Utility.stbi__malloc_mad3((int)(n), (int)(img_x), (int)(img_y), (int)(1)));
				for (j = (uint)(0); (j) < (img_y); ++j)
				{
					byte* _out_ = output + n * img_x * j;
					for (k = (int)(0); (k) < (decode_n); ++k)
					{
						stbi__resample r = res_comp[k];
						int y_bot = (int)((r.ystep) >= (r.vs >> 1) ? 1 : 0);
						coutput[k] = r.resample(img_comp[k].linebuf, (y_bot) != 0 ? r.line1 : r.line0, (y_bot) != 0 ? r.line0 : r.line1, (int)(r.w_lores), (int)(r.hs));
						if ((++r.ystep) >= (r.vs))
						{
							r.ystep = (int)(0);
							r.line0 = r.line1;
							if ((++r.ypos) < (img_comp[k].y))
								r.line1 += img_comp[k].w2;
						}
					}
					if ((n) >= (3))
					{
						byte* y = coutput[0];
						if ((img_n) == (3))
						{
							if ((is_rgb) != 0)
							{
								for (i = (uint)(0); (i) < (img_x); ++i)
								{
									_out_[0] = (byte)(y[i]);
									_out_[1] = (byte)(coutput[1][i]);
									_out_[2] = (byte)(coutput[2][i]);
									_out_[3] = (byte)(255);
									_out_ += n;
								}
							}
							else
							{
								YCbCr_to_RGB_kernel(_out_, y, coutput[1], coutput[2], (int)(img_x), (int)(n));
							}
						}
						else if ((img_n) == (4))
						{
							if ((app14_color_transform) == (0))
							{
								for (i = (uint)(0); (i) < (img_x); ++i)
								{
									byte m = (byte)(coutput[3][i]);
									_out_[0] = (byte)(stbi__blinn_8x8((byte)(coutput[0][i]), (byte)(m)));
									_out_[1] = (byte)(stbi__blinn_8x8((byte)(coutput[1][i]), (byte)(m)));
									_out_[2] = (byte)(stbi__blinn_8x8((byte)(coutput[2][i]), (byte)(m)));
									_out_[3] = (byte)(255);
									_out_ += n;
								}
							}
							else if ((app14_color_transform) == (2))
							{
								YCbCr_to_RGB_kernel(_out_, y, coutput[1], coutput[2], (int)(img_x), (int)(n));
								for (i = (uint)(0); (i) < (img_x); ++i)
								{
									byte m = (byte)(coutput[3][i]);
									_out_[0] = (byte)(stbi__blinn_8x8((byte)(255 - _out_[0]), (byte)(m)));
									_out_[1] = (byte)(stbi__blinn_8x8((byte)(255 - _out_[1]), (byte)(m)));
									_out_[2] = (byte)(stbi__blinn_8x8((byte)(255 - _out_[2]), (byte)(m)));
									_out_ += n;
								}
							}
							else
							{
								YCbCr_to_RGB_kernel(_out_, y, coutput[1], coutput[2], (int)(img_x), (int)(n));
							}
						}
						else
							for (i = (uint)(0); (i) < (img_x); ++i)
							{
								_out_[0] = (byte)(_out_[1] = (byte)(_out_[2] = (byte)(y[i])));
								_out_[3] = (byte)(255);
								_out_ += n;
							}
					}
					else
					{
						if ((is_rgb) != 0)
						{
							if ((n) == (1))
								for (i = (uint)(0); (i) < (img_x); ++i)
								{
									*_out_++ = (byte)(Conversion.stbi__compute_y((int)(coutput[0][i]), (int)(coutput[1][i]), (int)(coutput[2][i])));
								}
							else
							{
								for (i = (uint)(0); (i) < (img_x); ++i, _out_ += 2)
								{
									_out_[0] = (byte)(Conversion.stbi__compute_y((int)(coutput[0][i]), (int)(coutput[1][i]), (int)(coutput[2][i])));
									_out_[1] = (byte)(255);
								}
							}
						}
						else if (((img_n) == (4)) && ((app14_color_transform) == (0)))
						{
							for (i = (uint)(0); (i) < (img_x); ++i)
							{
								byte m = (byte)(coutput[3][i]);
								byte r = (byte)(stbi__blinn_8x8((byte)(coutput[0][i]), (byte)(m)));
								byte g = (byte)(stbi__blinn_8x8((byte)(coutput[1][i]), (byte)(m)));
								byte b = (byte)(stbi__blinn_8x8((byte)(coutput[2][i]), (byte)(m)));
								_out_[0] = (byte)(Conversion.stbi__compute_y((int)(r), (int)(g), (int)(b)));
								_out_[1] = (byte)(255);
								_out_ += n;
							}
						}
						else if (((img_n) == (4)) && ((app14_color_transform) == (2)))
						{
							for (i = (uint)(0); (i) < (img_x); ++i)
							{
								_out_[0] = (byte)(stbi__blinn_8x8((byte)(255 - coutput[0][i]), (byte)(coutput[3][i])));
								_out_[1] = (byte)(255);
								_out_ += n;
							}
						}
						else
						{
							byte* y = coutput[0];
							if ((n) == (1))
								for (i = (uint)(0); (i) < (img_x); ++i)
								{
									_out_[i] = (byte)(y[i]);
								}
							else
								for (i = (uint)(0); (i) < (img_x); ++i)
								{
									*_out_++ = (byte)(y[i]);
									*_out_++ = (byte)(255);
								}
						}
					}
				}
				stbi__cleanup_jpeg();
				*out_x = (int)(img_x);
				*out_y = (int)(img_y);
				if ((comp) != null)
					*comp = (int)((img_n) >= (3) ? 3 : 1);
				return output;
			}
		}

		protected override ImageResult InternalDecode(ColorComponents? requiredComponents)
		{
			stbi__setup_jpeg();

			int x, y, comp;
			int req_comp = requiredComponents == null ? 0 : (int)requiredComponents.Value;
			var result = load_jpeg_image(&x, &y, &comp, (int)(req_comp));

			return new ImageResult
			{
				Width = x,
				Height = y,
				ColorComponents = requiredComponents != null ? requiredComponents.Value : (ColorComponents)comp,
				BitsPerChannel = 8,
				Data = result
			};
		}

		public static bool Test(Stream stream)
		{
			var decoder = new JpgDecoder(stream);
			decoder.stbi__setup_jpeg();
			var r = decoder.stbi__decode_jpeg_header((int)(STBI__SCAN_type));
			stream.Rewind();

			return r;
		}

		public static ImageInfo? Info(Stream stream)
		{
			var decoder = new JpgDecoder(stream);

			var r = decoder.stbi__decode_jpeg_header((int)(STBI__SCAN_header));
			stream.Rewind();
			if (!r)
			{
				return null;
			}

			return new ImageInfo
			{
				Width = (int)decoder.img_x,
				Height = (int)decoder.img_y,
				ColorComponents = decoder.img_n >= 3 ? ColorComponents.RedGreenBlue: ColorComponents.Grey
			};
		}

		public static ImageResult Decode(Stream stream, ColorComponents? requiredComponents = null)
		{
			var decoder = new JpgDecoder(stream);
			return decoder.InternalDecode(requiredComponents);
		}
	}
}
