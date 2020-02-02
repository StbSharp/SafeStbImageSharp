namespace StbImageLib
{
	unsafe partial class StbImage
	{
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
	}
}
