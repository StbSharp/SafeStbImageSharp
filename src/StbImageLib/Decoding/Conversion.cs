namespace StbImageLib.Decoding
{
	internal unsafe static class Conversion
	{
		public static byte stbi__compute_y(int r, int g, int b)
		{
			return (byte)(((r * 77) + (g * 150) + (29 * b)) >> 8);
		}

		public static ushort stbi__compute_y_16(int r, int g, int b)
		{
			return (ushort)(((r * 77) + (g * 150) + (29 * b)) >> 8);
		}

		public static ushort* stbi__convert_format16(ushort* data, int img_n, int req_comp, uint x, uint y)
		{
			int i = 0;
			int j = 0;
			ushort* good;
			if ((req_comp) == (img_n))
				return data;
			good = (ushort*)(Utility.stbi__malloc((ulong)(req_comp * x * y * 2)));
			for (j = (int)(0); (j) < ((int)(y)); ++j)
			{
				ushort* src = data + j * x * img_n;
				ushort* dest = good + j * x * req_comp;
				switch (((img_n) * 8 + (req_comp)))
				{
					case ((1) * 8 + (2)):
						for (i = (int)(x - 1); (i) >= (0); --i, src += 1, dest += 2)
						{
							dest[0] = (ushort)(src[0]);
							dest[1] = (ushort)(0xffff);
						}
						break;
					case ((1) * 8 + (3)):
						for (i = (int)(x - 1); (i) >= (0); --i, src += 1, dest += 3)
						{
							dest[0] = (ushort)(dest[1] = (ushort)(dest[2] = (ushort)(src[0])));
						}
						break;
					case ((1) * 8 + (4)):
						for (i = (int)(x - 1); (i) >= (0); --i, src += 1, dest += 4)
						{
							dest[0] = (ushort)(dest[1] = (ushort)(dest[2] = (ushort)(src[0])));
							dest[3] = (ushort)(0xffff);
						}
						break;
					case ((2) * 8 + (1)):
						for (i = (int)(x - 1); (i) >= (0); --i, src += 2, dest += 1)
						{
							dest[0] = (ushort)(src[0]);
						}
						break;
					case ((2) * 8 + (3)):
						for (i = (int)(x - 1); (i) >= (0); --i, src += 2, dest += 3)
						{
							dest[0] = (ushort)(dest[1] = (ushort)(dest[2] = (ushort)(src[0])));
						}
						break;
					case ((2) * 8 + (4)):
						for (i = (int)(x - 1); (i) >= (0); --i, src += 2, dest += 4)
						{
							dest[0] = (ushort)(dest[1] = (ushort)(dest[2] = (ushort)(src[0])));
							dest[3] = (ushort)(src[1]);
						}
						break;
					case ((3) * 8 + (4)):
						for (i = (int)(x - 1); (i) >= (0); --i, src += 3, dest += 4)
						{
							dest[0] = (ushort)(src[0]);
							dest[1] = (ushort)(src[1]);
							dest[2] = (ushort)(src[2]);
							dest[3] = (ushort)(0xffff);
						}
						break;
					case ((3) * 8 + (1)):
						for (i = (int)(x - 1); (i) >= (0); --i, src += 3, dest += 1)
						{
							dest[0] = (ushort)(stbi__compute_y_16((int)(src[0]), (int)(src[1]), (int)(src[2])));
						}
						break;
					case ((3) * 8 + (2)):
						for (i = (int)(x - 1); (i) >= (0); --i, src += 3, dest += 2)
						{
							dest[0] = (ushort)(stbi__compute_y_16((int)(src[0]), (int)(src[1]), (int)(src[2])));
							dest[1] = (ushort)(0xffff);
						}
						break;
					case ((4) * 8 + (1)):
						for (i = (int)(x - 1); (i) >= (0); --i, src += 4, dest += 1)
						{
							dest[0] = (ushort)(stbi__compute_y_16((int)(src[0]), (int)(src[1]), (int)(src[2])));
						}
						break;
					case ((4) * 8 + (2)):
						for (i = (int)(x - 1); (i) >= (0); --i, src += 4, dest += 2)
						{
							dest[0] = (ushort)(stbi__compute_y_16((int)(src[0]), (int)(src[1]), (int)(src[2])));
							dest[1] = (ushort)(src[3]);
						}
						break;
					case ((4) * 8 + (3)):
						for (i = (int)(x - 1); (i) >= (0); --i, src += 4, dest += 3)
						{
							dest[0] = (ushort)(src[0]);
							dest[1] = (ushort)(src[1]);
							dest[2] = (ushort)(src[2]);
						}
						break;
					default:
						Decoder.stbi__err("0");
						break;
				}
			}
			CRuntime.free(data);
			return good;
		}

		public static byte* stbi__convert_format(byte* data, int img_n, int req_comp, uint x, uint y)
		{
			int i = 0;
			int j = 0;
			byte* good;
			if ((req_comp) == (img_n))
				return data;
			good = (byte*)(Utility.stbi__malloc_mad3((int)(req_comp), (int)(x), (int)(y), (int)(0)));
			for (j = (int)(0); (j) < ((int)(y)); ++j)
			{
				byte* src = data + j * x * img_n;
				byte* dest = good + j * x * req_comp;
				switch (((img_n) * 8 + (req_comp)))
				{
					case ((1) * 8 + (2)):
						for (i = (int)(x - 1); (i) >= (0); --i, src += 1, dest += 2)
						{
							dest[0] = (byte)(src[0]);
							dest[1] = (byte)(255);
						}
						break;
					case ((1) * 8 + (3)):
						for (i = (int)(x - 1); (i) >= (0); --i, src += 1, dest += 3)
						{
							dest[0] = (byte)(dest[1] = (byte)(dest[2] = (byte)(src[0])));
						}
						break;
					case ((1) * 8 + (4)):
						for (i = (int)(x - 1); (i) >= (0); --i, src += 1, dest += 4)
						{
							dest[0] = (byte)(dest[1] = (byte)(dest[2] = (byte)(src[0])));
							dest[3] = (byte)(255);
						}
						break;
					case ((2) * 8 + (1)):
						for (i = (int)(x - 1); (i) >= (0); --i, src += 2, dest += 1)
						{
							dest[0] = (byte)(src[0]);
						}
						break;
					case ((2) * 8 + (3)):
						for (i = (int)(x - 1); (i) >= (0); --i, src += 2, dest += 3)
						{
							dest[0] = (byte)(dest[1] = (byte)(dest[2] = (byte)(src[0])));
						}
						break;
					case ((2) * 8 + (4)):
						for (i = (int)(x - 1); (i) >= (0); --i, src += 2, dest += 4)
						{
							dest[0] = (byte)(dest[1] = (byte)(dest[2] = (byte)(src[0])));
							dest[3] = (byte)(src[1]);
						}
						break;
					case ((3) * 8 + (4)):
						for (i = (int)(x - 1); (i) >= (0); --i, src += 3, dest += 4)
						{
							dest[0] = (byte)(src[0]);
							dest[1] = (byte)(src[1]);
							dest[2] = (byte)(src[2]);
							dest[3] = (byte)(255);
						}
						break;
					case ((3) * 8 + (1)):
						for (i = (int)(x - 1); (i) >= (0); --i, src += 3, dest += 1)
						{
							dest[0] = (byte)(stbi__compute_y((int)(src[0]), (int)(src[1]), (int)(src[2])));
						}
						break;
					case ((3) * 8 + (2)):
						for (i = (int)(x - 1); (i) >= (0); --i, src += 3, dest += 2)
						{
							dest[0] = (byte)(stbi__compute_y((int)(src[0]), (int)(src[1]), (int)(src[2])));
							dest[1] = (byte)(255);
						}
						break;
					case ((4) * 8 + (1)):
						for (i = (int)(x - 1); (i) >= (0); --i, src += 4, dest += 1)
						{
							dest[0] = (byte)(stbi__compute_y((int)(src[0]), (int)(src[1]), (int)(src[2])));
						}
						break;
					case ((4) * 8 + (2)):
						for (i = (int)(x - 1); (i) >= (0); --i, src += 4, dest += 2)
						{
							dest[0] = (byte)(stbi__compute_y((int)(src[0]), (int)(src[1]), (int)(src[2])));
							dest[1] = (byte)(src[3]);
						}
						break;
					case ((4) * 8 + (3)):
						for (i = (int)(x - 1); (i) >= (0); --i, src += 4, dest += 3)
						{
							dest[0] = (byte)(src[0]);
							dest[1] = (byte)(src[1]);
							dest[2] = (byte)(src[2]);
						}
						break;
					default:
						Decoder.stbi__err("0");
						break;
				}
			}
			CRuntime.free(data);
			return good;
		}
	}
}
