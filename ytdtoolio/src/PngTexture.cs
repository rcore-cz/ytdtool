#nullable enable

using System;
using System.Linq;
using System.IO;
using System.Text.Json;

using SixLabors.ImageSharp;
using RageLib.ResourceWrappers;

using RageLib.Resources.Common;
using RageLib.Resources.GTA5.PC.Textures;
using RageLib.GTA5.ResourceWrappers.PC.Textures;
using StbDxtSharp;
using StbImageSharp;

namespace ytdtoolio {
	class Meta {
		public TextureFormat Format { get; set; } = TextureFormat.D3DFMT_DXT5;

		public int MipMapLevels { get; set; } = 1;

		public void Save(string path) =>
			File.WriteAllText(path, JsonSerializer.Serialize(this));

		public static Meta Load(string path) =>
			JsonSerializer.Deserialize<Meta>(File.ReadAllText(path))
				?? throw new Exception("Parsing meta JSON returned null. :(");
	}

	class PngTexture {
		Image image;
		Meta meta = new Meta();
		private string Path { get; set; }

		public TextureFormat Format { get => meta.Format; }
		public int MipMapLevels { get => meta.MipMapLevels; }
		public int Width { get => image.Width; }
		public int Height { get => image.Height; }

		private PngTexture(Image image) {
			this.image = image;
		}

		public PngTexture(ITexture tex) {
			meta.Format = tex.Format;
			meta.MipMapLevels = tex.MipMapLevels;

			image = tex.ToImage();
		}

		public void Save(string path) {
			Console.WriteLine($"  Writing {path}:");
			Console.WriteLine($"    {image.Width}x{image.Height}px {Format}, {MipMapLevels} levels");
			image.SaveAsPng(path);
			meta.Save($"{path}.json");
		}

		static public PngTexture Load(string path) {
			Console.WriteLine($"  Loading {path}:");
			var img = new PngTexture(Image.Load(path));
			img.Path = path;

			try {
				img.meta = Meta.Load($"{path}.json");
			} catch {}

			Console.WriteLine($"    {img.Width}x{img.Height}px {img.Format}, {img.MipMapLevels} levels");

			var maxMip = (int)Math.Min(Math.Log2(img.Width), Math.Log2(img.Height))-1;
			if (img.MipMapLevels > maxMip) {
				Console.WriteLine($"    Warning! {img.MipMapLevels} Mipmap levels doesn't make sense, using {maxMip}");
				img.meta.MipMapLevels = maxMip;
			}

			img.meta.MipMapLevels = maxMip;
			return img;
		}

		public ITexture ToTexture(string name) {
			// var imgs = image.GenerateMipMaps(MipMapLevels); 
			// var bytes =	imgs
			// 	.Select(img => Format.Size(img.Width, img.Height))
			// 	.Sum();
			//
			var tex = new TextureWrapper_GTA5_pc() {
				texture = new TextureDX11() {
					Name = (string_r)name,
					Width = (ushort)Width,
					Height = (ushort)Height,
					Levels = (byte)1,
					Format = (uint)Format,
					Stride = (ushort)Format.Stride(Width),
					Data = new TextureData_GTA5_pc() {
						FullData = new byte[Width*Height],
					}
				}
			};

			using (var stream = File.OpenRead(Path))
			{
				ImageResult imageFromMemory = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
				var compressDxt5 = StbDxt.CompressDxt5(imageFromMemory.Width, imageFromMemory.Height, imageFromMemory.Data);

				tex.SetTextureData(compressDxt5);
			}

			tex.UpdateClass();
			return tex;
		}
	}
}