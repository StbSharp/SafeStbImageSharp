using StbImageLib.Utility;
using System.IO;

namespace StbImageLib.Decoding
{
	public unsafe class TgaDecoder : Decoder
	{
		private TgaDecoder(Stream stream) : base(stream)
		{
		}

		private static int stbi__tga_get_comp(int bits_per_pixel, int is_grey, int* is_rgb16)
		{
			if ((is_rgb16) != null)
				*is_rgb16 = (int)(0);
			switch (bits_per_pixel)
			{
				case 8:
					return (int)(1);
				case 15:
				case 16:
					if (((bits_per_pixel) == (16)) && ((is_grey) != 0))
						return (int)(2);
					if ((is_rgb16) != null)
						*is_rgb16 = (int)(1);
					return (int)(3);
				case 24:
				case 32:
					return (int)(bits_per_pixel / 8);
				default:
					return (int)(0);
			}
		}

		private void stbi__tga_read_rgb16(byte* _out_)
		{
			ushort px = (ushort)(stbi__get16le());
			ushort fiveBitMask = (ushort)(31);
			int r = (int)((px >> 10) & fiveBitMask);
			int g = (int)((px >> 5) & fiveBitMask);
			int b = (int)(px & fiveBitMask);
			_out_[0] = ((byte)((r * 255) / 31));
			_out_[1] = ((byte)((g * 255) / 31));
			_out_[2] = ((byte)((b * 255) / 31));
		}

