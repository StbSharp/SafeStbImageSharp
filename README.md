# StbImageLib
[![NuGet](https://img.shields.io/nuget/v/StbImageLib.svg)](https://www.nuget.org/packages/StbImageLib/) [![Build status](https://ci.appveyor.com/api/projects/status/w6os3e5th6p529la?svg=true)](https://ci.appveyor.com/project/RomanShapiro/stbimagelib)

StbImageLib is **safe** C# library that can load images in JPG, PNG, BMP, TGA, PSD and GIF formats.

It is based on stb_image.h 2.22 code.


# Usage
Following code loads image from stream and converts it to 32-bit RGBA:
```c#
	ImageResult image;
	using (var stream = File.OpenRead(path))
	{
		image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
	}
```

If you are writing MonoGame application and would like to convert that data to the Texture2D. It could be done following way:
```c#
Texture2D texture = new Texture2D(GraphicsDevice, image.Width, image.Height, false, SurfaceFormat.Color);
texture.SetData(image.Data);
```

Or if you are writing WinForms app and would like StbSharp resulting bytes to be converted to the Bitmap. The sample code is:
```c#
byte[] data = image.Data;
// Convert rgba to bgra
for (int i = 0; i < x*y; ++i)
{
	byte r = data[i*4];
	byte g = data[i*4 + 1];
	byte b = data[i*4 + 2];
	byte a = data[i*4 + 3];


	data[i*4] = b;
	data[i*4 + 1] = g;
	data[i*4 + 2] = r;
	data[i*4 + 3] = a;
}

// Create Bitmap
Bitmap bmp = new Bitmap(_loadedImage.Width, _loadedImage.Height, PixelFormat.Format32bppArgb);
BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, _loadedImage.Width, _loadedImage.Height), ImageLockMode.WriteOnly,
	bmp.PixelFormat);

Marshal.Copy(data, 0, bmpData.Scan0, bmpData.Stride*bmp.Height);
bmp.UnlockBits(bmpData);
```

# License
Public Domain

# Credits
* [stb](https://github.com/nothings/stb)
