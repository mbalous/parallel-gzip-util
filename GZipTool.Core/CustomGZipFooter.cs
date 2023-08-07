using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace GzipTool.Core;

public sealed class CustomGZipFooter
{
	[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
	public sealed record CompressedChunkInfo
	{
		public long OriginalStart { get; set; }
		public long CompressedStart { get; set; }

		public long OriginalLength { get; set; }
		public long CompressedLength { get; set; }

		internal static readonly long ChunkInfoSize = sizeof(long) * 4; // number of fields 

		private string GetDebuggerDisplay()
		{
			return
				$"{nameof(OriginalStart)}= {OriginalStart}; " +
				$"{nameof(CompressedStart)}= {CompressedStart}; " +
				$"{nameof(OriginalLength)}= {OriginalLength}; " +
				$"{nameof(CompressedLength)}= {CompressedLength};";
		}
	}

	public long OriginalFileSize { get; private set; } = 0;

	public IReadOnlyList<CompressedChunkInfo> Chunks => _chunks;
	private readonly List<CompressedChunkInfo> _chunks = new List<CompressedChunkInfo>();
		
	private static readonly Encoding FooterEncoding = Encoding.ASCII;

	public CustomGZipFooter()
	{
	}

	public long GetFooterSize()
	{
		return _chunks.Count * CompressedChunkInfo.ChunkInfoSize +
		       sizeof(long) + // original size
		       sizeof(long);  // footer size
	}

	public void AddChunkInfo(long originalStart, long compressedStart, long originalLength, long compressedLength)
	{
		// TODO: checks args

		_chunks.Add(new CompressedChunkInfo()
		{
			OriginalStart = originalStart,
			CompressedStart = compressedStart,
			OriginalLength = originalLength,
			CompressedLength = compressedLength
		});

		OriginalFileSize += originalLength;
	}

	public void WriteToStream(Stream stream)
	{
		using (BinaryWriter bw = new BinaryWriter(stream, FooterEncoding, true))
		{
			// chunks are ordered in footer the way they're ordered in the compressed file
			foreach (var blockInfo in Chunks.OrderBy(x => x.CompressedStart))
			{
				bw.Write(blockInfo.OriginalStart);
				bw.Write(blockInfo.CompressedStart);
				bw.Write(blockInfo.OriginalLength);
				bw.Write(blockInfo.CompressedLength);
			}

			bw.Write(OriginalFileSize);
			bw.Write(GetFooterSize());
		}
	}

	public static CustomGZipFooter FromStream(Stream stream)
	{
		if (!stream.CanSeek)
			throw new ArgumentException("Given stream must allow seeking.", nameof(stream));

		CustomGZipFooter footer = new CustomGZipFooter();

		// seek to read the footer size
		stream.Seek(-sizeof(ulong), SeekOrigin.End);

		using (BinaryReader br = new BinaryReader(stream, FooterEncoding, true))
		{
			var footerSize = br.ReadInt64();

			br.BaseStream.Seek(-footerSize, SeekOrigin.End);

			var chunkInfosCount = GetNumberOfChunksFromByteSize(footerSize);

			for (int i = 0; i < chunkInfosCount; i++)
			{
				long originalStart = br.ReadInt64();
				long compressedStart = br.ReadInt64();
				long originalLength = br.ReadInt64();
				long compressedLength = br.ReadInt64();

				footer.AddChunkInfo(originalStart, compressedStart, originalLength, compressedLength);
			}

			long originalSize = br.ReadInt64();
			footer.OriginalFileSize = originalSize;
		}

		return footer;
	}

	private static long GetNumberOfChunksFromByteSize(long size)
	{
		return (size - sizeof(long)) / (CompressedChunkInfo.ChunkInfoSize);
	}

	public override bool Equals(object obj)
	{
		if (obj is CustomGZipFooter otherFooter)
		{

			// TODO: the collections are considered to be equal even when not in the same order
			return OriginalFileSize == otherFooter.OriginalFileSize &&
			       Enumerable.SequenceEqual(Chunks, otherFooter.Chunks);
		}

		return false;
	}
}