		private ImageResult InternalDecode(ColorComponents? requiredComponents)
		{
			int tga_offset = (int)(stbi__get8());
			int tga_indexed = (int)(stbi__get8());
			int tga_image_type = (int)(stbi__get8());
			int tga_is_RLE = (int)(0);
			int tga_palette_start = (int)(stbi__get16le());
			int tga_palette_len = (int)(stbi__get16le());
			int tga_palette_bits = (int)(stbi__get8());
			int tga_x_origin = (int)(stbi__get16le());
			int tga_y_origin = (int)(stbi__get16le());
			int tga_width = (int)(stbi__get16le());
			int tga_height = (int)(stbi__get16le());
			int tga_bits_per_pixel = (int)(stbi__get8());
			int tga_comp = 0;
			int tga_rgb16 = (int)(0);
			int tga_inverted = (int)(stbi__get8());
			byte[] tga_data;
			byte[] tga_palette = (null);
			int i = 0;
			int j = 0;
			byte* raw_data = stackalloc byte[4];
			raw_data[0] = (byte)(0);

			int RLE_count = (int)(0);
			int RLE_repeating = (int)(0);
			int read_next_pixel = (int)(1);
			if ((tga_image_type) >= (8))
			{
				tga_image_type -= (int)(8);
				tga_is_RLE = (int)(1);
			}

			tga_inverted = (int)(1 - ((tga_inverted >> 5) & 1));
			if ((tga_indexed) != 0)
				tga_comp = (int)(stbi__tga_get_comp((int)(tga_palette_bits), (int)(0), &tga_rgb16));
			else
				tga_comp = (int)(stbi__tga_get_comp((int)(tga_bits_per_pixel), (tga_image_type) == (3) ? 1 : 0, &tga_rgb16));
			if (tga_comp == 0)
				stbi__err("bad format");

			if (Memory.stbi__mad3sizes_valid((int)(tga_width), (int)(tga_height), (int)(tga_comp), (int)(0)) == 0)
				stbi__err("too large");
			tga_data = new byte[tga_width * tga_height * tga_comp];
			stbi__skip((int)(tga_offset));
			if (((tga_indexed == 0) && (tga_is_RLE == 0)) && (tga_rgb16 == 0))
			{
				for (i = (int)(0); (i) < (tga_height); ++i)
				{
					int row = (int)((tga_inverted) != 0 ? tga_height - i - 1 : i);
					stbi__getn(tga_data, row * tga_width * tga_comp, (int)(tga_width * tga_comp));
				}
			}
			else
			{
				if ((tga_indexed) != 0)
				{
					stbi__skip((int)(tga_palette_start));
					tga_palette = new byte[tga_palette_len * tga_comp];
					if ((tga_rgb16) != 0)
					{
						fixed (byte* pal_entry2 = &tga_palette[0])
						{
							var pal_entry = pal_entry2;
							for (i = (int)(0); (i) < (tga_palette_len); ++i)
							{
								stbi__tga_read_rgb16(pal_entry);
								pal_entry += tga_comp;
							}
						}
					}
					else if (!stbi__getn(tga_palette, 0, (int)(tga_palette_len * tga_comp)))
					{
						stbi__err("bad palette");
					}
				}
				for (i = (int)(0); (i) < (tga_width * tga_height); ++i)
				{
					if ((tga_is_RLE) != 0)
					{
						if ((RLE_count) == (0))
						{
							int RLE_cmd = (int)(stbi__get8());
							RLE_count = (int)(1 + (RLE_cmd & 127));
							RLE_repeating = (int)(RLE_cmd >> 7);
							read_next_pixel = (int)(1);
						}
						else if (RLE_repeating == 0)
						{
							read_next_pixel = (int)(1);
						}
					}
					else
					{
						read_next_pixel = (int)(1);
					}
					if ((read_next_pixel) != 0)
					{
						if ((tga_indexed) != 0)
						{
							int pal_idx = (int)(((tga_bits_per_pixel) == (8)) ? stbi__get8() : stbi__get16le());
							if ((pal_idx) >= (tga_palette_len))
							{
								pal_idx = (int)(0);
							}
							pal_idx *= (int)(tga_comp);
							for (j = (int)(0); (j) < (tga_comp); ++j)
							{
								raw_data[j] = (byte)(tga_palette[pal_idx + j]);
							}
						}
						else if ((tga_rgb16) != 0)
						{
							stbi__tga_read_rgb16(raw_data);
						}
						else
						{
							for (j = (int)(0); (j) < (tga_comp); ++j)
							{
								raw_data[j] = (byte)(stbi__get8());
							}
						}
						read_next_pixel = (int)(0);
					}
					for (j = (int)(0); (j) < (tga_comp); ++j)
					{
						tga_data[i * tga_comp + j] = (byte)(raw_data[j]);
					}
					--RLE_count;
				}
				if ((tga_inverted) != 0)
				{
					for (j = (int)(0); (j * 2) < (tga_height); ++j)
					{
						int index1 = (int)(j * tga_width * tga_comp);
						int index2 = (int)((tga_height - 1 - j) * tga_width * tga_comp);
						for (i = (int)(tga_width * tga_comp); (i) > (0); --i)
						{
							byte temp = (byte)(tga_data[index1]);
							tga_data[index1] = (byte)(tga_data[index2]);
							tga_data[index2] = (byte)(temp);
							++index1;
							++index2;
						}
					}
				}
			}

			if (((tga_comp) >= (3)) && (tga_rgb16 == 0))
			{
				fixed (byte* tga_pixel2 = tga_data)
				{
					var tga_pixel = tga_pixel2;
					for (i = (int)(0); (i) < (tga_width * tga_height); ++i)
					{
						byte temp = (byte)(tga_pixel[0]);
						tga_pixel[0] = (byte)(tga_pixel[2]);
						tga_pixel[2] = (byte)(temp);
						tga_pixel += tga_comp;
					}
				}
			}

			var req_comp = requiredComponents.ToReqComp();
			if (((req_comp) != 0) && (req_comp != tga_comp))
				tga_data = Conversion.stbi__convert_format(tga_data, (int)(tga_comp), (int)(req_comp), (uint)(tga_width), (uint)(tga_height));
			tga_palette_start = (int)(tga_palette_len = (int)(tga_palette_bits = (int)(tga_x_origin = (int)(tga_y_origin = (int)(0)))));

			return new ImageResult
			{
				Width = tga_width,
				Height = tga_height,
				ColorComponents = requiredComponents != null ? requiredComponents.Value : (ColorComponents)tga_comp,
				BitsPerChannel = 8,
				Data = tga_data
			};
		}

