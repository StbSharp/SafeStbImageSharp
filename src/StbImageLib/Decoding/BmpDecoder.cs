﻿namespace StbImageLib.Decoding
{
	private unsafe class BmpDecoder: Decoder
	{
		private static int stbi__bmp_test_raw(stbi__context s)
		{
			int r = 0;
			int sz = 0;
			if (stbi__get8(s) != 'B')
				return (int)(0);
			if (stbi__get8(s) != 'M')
				return (int)(0);
			stbi__get32le(s);
			stbi__get16le(s);
			stbi__get16le(s);
			stbi__get32le(s);
			sz = (int)(stbi__get32le(s));
			r = (int)((((((sz) == (12)) || ((sz) == (40))) || ((sz) == (56))) || ((sz) == (108))) || ((sz) == (124)) ? 1 : 0);
			return (int)(r);
		}

		private static int stbi__bmp_test(stbi__context s)
		{
			int r = (int)(stbi__bmp_test_raw(s));
			stbi__rewind(s);
			return (int)(r);
		}

		private static int stbi__high_bit(uint z)
		{
			int n = (int)(0);
			if ((z) == (0))
				return (int)(-1);
			if ((z) >= (0x10000))
			{
				n += (int)(16);
				z >>= 16;
			}

			if ((z) >= (0x00100))
			{
				n += (int)(8);
				z >>= 8;
			}

			if ((z) >= (0x00010))
			{
				n += (int)(4);
				z >>= 4;
			}

			if ((z) >= (0x00004))
			{
				n += (int)(2);
				z >>= 2;
			}

			if ((z) >= (0x00002))
			{
				n += (int)(1);
				z >>= 1;
			}

			return (int)(n);
		}

		private static int stbi__bitcount(uint a)
		{
			a = (uint)((a & 0x55555555) + ((a >> 1) & 0x55555555));
			a = (uint)((a & 0x33333333) + ((a >> 2) & 0x33333333));
			a = (uint)((a + (a >> 4)) & 0x0f0f0f0f);
			a = (uint)(a + (a >> 8));
			a = (uint)(a + (a >> 16));
			return (int)(a & 0xff);
		}

		private static int stbi__shiftsigned(uint v, int shift, int bits)
		{
			uint* mul_table = stackalloc uint[9];
			mul_table[0] = (uint)(0);
			mul_table[1] = (uint)(0xff);
			mul_table[2] = (uint)(0x55);
			mul_table[3] = (uint)(0x49);
			mul_table[4] = (uint)(0x11);
			mul_table[5] = (uint)(0x21);
			mul_table[6] = (uint)(0x41);
			mul_table[7] = (uint)(0x81);
			mul_table[8] = (uint)(0x01);

			uint* shift_table = stackalloc uint[9];
			shift_table[0] = (uint)(0);
			shift_table[1] = (uint)(0);
			shift_table[2] = (uint)(0);
			shift_table[3] = (uint)(1);
			shift_table[4] = (uint)(0);
			shift_table[5] = (uint)(2);
			shift_table[6] = (uint)(4);
			shift_table[7] = (uint)(6);
			shift_table[8] = (uint)(0);

			if ((shift) < (0))
				v <<= -shift;
			else
				v >>= shift;
			v >>= (8 - bits);
			return (int)((int)(v * (int)mul_table[bits]) >> (int)shift_table[bits]);
		}

		private static void* stbi__bmp_parse_header(stbi__context s, stbi__bmp_data* info)
		{
			int hsz = 0;
			if ((stbi__get8(s) != 'B') || (stbi__get8(s) != 'M'))
				return ((byte*)((ulong)((stbi__err("not BMP")) != 0 ? ((byte*)null) : (null))));
			stbi__get32le(s);
			stbi__get16le(s);
			stbi__get16le(s);
			info->offset = (int)(stbi__get32le(s));
			info->hsz = (int)(hsz = (int)(stbi__get32le(s)));
			info->mr = (uint)(info->mg = (uint)(info->mb = (uint)(info->ma = (uint)(0))));
			if (((((hsz != 12) && (hsz != 40)) && (hsz != 56)) && (hsz != 108)) && (hsz != 124))
				return ((byte*)((ulong)((stbi__err("unknown BMP")) != 0 ? ((byte*)null) : (null))));
			if ((hsz) == (12))
			{
				s.img_x = (uint)(stbi__get16le(s));
				s.img_y = (uint)(stbi__get16le(s));
			}
			else
			{
				s.img_x = (uint)(stbi__get32le(s));
				s.img_y = (uint)(stbi__get32le(s));
			}

			if (stbi__get16le(s) != 1)
				return ((byte*)((ulong)((stbi__err("bad BMP")) != 0 ? ((byte*)null) : (null))));
			info->bpp = (int)(stbi__get16le(s));
			if (hsz != 12)
			{
				int compress = (int)(stbi__get32le(s));
				if (((compress) == (1)) || ((compress) == (2)))
					return ((byte*)((ulong)((stbi__err("BMP RLE")) != 0 ? ((byte*)null) : (null))));
				stbi__get32le(s);
				stbi__get32le(s);
				stbi__get32le(s);
				stbi__get32le(s);
				stbi__get32le(s);
				if (((hsz) == (40)) || ((hsz) == (56)))
				{
					if ((hsz) == (56))
					{
						stbi__get32le(s);
						stbi__get32le(s);
						stbi__get32le(s);
						stbi__get32le(s);
					}
					if (((info->bpp) == (16)) || ((info->bpp) == (32)))
					{
						if ((compress) == (0))
						{
							if ((info->bpp) == (32))
							{
								info->mr = (uint)(0xffu << 16);
								info->mg = (uint)(0xffu << 8);
								info->mb = (uint)(0xffu << 0);
								info->ma = (uint)(0xffu << 24);
								info->all_a = (uint)(0);
							}
							else
							{
								info->mr = (uint)(31u << 10);
								info->mg = (uint)(31u << 5);
								info->mb = (uint)(31u << 0);
							}
						}
						else if ((compress) == (3))
						{
							info->mr = (uint)(stbi__get32le(s));
							info->mg = (uint)(stbi__get32le(s));
							info->mb = (uint)(stbi__get32le(s));
							if (((info->mr) == (info->mg)) && ((info->mg) == (info->mb)))
							{
								return ((byte*)((ulong)((stbi__err("bad BMP")) != 0 ? ((byte*)null) : (null))));
							}
						}
						else
							return ((byte*)((ulong)((stbi__err("bad BMP")) != 0 ? ((byte*)null) : (null))));
					}
				}
				else
				{
					int i = 0;
					if ((hsz != 108) && (hsz != 124))
						return ((byte*)((ulong)((stbi__err("bad BMP")) != 0 ? ((byte*)null) : (null))));
					info->mr = (uint)(stbi__get32le(s));
					info->mg = (uint)(stbi__get32le(s));
					info->mb = (uint)(stbi__get32le(s));
					info->ma = (uint)(stbi__get32le(s));
					stbi__get32le(s);
					for (i = (int)(0); (i) < (12); ++i)
					{
						stbi__get32le(s);
					}
					if ((hsz) == (124))
					{
						stbi__get32le(s);
						stbi__get32le(s);
						stbi__get32le(s);
						stbi__get32le(s);
					}
				}
			}

			return (void*)(1);
		}

		private static void* stbi__bmp_load(stbi__context s, int* x, int* y, int* comp, int req_comp, stbi__result_info* ri)
		{
			byte* _out_;
			uint mr = (uint)(0);
			uint mg = (uint)(0);
			uint mb = (uint)(0);
			uint ma = (uint)(0);
			uint all_a = 0;
			byte* pal = stackalloc byte[256 * 4];
			int psize = (int)(0);
			int i = 0;
			int j = 0;
			int width = 0;
			int flip_vertically = 0;
			int pad = 0;
			int target = 0;
			stbi__bmp_data info = new stbi__bmp_data();
			info.all_a = (uint)(255);
			if ((stbi__bmp_parse_header(s, &info)) == (null))
				return (null);
			flip_vertically = (int)(((int)(s.img_y)) > (0) ? 1 : 0);
			s.img_y = (uint)(CRuntime.abs((int)(s.img_y)));
			mr = (uint)(info.mr);
			mg = (uint)(info.mg);
			mb = (uint)(info.mb);
			ma = (uint)(info.ma);
			all_a = (uint)(info.all_a);
			if ((info.hsz) == (12))
			{
				if ((info.bpp) < (24))
					psize = (int)((info.offset - 14 - 24) / 3);
			}
			else
			{
				if ((info.bpp) < (16))
					psize = (int)((info.offset - 14 - info.hsz) >> 2);
			}

			s.img_n = (int)((ma) != 0 ? 4 : 3);
			if (((req_comp) != 0) && ((req_comp) >= (3)))
				target = (int)(req_comp);
			else
				target = (int)(s.img_n);
			if (stbi__mad3sizes_valid((int)(target), (int)(s.img_x), (int)(s.img_y), (int)(0)) == 0)
				return ((byte*)((ulong)((stbi__err("too large")) != 0 ? ((byte*)null) : (null))));
			_out_ = (byte*)(stbi__malloc_mad3((int)(target), (int)(s.img_x), (int)(s.img_y), (int)(0)));
			if (_out_ == null)
				return ((byte*)((ulong)((stbi__err("outofmem")) != 0 ? ((byte*)null) : (null))));
			if ((info.bpp) < (16))
			{
				int z = (int)(0);
				if (((psize) == (0)) || ((psize) > (256)))
				{
					CRuntime.free(_out_);
					return ((byte*)((ulong)((stbi__err("invalid")) != 0 ? ((byte*)null) : (null))));
				}
				for (i = (int)(0); (i) < (psize); ++i)
				{
					pal[i * 4 + 2] = (byte)(stbi__get8(s));
					pal[i * 4 + 1] = (byte)(stbi__get8(s));
					pal[i * 4 + 0] = (byte)(stbi__get8(s));
					if (info.hsz != 12)
						stbi__get8(s);
					pal[i * 4 + 3] = (byte)(255);
				}
				stbi__skip(s, (int)(info.offset - 14 - info.hsz - psize * ((info.hsz) == (12) ? 3 : 4)));
				if ((info.bpp) == (1))
					width = (int)((s.img_x + 7) >> 3);
				else if ((info.bpp) == (4))
					width = (int)((s.img_x + 1) >> 1);
				else if ((info.bpp) == (8))
					width = (int)(s.img_x);
				else
				{
					CRuntime.free(_out_);
					return ((byte*)((ulong)((stbi__err("bad bpp")) != 0 ? ((byte*)null) : (null))));
				}
				pad = (int)((-width) & 3);
				if ((info.bpp) == (1))
				{
					for (j = (int)(0); (j) < ((int)(s.img_y)); ++j)
					{
						int bit_offset = (int)(7);
						int v = (int)(stbi__get8(s));
						for (i = (int)(0); (i) < ((int)(s.img_x)); ++i)
						{
							int color = (int)((v >> bit_offset) & 0x1);
							_out_[z++] = (byte)(pal[color * 4 + 0]);
							_out_[z++] = (byte)(pal[color * 4 + 1]);
							_out_[z++] = (byte)(pal[color * 4 + 2]);
							if ((target) == (4))
								_out_[z++] = (byte)(255);
							if ((i + 1) == ((int)(s.img_x)))
								break;
							if ((--bit_offset) < (0))
							{
								bit_offset = (int)(7);
								v = (int)(stbi__get8(s));
							}
						}
						stbi__skip(s, (int)(pad));
					}
				}
				else
				{
					for (j = (int)(0); (j) < ((int)(s.img_y)); ++j)
					{
						for (i = (int)(0); (i) < ((int)(s.img_x)); i += (int)(2))
						{
							int v = (int)(stbi__get8(s));
							int v2 = (int)(0);
							if ((info.bpp) == (4))
							{
								v2 = (int)(v & 15);
								v >>= 4;
							}
							_out_[z++] = (byte)(pal[v * 4 + 0]);
							_out_[z++] = (byte)(pal[v * 4 + 1]);
							_out_[z++] = (byte)(pal[v * 4 + 2]);
							if ((target) == (4))
								_out_[z++] = (byte)(255);
							if ((i + 1) == ((int)(s.img_x)))
								break;
							v = (int)(((info.bpp) == (8)) ? stbi__get8(s) : v2);
							_out_[z++] = (byte)(pal[v * 4 + 0]);
							_out_[z++] = (byte)(pal[v * 4 + 1]);
							_out_[z++] = (byte)(pal[v * 4 + 2]);
							if ((target) == (4))
								_out_[z++] = (byte)(255);
						}
						stbi__skip(s, (int)(pad));
					}
				}
			}
			else
			{
				int rshift = (int)(0);
				int gshift = (int)(0);
				int bshift = (int)(0);
				int ashift = (int)(0);
				int rcount = (int)(0);
				int gcount = (int)(0);
				int bcount = (int)(0);
				int acount = (int)(0);
				int z = (int)(0);
				int easy = (int)(0);
				stbi__skip(s, (int)(info.offset - 14 - info.hsz));
				if ((info.bpp) == (24))
					width = (int)(3 * s.img_x);
				else if ((info.bpp) == (16))
					width = (int)(2 * s.img_x);
				else
					width = (int)(0);
				pad = (int)((-width) & 3);
				if ((info.bpp) == (24))
				{
					easy = (int)(1);
				}
				else if ((info.bpp) == (32))
				{
					if (((((mb) == (0xff)) && ((mg) == (0xff00))) && ((mr) == (0x00ff0000))) && ((ma) == (0xff000000)))
						easy = (int)(2);
				}
				if (easy == 0)
				{
					if (((mr == 0) || (mg == 0)) || (mb == 0))
					{
						CRuntime.free(_out_);
						return ((byte*)((ulong)((stbi__err("bad masks")) != 0 ? ((byte*)null) : (null))));
					}
					rshift = (int)(stbi__high_bit((uint)(mr)) - 7);
					rcount = (int)(stbi__bitcount((uint)(mr)));
					gshift = (int)(stbi__high_bit((uint)(mg)) - 7);
					gcount = (int)(stbi__bitcount((uint)(mg)));
					bshift = (int)(stbi__high_bit((uint)(mb)) - 7);
					bcount = (int)(stbi__bitcount((uint)(mb)));
					ashift = (int)(stbi__high_bit((uint)(ma)) - 7);
					acount = (int)(stbi__bitcount((uint)(ma)));
				}
				for (j = (int)(0); (j) < ((int)(s.img_y)); ++j)
				{
					if ((easy) != 0)
					{
						for (i = (int)(0); (i) < ((int)(s.img_x)); ++i)
						{
							byte a = 0;
							_out_[z + 2] = (byte)(stbi__get8(s));
							_out_[z + 1] = (byte)(stbi__get8(s));
							_out_[z + 0] = (byte)(stbi__get8(s));
							z += (int)(3);
							a = (byte)((easy) == (2) ? stbi__get8(s) : 255);
							all_a |= (uint)(a);
							if ((target) == (4))
								_out_[z++] = (byte)(a);
						}
					}
					else
					{
						int bpp = (int)(info.bpp);
						for (i = (int)(0); (i) < ((int)(s.img_x)); ++i)
						{
							uint v = (uint)((bpp) == (16) ? (uint)(stbi__get16le(s)) : stbi__get32le(s));
							uint a = 0;
							_out_[z++] = ((byte)((stbi__shiftsigned((uint)(v & mr), (int)(rshift), (int)(rcount))) & 255));
							_out_[z++] = ((byte)((stbi__shiftsigned((uint)(v & mg), (int)(gshift), (int)(gcount))) & 255));
							_out_[z++] = ((byte)((stbi__shiftsigned((uint)(v & mb), (int)(bshift), (int)(bcount))) & 255));
							a = (uint)((ma) != 0 ? stbi__shiftsigned((uint)(v & ma), (int)(ashift), (int)(acount)) : 255);
							all_a |= (uint)(a);
							if ((target) == (4))
								_out_[z++] = ((byte)((a) & 255));
						}
					}
					stbi__skip(s, (int)(pad));
				}
			}

			if (((target) == (4)) && ((all_a) == (0)))
				for (i = (int)(4 * s.img_x * s.img_y - 1); (i) >= (0); i -= (int)(4))
				{
					_out_[i] = (byte)(255);
				}
			if ((flip_vertically) != 0)
			{
				byte t = 0;
				for (j = (int)(0); (j) < ((int)(s.img_y) >> 1); ++j)
				{
					byte* p1 = _out_ + j * s.img_x * target;
					byte* p2 = _out_ + (s.img_y - 1 - j) * s.img_x * target;
					for (i = (int)(0); (i) < ((int)(s.img_x) * target); ++i)
					{
						t = (byte)(p1[i]);
						p1[i] = (byte)(p2[i]);
						p2[i] = (byte)(t);
					}
				}
			}

			if (((req_comp) != 0) && (req_comp != target))
			{
				_out_ = stbi__convert_format(_out_, (int)(target), (int)(req_comp), (uint)(s.img_x), (uint)(s.img_y));
				if ((_out_) == (null))
					return _out_;
			}

			*x = (int)(s.img_x);
			*y = (int)(s.img_y);
			if ((comp) != null)
				*comp = (int)(s.img_n);
			return _out_;
		}

	}
}
