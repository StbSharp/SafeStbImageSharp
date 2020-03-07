using SafeStbImageSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace StbSharp.WinForms.Test
{
	public partial class Form1 : Form
	{
		private string _fileName;
		private ImageResult _loadedImage;

		public Form1()
		{
			InitializeComponent();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			try
			{
				using (var dlg = new OpenFileDialog())
				{
					dlg.Filter =
						"PNG Files (*.png)|*.png|JPEG Files (*.jpg)|*.jpg|BMP Files (*.bmp)|*.bmp|PSD Files (*.psd)|*.psd|TGA Files (*.tga)|*.tga|GIF Files (*.gif)|*.gif|All Files (*.*)|*.*";
					if (dlg.ShowDialog() != DialogResult.OK)
					{
						return;
					}

					_fileName = dlg.FileName;

					var bytes = File.ReadAllBytes(_fileName);

					using (var stream = File.OpenRead(_fileName))
					{
						_loadedImage = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
					}
					SetImage();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error", ex.Message);
			}
		}

		private void SetImage()
		{
			// Convert to bgra
			var data = new byte[_loadedImage.Data.Length];
			Array.Copy(_loadedImage.Data, data, data.Length);

			for (var i = 0; i < _loadedImage.Width*_loadedImage.Height; ++i)
			{
				var r = data[i*4];
				var g = data[i*4 + 1];
				var b = data[i*4 + 2];
				var a = data[i*4 + 3];


				data[i*4] = b;
				data[i*4 + 1] = g;
				data[i*4 + 2] = r;
				data[i*4 + 3] = a;
			}

			// Convert to Bitmap
			var bmp = new Bitmap(_loadedImage.Width, _loadedImage.Height, PixelFormat.Format32bppArgb);
			var bmpData = bmp.LockBits(new Rectangle(0, 0, _loadedImage.Width, _loadedImage.Height), ImageLockMode.WriteOnly,
				bmp.PixelFormat);

			Marshal.Copy(data, 0, bmpData.Scan0, bmpData.Stride*bmp.Height);
			bmp.UnlockBits(bmpData);

			pictureBox1.Image = bmp;
			_numericWidth.Value = _loadedImage.Width;
			_numericHeight.Value = _loadedImage.Height;
		}
	}
}