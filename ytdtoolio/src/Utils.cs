using System;
using System.Runtime.InteropServices;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;

using static RageLib.ResourceWrappers.TextureFormat;
using RageLib.ResourceWrappers;

namespace ytdtoolio {
	class FuckDX {
		[DllImport("FuckDX", CallingConvention=CallingConvention.Cdecl)]
		public static extern bool encode([MarshalAs(42)] byte[] src, TextureFormat fmt, UInt32 level, int w, int h, [MarshalAs(42)] byte[] dst);

		[DllImport("FuckDX", CallingConvention=CallingConvention.Cdecl)]
		public static extern bool decode([MarshalAs(42)] byte[] src, TextureFormat fmt, int w, int h, [MarshalAs(42)] byte[] dst);
	}

	static class TextureFormatExt {
		public static int Stride(this TextureFormat fmt, int width) {
			switch (fmt) {
				case D3DFMT_A8R8G8B8: return 4 * width;
				case D3DFMT_L8:       return 1 * width;
				case D3DFMT_A8:       return 1 * width;
				case D3DFMT_A1R5G5B5: return 2 * width;
				case D3DFMT_A8B8G8R8: return 4 * width;
				// Compressed formats
				case D3DFMT_DXT1: return width/4*8 /4;
				//case D3DFMT_DXT3: ;
				case D3DFMT_DXT5: return width / 4 * 16 / 4;
				case D3DFMT_ATI1: return width / 4 * 8  / 4;
				case D3DFMT_ATI2: return width / 4 * 16 / 4;
				case D3DFMT_BC7:  return width / 4 * 16 / 4;
				default: throw new NotImplementedException();
			}
		}

		public static int Size(this TextureFormat fmt, int width, int height) {
			return fmt.Stride(width) * height;
		}
	}

	static class ITextureExt {
		public static Image ToImage(this ITexture tex) {
			var d = tex.GetTextureData();
			int w = tex.Width, h = tex.Height;

			switch (tex.Format) {
				case D3DFMT_A8R8G8B8: return Image.LoadPixelData<Bgra32>(d, w, h);
				case D3DFMT_L8:       return Image.LoadPixelData<L8>(d, w, h);
				case D3DFMT_A8:       return Image.LoadPixelData<A8>(d, w, h);
				case D3DFMT_A1R5G5B5: return Image.LoadPixelData<Bgra5551>(d, w, h);
				case D3DFMT_A8B8G8R8: return Image.LoadPixelData<Rgba32>(d, w, h);
			}
			
			var uncompressed = new byte[D3DFMT_A8B8G8R8.Size(w, h)];
			if (!FuckDX.decode(d, tex.Format, w, h, uncompressed)) {
				throw new NotSupportedException(
					$"Failed to decode {tex.Name}: I guess {tex.Format} is not supported :( Mention @Kng if you care"
				);
			}

			return Image.LoadPixelData<Rgba32>(uncompressed, w, h);
		}
	}
}