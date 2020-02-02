using StbImageLib.Utility;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace StbImageLib.Decoding
{
	public class BmpDecoder: Decoder
	{
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

		private static uint[] mul_table = { 0, 0xff, 0x55, 0x49, 0x11, 0x21, 0x41, 0x81, 0x01 };
		private static uint[] shift_table = { 0, 0, 0, 1, 0, 2, 4, 6, 0 };

		private BmpDecoder(Stream stream): base(stream)
		{
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
			if ((shift) < (0))
				v <<= -shift;
			else
				v >>= shift;
			v >>= (8 - bits);
			return (int)((int)(v * (int)mul_table[bits]) >> (int)shift_table[bits]);
		}

		private void stbi__bmp_parse_header(ref stbi__bmp_data info)
		{
			int hsz = 0;
			if ((stbi__get8() != 'B') || (stbi__get8() != 'M'))
				stbi__err("not BMP");
			stbi__get32le();
			stbi__get16le();
			stbi__get16le();
			info.offset = (int)(stbi__get32le());
			info.hsz = (int)(hsz = (int)(stbi__get32le()));
			info.mr = (uint)(info.mg = (uint)(info.mb = (uint)(info.ma = (uint)(0))));
			if (((((hsz != 12) && (hsz != 40)) && (hsz != 56)) && (hsz != 108)) && (hsz != 124))
				stbi__err("unknown BMP");
			if ((hsz) == (12))
			{
				img_x = (stbi__get16le());
				img_y = (stbi__get16le());
			}
			else
			{
				img_x = (int)(stbi__get32le());
				img_y = (int)(stbi__get32le());
			}

			if (stbi__get16le() != 1)
				stbi__err("bad BMP");
			info.bpp = (int)(stbi__get16le());
			if (hsz != 12)
			{
				int compress = (int)(stbi__get32le());
				if (((compress) == (1)) || ((compress) == (2)))
					stbi__err("BMP RLE");
				stbi__get32le();
				stbi__get32le();
				stbi__get32le();
				stbi__get32le();
				stbi__get32le();
				if (((hsz) == (40)) || ((hsz) == (56)))
				{
					if ((hsz) == (56))
					{
						stbi__get32le();
						stbi__get32le();
						stbi__get32le();
						stbi__get32le();
					}
					if (((info.bpp) == (16)) || ((info.bpp) == (32)))
					{
						if ((compress) == (0))
						{
							if ((info.bpp) == (32))
							{
								info.mr = (uint)(0xffu << 16);
								info.mg = (uint)(0xffu << 8);
								info.mb = (uint)(0xffu << 0);
								info.ma = (uint)(0xffu << 24);
								info.all_a = (uint)(0);
							}
							else
							{
								info.mr = (uint)(31u << 10);
								info.mg = (uint)(31u << 5);
								info.mb = (uint)(31u << 0);
							}
						}
						else if ((compress) == (3))
						{
							info.mr = (uint)(stbi__get32le());
							info.mg = (uint)(stbi__get32le());
							info.mb = (uint)(stbi__get32le());
							if (((info.mr) == (info.mg)) && ((info.mg) == (info.mb)))
							{
								stbi__err("bad BMP");
							}
						}
						else
							stbi__err("bad BMP");
					}
				}
				else
				{
					int i = 0;
					if ((hsz != 108) && (hsz != 124))
						stbi__err("bad BMP");
					info.mr = (uint)(stbi__get32le());
					info.mg = (uint)(stbi__get32le());
					info.mb = (uint)(stbi__get32le());
					info.ma = (uint)(stbi__get32le());
					stbi__get32le();
					for (i = (int)(0); (i) < (12); ++i)
					{
						stbi__get32le();
					}
					if ((hsz) == (124))
					{
						stbi__get32le();
						stbi__get32le();
						stbi__get32le();
						stbi__get32le();
					}
				}
			}
		}

		private ImageResult InternalDecode(ColorComponents? requiredComponents)
		{
			byte[] _out_;
			uint mr = (uint)(0);
			uint mg = (uint)(0);
			uint mb = (uint)(0);
			uint ma = (uint)(0);
			uint all_a = 0;
			byte[] pal = new byte[256 * 4];
			int psize = (int)(0);
			int i = 0;
			int j = 0;
			int width = 0;
			int flip_vertically = 0;
			int pad = 0;
			int target = 0;
			stbi__bmp_data info = new stbi__bmp_data();
			info.all_a = (uint)(255);
			stbi__bmp_parse_header(ref info);
			flip_vertically = (int)(((int)(img_y)) > (0) ? 1 : 0);
			img_y = Math.Abs((int)(img_y));
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

			img_n = (int)((ma) != 0 ? 4 : 3);
			if (requiredComponents != null && (int)requiredComponents.Value >= (3))
				target = (int)requiredComponents.Value;
			else
				target = (int)(img_n);
			if (Memory.stbi__mad3sizes_valid((int)(target), (int)(img_x), (int)(img_y), (int)(0)) == 0)
				stbi__err("too large");
			_out_ = new byte[target * img_x * img_y];
			if ((info.bpp) < (16))
			{
				int z = (int)(0);
				if (((psize) == (0)) || ((psize) > (256)))
				{
					stbi__err("invalid");
				}
				for (i = (int)(0); (i) < (psize); ++i)
				{
					pal[i * 4 + 2] = (byte)(stbi__get8());
					pal[i * 4 + 1] = (byte)(stbi__get8());
					pal[i * 4 + 0] = (byte)(stbi__get8());
					if (info.hsz != 12)
						stbi__get8();
					pal[i * 4 + 3] = (byte)(255);
				}
				stbi__skip((int)(info.offset - 14 - info.hsz - psize * ((info.hsz) == (12) ? 3 : 4)));
				if ((info.bpp) == (1))
					width = (int)((img_x + 7) >> 3);
				else if ((info.bpp) == (4))
					width = (int)((img_x + 1) >> 1);
				else if ((info.bpp) == (8))
					width = (int)(img_x);
				else
				{
					stbi__err("bad bpp");
				}
				pad = (int)((-width) & 3);
				if ((info.bpp) == (1))
				{
					for (j = (int)(0); (j) < ((int)(img_y)); ++j)
					{
						int bit_offset = (int)(7);
						int v = (int)(stbi__get8());
						for (i = (int)(0); (i) < ((int)(img_x)); ++i)
						{
							int color = (int)((v >> bit_offset) & 0x1);
							_out_[z++] = (byte)(pal[color * 4 + 0]);
							_out_[z++] = (byte)(pal[color * 4 + 1]);
							_out_[z++] = (byte)(pal[color * 4 + 2]);
							if ((target) == (4))
								_out_[z++] = (byte)(255);
							if ((i + 1) == ((int)(img_x)))
								break;
							if ((--bit_offset) < (0))
							{
								bit_offset = (int)(7);
								v = (int)(stbi__get8());
							}
						}
						stbi__skip((int)(pad));
					}
				}
				else
				{
					for (j = (int)(0); (j) < ((int)(img_y)); ++j)
					{
						for (i = (int)(0); (i) < ((int)(img_x)); i += (int)(2))
						{
							int v = (int)(stbi__get8());
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
							if ((i + 1) == ((int)(img_x)))
								break;
							v = (int)(((info.bpp) == (8)) ? stbi__get8() : v2);
							_out_[z++] = (byte)(pal[v * 4 + 0]);
							_out_[z++] = (byte)(pal[v * 4 + 1]);
							_out_[z++] = (byte)(pal[v * 4 + 2]);
							if ((target) == (4))
								_out_[z++] = (byte)(255);
						}
						stbi__skip((int)(pad));
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
				stbi__skip((int)(info.offset - 14 - info.hsz));
				if ((info.bpp) == (24))
					width = (int)(3 * img_x);
				else if ((info.bpp) == (16))
					width = (int)(2 * img_x);
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
						stbi__err("bad masks");
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
				for (j = (int)(0); (j) < ((int)(img_y)); ++j)
				{
					if ((easy) != 0)
					{
						for (i = (int)(0); (i) < ((int)(img_x)); ++i)
						{
							byte a = 0;
							_out_[z + 2] = (byte)(stbi__get8());
							_out_[z + 1] = (byte)(stbi__get8());
							_out_[z + 0] = (byte)(stbi__get8());
							z += (int)(3);
							a = (byte)((easy) == (2) ? stbi__get8() : 255);
							all_a |= (uint)(a);
							if ((target) == (4))
								_out_[z++] = (byte)(a);
						}
					}
					else
					{
						int bpp = (int)(info.bpp);
						for (i = (int)(0); (i) < ((int)(img_x)); ++i)
						{
							uint v = (uint)((bpp) == (16) ? (uint)(stbi__get16le()) : stbi__get32le());
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
					stbi__skip((int)(pad));
				}
			}

			if (((target) == (4)) && ((all_a) == (0)))
				for (i = (int)(4 * img_x * img_y - 1); (i) >= (0); i -= (int)(4))
				{
					_out_[i] = (byte)(255);
				}
			if ((flip_vertically) != 0)
			{
				byte t = 0;
				var ptr = new FakePtr<byte>(_out_);
					for (j = (int)(0); (j) < ((int)(img_y) >> 1); ++j)
					{
						FakePtr<byte> p1 = ptr + j * img_x * target;
						FakePtr<byte> p2 = ptr + (img_y - 1 - j) * img_x * target;
						for (i = (int)(0); (i) < ((int)(img_x) * target); ++i)
						{
							t = (byte)(p1[i]);
							p1[i] = (byte)(p2[i]);
							p2[i] = (byte)(t);
						}
					}
			}

			if (requiredComponents != null && (int)requiredComponents.Value != target)
			{
				_out_ = Conversion.stbi__convert_format(_out_, (int)(target), (int)(requiredComponents.Value), (uint)(img_x), (uint)(img_y));
			}

			return new ImageResult
			{
				Width = (int)img_x,
				Height = (int)img_y,
				SourceComponents = (ColorComponents)img_n,
				ColorComponents = requiredComponents != null ? requiredComponents.Value : (ColorComponents)img_n,
				BitsPerChannel = 8,
				Data = _out_
			};
		}

		private static bool TestInternal(Stream stream)
		{
			int sz = 0;
			if (stream.ReadByte() != 'B')
				return false;
			if (stream.ReadByte() != 'M')
				return false;

			stream.stbi__get32le();
			stream.stbi__get16le();
			stream.stbi__get16le();
			stream.stbi__get32le();
			sz = (int)(stream.stbi__get32le());
			bool r = ((sz) == (12)) || ((sz) == (40)) || ((sz) == (56)) || ((sz) == (108)) || ((sz) == (124));
			return r;
		}

		public static bool Test(Stream stream)
		{
			var r = TestInternal(stream);
			stream.Rewind();
			return r;
		}

		public static ImageInfo? Info(Stream stream)
		{
			stbi__bmp_data info = new stbi__bmp_data
			{
				all_a = (uint)(255)
			};

			var decoder = new BmpDecoder(stream);
			try
			{
				decoder.stbi__bmp_parse_header(ref info);
			}
			catch (Exception)
			{
				return null;
			}
			finally
			{
				stream.Rewind();
			}

			return new ImageInfo
			{
				Width = (int)decoder.img_x,
				Height = (int)decoder.img_y,
				ColorComponents = info.ma != 0 ? ColorComponents.RedGreenBlueAlpha : ColorComponents.RedGreenBlue,
				BitsPerChannel = 8
			};
		}

		public static ImageResult Decode(Stream stream, ColorComponents? requiredComponents = null)
		{
			var decoder = new BmpDecoder(stream);
			return decoder.InternalDecode(requiredComponents);
		}
	}
}
