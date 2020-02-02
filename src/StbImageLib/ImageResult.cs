namespace StbImageLib
{
#if !STBSHARP_INTERNAL
	public
#else
	internal
#endif
	unsafe class ImageResult
	{
		public int Width { get; set; }
		public int Height { get; set; }
		public ColorComponents ColorComponents { get; set; }

		/// <summary>
		/// Either 8 or 16
		/// </summary>
		public int BitsPerChannel { get; set; }
		public byte[] Data { get; set; }
	}
}