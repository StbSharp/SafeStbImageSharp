# SafeStbImageSharp
[![NuGet](https://img.shields.io/nuget/v/SafeStbImageSharp.svg)](https://www.nuget.org/packages/SafeStbImageSharp/)
[![Build status]![Build & Publish](https://github.com/StbSharp/SafeStbImageSharp/workflows/Build%20&%20Publish/badge.svg)
[![Chat](https://img.shields.io/discord/628186029488340992.svg)](https://discord.gg/ZeHxhCY)

SafeStbImageSharp is safe and refactored version of [StbImageSharp](https://github.com/StbSharp/StbImageSharp).

# Adding Reference
There are two ways of referencing SafeStbImageSharp in the project:
1. Through nuget: https://www.nuget.org/packages/SafeStbImageSharp/
2. As submodule:
    
    a. `git submodule add https://github.com/StbSharp/SafeStbImageSharp.git`
    
    b. Now there are two options:
       
      * Add SafeStbImageSharp/src/SafeStbImageSharp/SafeStbImageSharp.csproj to the solution
       
      * Include *.cs from SafeStbImageSharp/src/SafeStbImageSharp directly in the project. In this case, it might make sense to add STBSHARP_INTERNAL build compilation symbol to the project, so SafeStbImageSharp classes would become internal.

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

# Reliability & Performance
There is special app to measure reliability & performance of SafeStbImageSharp in comparison to the original stb_image.h: https://github.com/StbSharp/SafeStbImageSharp/tree/master/tests/StbImageSharp.Testing

It goes through every image file in the specified folder and tries to load it 10 times with SafeStbImageSharp, then 10 times with C++/CLI wrapper over the original stb_image.h(Stb.Native). Then it compares whether the results are byte-wise similar and also calculates loading times. Also it sums up and reports loading times for each method.

Moreover SixLabor ImageSharp is included in the testing too.

I've used it over following set of images: https://github.com/StbSharp/TestImages

The byte-wise comprarison results are similar for StbImageSharp and Stb.Native.

And performance comparison results are(times are total loading times):
```
10 -- SafeStbImageSharp - jpg: 16139 ms, tga: 4075 ms, bmp: 370 ms, psd: 2 ms, png: 73274 ms, Total: 93860 ms
10 -- Stb.Native - jpg: 6437 ms, tga: 2140 ms, bmp: 132 ms, psd: 0 ms, png: 52758 ms, Total: 61467 ms
10 -- ImageSharp - jpg: 101309 ms, bmp: 63 ms, png: 44211 ms, Total: 145583 ms
10 -- Total files processed - jpg: 170, tga: 41, bmp: 7, psd: 1, png: 564, Total: 783
10 -- StbImageSharp/Stb.Native matches/processed - 783/787
```

# License
Public Domain

# Credits
* [stb](https://github.com/nothings/stb)
