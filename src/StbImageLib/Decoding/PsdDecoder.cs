using System.IO;

namespace StbImageLib.Decoding
{
	public unsafe class PsdDecoder: Decoder
	{
		private PsdDecoder(Stream stream): base(stream)
		{
		}

		private static int stbi__psd_test(stbi__context s)
		{
			int r = (((stbi__get32be()) == (0x38425053))) ? 1 : 0;
			stbi__rewind(s);
			return (int)(r);
		}

		private int stbi__psd_decode_rle(byte* p, int pixelCount)
		{
			int count = 0;
			int nleft = 0;
			int len = 0;
			count = (int)(0);
			while ((nleft = (int)(pixelCount - count)) > (0))
			{
				len = (int)(stbi__get8());
				if ((len) == (128))
				{
				}
				else if ((len) < (128))
				{
					len++;
					if ((len) > (nleft))
						return (int)(0);
					count += (int)(len);
					while ((len) != 0)
					{
						*p = (byte)(stbi__get8());
						p += 4;
						len--;
					}
				}
				else if ((len) > (128))
				{
					byte val = 0;
					len = (int)(257 - len);
					if ((len) > (nleft))
						return (int)(0);
					val = (byte)(stbi__get8());
					count += (int)(len);
					while ((len) != 0)
					{
						*p = (byte)(val);
						p += 4;
						len--;
					}
				}
			}
			return (int)(1);
		}

		private void* stbi__psd_load(int* x, int* y, int* comp, int req_comp, stbi__result_info* ri, int bpc)
		{
			int pixelCount = 0;
			int channelCount = 0;
			int compression = 0;
			int channel = 0;
			int i = 0;
			int bitdepth = 0;
			int w = 0;
			int h = 0;
			byte* _out_;
			if (stbi__get32be() != 0x38425053)
				stbi__err("not PSD");
			if (stbi__get16be() != 1)
				stbi__err("wrong version");
			stbi__skip((int)(6));
			channelCount = (int)(stbi__get16be());
			if (((channelCount) < (0)) || ((channelCount) > (16)))
				stbi__err("wrong channel count");
			h = (int)(stbi__get32be());
			w = (int)(stbi__get32be());
			bitdepth = (int)(stbi__get16be());
			if ((bitdepth != 8) && (bitdepth != 16))
				stbi__err("unsupported bit depth");
			if (stbi__get16be() != 3)
				stbi__err("wrong color format");
			stbi__skip((int)(stbi__get32be()));
			stbi__skip((int)(stbi__get32be()));
			stbi__skip((int)(stbi__get32be()));
			compression = (int)(stbi__get16be());
			if ((compression) > (1))
				stbi__err("bad compression");
			if (Utility.stbi__mad3sizes_valid((int)(4), (int)(w), (int)(h), (int)(0)) == 0)
				stbi__err("too large");
			if (((compression == 0) && ((bitdepth) == (16))) && ((bpc) == (16)))
			{
				_out_ = (byte*)(Utility.stbi__malloc_mad3((int)(8), (int)(w), (int)(h), (int)(0)));
				ri->bits_per_channel = (int)(16);
			}
			else
				_out_ = (byte*)(Utility.stbi__malloc((ulong)(4 * w * h)));
			pixelCount = (int)(w * h);
			if ((compression) != 0)
			{
				stbi__skip((int)(h * channelCount * 2));
				for (channel = (int)(0); (channel) < (4); channel++)
				{
					byte* p;
					p = _out_ + channel;
					if ((channel) >= (channelCount))
					{
						for (i = (int)(0); (i) < (pixelCount); i++, p += 4)
						{
							*p = (byte)((channel) == (3) ? 255 : 0);
						}
					}
					else
					{
						if (stbi__psd_decode_rle(p, (int)(pixelCount)) == 0)
						{
							CRuntime.free(_out_);
							stbi__err("corrupt");
						}
					}
				}
			}
			else
			{
				for (channel = (int)(0); (channel) < (4); channel++)
				{
					if ((channel) >= (channelCount))
					{
						if (((bitdepth) == (16)) && ((bpc) == (16)))
						{
							ushort* q = ((ushort*)(_out_)) + channel;
							ushort val = (ushort)((channel) == (3) ? 65535 : 0);
							for (i = (int)(0); (i) < (pixelCount); i++, q += 4)
							{
								*q = (ushort)(val);
							}
						}
						else
						{
							byte* p = _out_ + channel;
							byte val = (byte)((channel) == (3) ? 255 : 0);
							for (i = (int)(0); (i) < (pixelCount); i++, p += 4)
							{
								*p = (byte)(val);
							}
						}
					}
					else
					{
						if ((ri->bits_per_channel) == (16))
						{
							ushort* q = ((ushort*)(_out_)) + channel;
							for (i = (int)(0); (i) < (pixelCount); i++, q += 4)
							{
								*q = ((ushort)(stbi__get16be()));
							}
						}
						else
						{
							byte* p = _out_ + channel;
							if ((bitdepth) == (16))
							{
								for (i = (int)(0); (i) < (pixelCount); i++, p += 4)
								{
									*p = ((byte)(stbi__get16be() >> 8));
								}
							}
							else
							{
								for (i = (int)(0); (i) < (pixelCount); i++, p += 4)
								{
									*p = (byte)(stbi__get8());
								}
							}
						}
					}
				}
			}

			if ((channelCount) >= (4))
			{
				if ((ri->bits_per_channel) == (16))
				{
					for (i = (int)(0); (i) < (w * h); ++i)
					{
						ushort* pixel = (ushort*)(_out_) + 4 * i;
						if ((pixel[3] != 0) && (pixel[3] != 65535))
						{
							float a = (float)(pixel[3] / 65535.0f);
							float ra = (float)(1.0f / a);
							float inv_a = (float)(65535.0f * (1 - ra));
							pixel[0] = ((ushort)(pixel[0] * ra + inv_a));
							pixel[1] = ((ushort)(pixel[1] * ra + inv_a));
							pixel[2] = ((ushort)(pixel[2] * ra + inv_a));
						}
					}
				}
				else
				{
					for (i = (int)(0); (i) < (w * h); ++i)
					{
						byte* pixel = _out_ + 4 * i;
						if ((pixel[3] != 0) && (pixel[3] != 255))
						{
							float a = (float)(pixel[3] / 255.0f);
							float ra = (float)(1.0f / a);
							float inv_a = (float)(255.0f * (1 - ra));
							pixel[0] = ((byte)(pixel[0] * ra + inv_a));
							pixel[1] = ((byte)(pixel[1] * ra + inv_a));
							pixel[2] = ((byte)(pixel[2] * ra + inv_a));
						}
					}
				}
			}

			if (((req_comp) != 0) && (req_comp != 4))
			{
				if ((ri->bits_per_channel) == (16))
					_out_ = (byte*)(stbi__convert_format16((ushort*)(_out_), (int)(4), (int)(req_comp), (uint)(w), (uint)(h)));
				else
					_out_ = stbi__convert_format(_out_, (int)(4), (int)(req_comp), (uint)(w), (uint)(h));
				if ((_out_) == (null))
					return _out_;
			}

			if ((comp) != null)
				*comp = (int)(4);
			*y = (int)(h);
			*x = (int)(w);
			return _out_;
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
	}
}
