namespace StbImageLib.Decoding
{
	internal unsafe static class Utility
	{
		public static void* stbi__malloc(int size)
		{
			return CRuntime.malloc((ulong)size);
		}

		public static void* stbi__malloc(ulong size)
		{
			return stbi__malloc((int)size);
		}

		public static int stbi__addsizes_valid(int a, int b)
		{
			if ((b) < (0))
				return (int)(0);
			return (a <= 2147483647 - b) ? 1 : 0;
		}

		public static int stbi__mul2sizes_valid(int a, int b)
		{
			if (((a) < (0)) || ((b) < (0)))
				return (int)(0);
			if ((b) == (0))
				return (int)(1);
			return (a <= 2147483647 / b) ? 1 : 0;
		}

		public static int stbi__mad2sizes_valid(int a, int b, int add)
		{
			return (int)(((stbi__mul2sizes_valid((int)(a), (int)(b))) != 0) && ((stbi__addsizes_valid((int)(a * b), (int)(add))) != 0) ? 1 : 0);
		}

		public static int stbi__mad3sizes_valid(int a, int b, int c, int add)
		{
			return (int)((((stbi__mul2sizes_valid((int)(a), (int)(b))) != 0) && ((stbi__mul2sizes_valid((int)(a * b), (int)(c))) != 0)) && ((stbi__addsizes_valid((int)(a * b * c), (int)(add))) != 0) ? 1 : 0);
		}

		public static void* stbi__malloc_mad2(int a, int b, int add)
		{
			if (stbi__mad2sizes_valid((int)(a), (int)(b), (int)(add)) == 0)
				return (null);
			return stbi__malloc((ulong)(a * b + add));
		}

		public static void* stbi__malloc_mad3(int a, int b, int c, int add)
		{
			if (stbi__mad3sizes_valid((int)(a), (int)(b), (int)(c), (int)(add)) == 0)
				return (null);
			return stbi__malloc((ulong)(a * b * c + add));
		}

		public static int stbi__bitreverse16(int n)
		{
			n = (int)(((n & 0xAAAA) >> 1) | ((n & 0x5555) << 1));
			n = (int)(((n & 0xCCCC) >> 2) | ((n & 0x3333) << 2));
			n = (int)(((n & 0xF0F0) >> 4) | ((n & 0x0F0F) << 4));
			n = (int)(((n & 0xFF00) >> 8) | ((n & 0x00FF) << 8));
			return (int)(n);
		}

		public static int stbi__bit_reverse(int v, int bits)
		{
			return (int)(stbi__bitreverse16((int)(v)) >> (16 - bits));
		}
	}
}
