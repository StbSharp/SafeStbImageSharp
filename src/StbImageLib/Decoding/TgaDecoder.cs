﻿namespace StbImageLib.Decoding
{
	public unsafe class TgaDecoder: Decoder
	{
		public static int stbi__tga_get_comp(int bits_per_pixel, int is_grey, int* is_rgb16)
		{
			if ((is_rgb16) != null)
				*is_rgb16 = (int)(0);
			switch (bits_per_pixel)
			{
				case 8:
					return (int)(STBI_grey);
				case 15:
				case 16:
					if (((bits_per_pixel) == (16)) && ((is_grey) != 0))
						return (int)(STBI_grey_alpha);
					if ((is_rgb16) != null)
						*is_rgb16 = (int)(1);
					return (int)(STBI_rgb);
				case 24:
				case 32:
					return (int)(bits_per_pixel / 8);
				default:
					return (int)(0);
			}

		}

		public static int stbi__tga_info(stbi__context s, int* x, int* y, int* comp)
		{
			int tga_w = 0;
			int tga_h = 0;
			int tga_comp = 0;
			int tga_image_type = 0;
			int tga_bits_per_pixel = 0;
			int tga_colormap_bpp = 0;
			int sz = 0;
			int tga_colormap_type = 0;
			stbi__get8(s);
			tga_colormap_type = (int)(stbi__get8(s));
			if ((tga_colormap_type) > (1))
			{
				stbi__rewind(s);
				return (int)(0);
			}

			tga_image_type = (int)(stbi__get8(s));
			if ((tga_colormap_type) == (1))
			{
				if ((tga_image_type != 1) && (tga_image_type != 9))
				{
					stbi__rewind(s);
					return (int)(0);
				}
				stbi__skip(s, (int)(4));
				sz = (int)(stbi__get8(s));
				if (((((sz != 8) && (sz != 15)) && (sz != 16)) && (sz != 24)) && (sz != 32))
				{
					stbi__rewind(s);
					return (int)(0);
				}
				stbi__skip(s, (int)(4));
				tga_colormap_bpp = (int)(sz);
			}
			else
			{
				if ((((tga_image_type != 2) && (tga_image_type != 3)) && (tga_image_type != 10)) && (tga_image_type != 11))
				{
					stbi__rewind(s);
					return (int)(0);
				}
				stbi__skip(s, (int)(9));
				tga_colormap_bpp = (int)(0);
			}

			tga_w = (int)(stbi__get16le(s));
			if ((tga_w) < (1))
			{
				stbi__rewind(s);
				return (int)(0);
			}

			tga_h = (int)(stbi__get16le(s));
			if ((tga_h) < (1))
			{
				stbi__rewind(s);
				return (int)(0);
			}

			tga_bits_per_pixel = (int)(stbi__get8(s));
			stbi__get8(s);
			if (tga_colormap_bpp != 0)
			{
				if ((tga_bits_per_pixel != 8) && (tga_bits_per_pixel != 16))
				{
					stbi__rewind(s);
					return (int)(0);
				}
				tga_comp = (int)(stbi__tga_get_comp((int)(tga_colormap_bpp), (int)(0), (null)));
			}
			else
			{
				tga_comp = (int)(stbi__tga_get_comp((int)(tga_bits_per_pixel), (((tga_image_type) == (3))) || (((tga_image_type) == (11))) ? 1 : 0, (null)));
			}

			if (tga_comp == 0)
			{
				stbi__rewind(s);
				return (int)(0);
			}

			if ((x) != null)
				*x = (int)(tga_w);
			if ((y) != null)
				*y = (int)(tga_h);
			if ((comp) != null)
				*comp = (int)(tga_comp);
			return (int)(1);
		}

		public static int stbi__tga_test(stbi__context s)
		{
			int res = (int)(0);
			int sz = 0;
			int tga_color_type = 0;
			stbi__get8(s);
			tga_color_type = (int)(stbi__get8(s));
			if ((tga_color_type) > (1))
				goto errorEnd;
			sz = (int)(stbi__get8(s));
			if ((tga_color_type) == (1))
			{
				if ((sz != 1) && (sz != 9))
					goto errorEnd;
				stbi__skip(s, (int)(4));
				sz = (int)(stbi__get8(s));
				if (((((sz != 8) && (sz != 15)) && (sz != 16)) && (sz != 24)) && (sz != 32))
					goto errorEnd;
				stbi__skip(s, (int)(4));
			}
			else
			{
				if ((((sz != 2) && (sz != 3)) && (sz != 10)) && (sz != 11))
					goto errorEnd;
				stbi__skip(s, (int)(9));
			}

			if ((stbi__get16le(s)) < (1))
				goto errorEnd;
			if ((stbi__get16le(s)) < (1))
				goto errorEnd;
			sz = (int)(stbi__get8(s));
			if ((((tga_color_type) == (1)) && (sz != 8)) && (sz != 16))
				goto errorEnd;
			if (((((sz != 8) && (sz != 15)) && (sz != 16)) && (sz != 24)) && (sz != 32))
				goto errorEnd;
			res = (int)(1);
			errorEnd:
			;
			stbi__rewind(s);
			return (int)(res);
		}

		public static void stbi__tga_read_rgb16(stbi__context s, byte* _out_)
		{
			ushort px = (ushort)(stbi__get16le(s));
			ushort fiveBitMask = (ushort)(31);
			int r = (int)((px >> 10) & fiveBitMask);
			int g = (int)((px >> 5) & fiveBitMask);
			int b = (int)(px & fiveBitMask);
			_out_[0] = ((byte)((r * 255) / 31));
			_out_[1] = ((byte)((g * 255) / 31));
			_out_[2] = ((byte)((b * 255) / 31));
		}

		public static void* stbi__tga_load(stbi__context s, int* x, int* y, int* comp, int req_comp, stbi__result_info* ri)
		{
			int tga_offset = (int)(stbi__get8(s));
			int tga_indexed = (int)(stbi__get8(s));
			int tga_image_type = (int)(stbi__get8(s));
			int tga_is_RLE = (int)(0);
			int tga_palette_start = (int)(stbi__get16le(s));
			int tga_palette_len = (int)(stbi__get16le(s));
			int tga_palette_bits = (int)(stbi__get8(s));
			int tga_x_origin = (int)(stbi__get16le(s));
			int tga_y_origin = (int)(stbi__get16le(s));
			int tga_width = (int)(stbi__get16le(s));
			int tga_height = (int)(stbi__get16le(s));
			int tga_bits_per_pixel = (int)(stbi__get8(s));
			int tga_comp = 0;
			int tga_rgb16 = (int)(0);
			int tga_inverted = (int)(stbi__get8(s));
			byte* tga_data;
			byte* tga_palette = (null);
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
				return ((byte*)((ulong)((stbi__err("bad format")) != 0 ? ((byte*)null) : (null))));
			*x = (int)(tga_width);
			*y = (int)(tga_height);
			if ((comp) != null)
				*comp = (int)(tga_comp);
			if (stbi__mad3sizes_valid((int)(tga_width), (int)(tga_height), (int)(tga_comp), (int)(0)) == 0)
				return ((byte*)((ulong)((stbi__err("too large")) != 0 ? ((byte*)null) : (null))));
			tga_data = (byte*)(stbi__malloc_mad3((int)(tga_width), (int)(tga_height), (int)(tga_comp), (int)(0)));
			if (tga_data == null)
				return ((byte*)((ulong)((stbi__err("outofmem")) != 0 ? ((byte*)null) : (null))));
			stbi__skip(s, (int)(tga_offset));
			if (((tga_indexed == 0) && (tga_is_RLE == 0)) && (tga_rgb16 == 0))
			{
				for (i = (int)(0); (i) < (tga_height); ++i)
				{
					int row = (int)((tga_inverted) != 0 ? tga_height - i - 1 : i);
					byte* tga_row = tga_data + row * tga_width * tga_comp;
					stbi__getn(s, tga_row, (int)(tga_width * tga_comp));
				}
			}
			else
			{
				if ((tga_indexed) != 0)
				{
					stbi__skip(s, (int)(tga_palette_start));
					tga_palette = (byte*)(stbi__malloc_mad2((int)(tga_palette_len), (int)(tga_comp), (int)(0)));
					if (tga_palette == null)
					{
						CRuntime.free(tga_data);
						return ((byte*)((ulong)((stbi__err("outofmem")) != 0 ? ((byte*)null) : (null))));
					}
					if ((tga_rgb16) != 0)
					{
						byte* pal_entry = tga_palette;
						for (i = (int)(0); (i) < (tga_palette_len); ++i)
						{
							stbi__tga_read_rgb16(s, pal_entry);
							pal_entry += tga_comp;
						}
					}
					else if (stbi__getn(s, tga_palette, (int)(tga_palette_len * tga_comp)) == 0)
					{
						CRuntime.free(tga_data);
						CRuntime.free(tga_palette);
						return ((byte*)((ulong)((stbi__err("bad palette")) != 0 ? ((byte*)null) : (null))));
					}
				}
				for (i = (int)(0); (i) < (tga_width * tga_height); ++i)
				{
					if ((tga_is_RLE) != 0)
					{
						if ((RLE_count) == (0))
						{
							int RLE_cmd = (int)(stbi__get8(s));
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
							int pal_idx = (int)(((tga_bits_per_pixel) == (8)) ? stbi__get8(s) : stbi__get16le(s));
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
							stbi__tga_read_rgb16(s, raw_data);
						}
						else
						{
							for (j = (int)(0); (j) < (tga_comp); ++j)
							{
								raw_data[j] = (byte)(stbi__get8(s));
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
				if (tga_palette != (null))
				{
					CRuntime.free(tga_palette);
				}
			}

			if (((tga_comp) >= (3)) && (tga_rgb16 == 0))
			{
				byte* tga_pixel = tga_data;
				for (i = (int)(0); (i) < (tga_width * tga_height); ++i)
				{
					byte temp = (byte)(tga_pixel[0]);
					tga_pixel[0] = (byte)(tga_pixel[2]);
					tga_pixel[2] = (byte)(temp);
					tga_pixel += tga_comp;
				}
			}

			if (((req_comp) != 0) && (req_comp != tga_comp))
				tga_data = stbi__convert_format(tga_data, (int)(tga_comp), (int)(req_comp), (uint)(tga_width), (uint)(tga_height));
			tga_palette_start = (int)(tga_palette_len = (int)(tga_palette_bits = (int)(tga_x_origin = (int)(tga_y_origin = (int)(0)))));
			return tga_data;
		}
	}
}