		public static bool Test(Stream stream)
		{
			try
			{
				stream.stbi__get8();
				var tga_color_type = (int)(stream.stbi__get8());
				if ((tga_color_type) > (1))
					return false;
				var sz = (int)(stream.stbi__get8());
				if ((tga_color_type) == (1))
				{
					if ((sz != 1) && (sz != 9))
						return false;
					stream.stbi__skip((int)(4));
					sz = (int)(stream.stbi__get8());
					if (((((sz != 8) && (sz != 15)) && (sz != 16)) && (sz != 24)) && (sz != 32))
						return false;
					stream.stbi__skip((int)(4));
				}
				else
				{
					if ((((sz != 2) && (sz != 3)) && (sz != 10)) && (sz != 11))
						return false;
					stream.stbi__skip((int)(9));
				}

				if ((stream.stbi__get16le()) < (1))
					return false;
				if ((stream.stbi__get16le()) < (1))
					return false;
				sz = (int)(stream.stbi__get8());
				if ((((tga_color_type) == (1)) && (sz != 8)) && (sz != 16))
					return false;
				if (((((sz != 8) && (sz != 15)) && (sz != 16)) && (sz != 24)) && (sz != 32))
					return false;

				return true;
			}
			finally
			{
				stream.Rewind();
			}
		}

		public static ImageInfo? Info(Stream stream)
		{
			try
			{
				int tga_w = 0;
				int tga_h = 0;
				int tga_comp = 0;
				int tga_image_type = 0;
				int tga_bits_per_pixel = 0;
				int tga_colormap_bpp = 0;
				int sz = 0;
				int tga_colormap_type = 0;
				stream.stbi__get8();
				tga_colormap_type = (int)(stream.stbi__get8());
				if ((tga_colormap_type) > (1))
				{
					return null;
				}

				tga_image_type = (int)(stream.stbi__get8());
				if ((tga_colormap_type) == (1))
				{
					if ((tga_image_type != 1) && (tga_image_type != 9))
					{
						return null;
					}
					stream.stbi__skip((int)(4));
					sz = (int)(stream.stbi__get8());
					if (((((sz != 8) && (sz != 15)) && (sz != 16)) && (sz != 24)) && (sz != 32))
					{
						return null;
					}
					stream.stbi__skip((int)(4));
					tga_colormap_bpp = (int)(sz);
				}
				else
				{
					if ((((tga_image_type != 2) && (tga_image_type != 3)) && (tga_image_type != 10)) && (tga_image_type != 11))
					{
						return null;
					}
					stream.stbi__skip((int)(9));
					tga_colormap_bpp = (int)(0);
				}

				tga_w = (int)(stream.stbi__get16le());
				if ((tga_w) < (1))
				{
					return null;
				}

				tga_h = (int)(stream.stbi__get16le());
				if ((tga_h) < (1))
				{
					return null;
				}

				tga_bits_per_pixel = (int)(stream.stbi__get8());
				stream.stbi__get8();
				if (tga_colormap_bpp != 0)
				{
					if ((tga_bits_per_pixel != 8) && (tga_bits_per_pixel != 16))
					{
						return null;
					}
					tga_comp = (int)(stbi__tga_get_comp((int)(tga_colormap_bpp), (int)(0), (null)));
				}
				else
				{
					tga_comp = (int)(stbi__tga_get_comp((int)(tga_bits_per_pixel), (((tga_image_type) == (3))) || (((tga_image_type) == (11))) ? 1 : 0, (null)));
				}

				if (tga_comp == 0)
				{
					return null;
				}

				return new ImageInfo
				{
					Width = tga_w,
					Height = tga_h,
					ColorComponents = (ColorComponents)tga_comp,
					BitsPerChannel = tga_bits_per_pixel
				};
			}
			finally
			{
				stream.Rewind();
			}
		}

		public static ImageResult Decode(Stream stream, ColorComponents? requiredComponents = null)
		{
			var decoder = new TgaDecoder(stream);
			return decoder.InternalDecode(requiredComponents);
		}
	}
}