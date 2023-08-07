using System.IO;
using System.IO.Compression;

namespace GzipTool.Core;

public static class GZipWrapper
{
	#region Compress

	public static byte[] Compress(byte[] data, int start = 0)
	{
		return Compress(data, start, data.Length);
	}

	public static byte[] Compress(byte[] data, int start, int length)
	{
		using (var inStream = new MemoryStream(data, start, length, false))
		{
			using (var outStream = new MemoryStream())
			{
				Compress(inStream, outStream);
				outStream.Seek(0, SeekOrigin.Begin);
				return outStream.ToArray();
			}
		}
	}

	public static void Compress(Stream inStream, Stream outStream)
	{
		using (var gzip = new GZipStream(outStream, CompressionMode.Compress, true))
			inStream.CopyTo(gzip);
	}

	#endregion

	#region Decompress

	public static byte[] Decompress(byte[] data, int start = 0)
	{
		return Decompress(data, start, data.Length);
	}

	public static byte[] Decompress(byte[] data, int start, int length)
	{
		using (var inStream = new MemoryStream(data, start, length, false))
		{
			using (var outStream = new MemoryStream())
			{
				Decompress(inStream, outStream);
				outStream.Seek(0, SeekOrigin.Begin);
				return outStream.ToArray();
			}
		}
	}

	public static void Decompress(Stream inStream, Stream outStream)
	{
		using (var gzip = new GZipStream(inStream, CompressionMode.Decompress, true))
			gzip.CopyTo(outStream);
	}

	#endregion
}