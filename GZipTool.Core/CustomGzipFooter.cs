using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace GzipTool.Core
{
	public class CustomGzipFooter
	{
		[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
		public class CompressedChunkInfo
		{
			public long OriginalStart { get; set; }
			public long CompressedStart { get; set; }

			public long OriginalLength { get; set; }
			public long CompressedLength { get; set; }

			public override bool Equals(object obj)
			{
				if (obj is CompressedChunkInfo other)
				{
					return
						this.OriginalStart == other.OriginalStart &&
						this.CompressedStart == other.CompressedStart &&
						this.OriginalLength == other.OriginalLength &&
						this.CompressedLength == other.CompressedLength;
				}

				return base.Equals(obj);
			}

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

		public long OriginalSize { get; private set; } = 0;

		public IReadOnlyCollection<CompressedChunkInfo> Chunks => this._chunks;

		private readonly List<CompressedChunkInfo> _chunks = new List<CompressedChunkInfo>();

		public long GetFooterSize()
		{
			return this._chunks.Count * CompressedChunkInfo.ChunkInfoSize +
				sizeof(long) + // original size
				sizeof(long);  // footer size
		}

		public void AddChunkInfo(long originalStart, long compressedStart, long originalLength, long compressedLength)
		{
			// TODO: checks args

			this._chunks.Add(new CompressedChunkInfo()
			{
				OriginalStart = originalStart,
				CompressedStart = compressedStart,
				OriginalLength = originalLength,
				CompressedLength = compressedLength
			});

			this.OriginalSize += originalLength;
		}

		public void WriteToStream(Stream stream)
		{
			using (BinaryWriter bw = new BinaryWriter(stream, Encoding.ASCII, true))
			{
				// chunks are ordered in footer the way they're ordered in the compressed file
				foreach (var blockInfo in this.Chunks.OrderBy(x => x.CompressedStart))
				{
					bw.Write(blockInfo.OriginalStart);
					bw.Write(blockInfo.CompressedStart);
					bw.Write(blockInfo.OriginalLength);
					bw.Write(blockInfo.CompressedLength);
				}

				bw.Write(this.OriginalSize);
				bw.Write(GetFooterSize());
			}
		}

		public static CustomGzipFooter FromStream(Stream stream)
		{
			if (!stream.CanSeek)
				throw new ArgumentException("Given stream must allow seeking.", nameof(stream));

			CustomGzipFooter footer = new CustomGzipFooter();

			// seek to read the footer size
			stream.Seek(-sizeof(ulong), SeekOrigin.End);

			using (BinaryReader br = new BinaryReader(stream, Encoding.ASCII, true))
			{
				var footerSize = br.ReadInt64();

				br.BaseStream.Seek(-footerSize, SeekOrigin.End);

				var chunkInfosCount = GetNumberOfChunksFromByteSize(footerSize);

				for (int i = 0; i < chunkInfosCount; i++)
				{
					long originalStart, compressedStart, originalLength, compressedLength;

					originalStart = br.ReadInt64();
					compressedStart = br.ReadInt64();
					originalLength = br.ReadInt64();
					compressedLength = br.ReadInt64();

					footer.AddChunkInfo(originalStart, compressedStart, originalLength, compressedLength);
				}

				long originalSize = br.ReadInt64();
				footer.OriginalSize = originalSize;
			}

			return footer;
		}

		private static long GetNumberOfChunksFromByteSize(long size)
		{
			return (size - sizeof(long)) / (CompressedChunkInfo.ChunkInfoSize);
		}

		public override bool Equals(object obj)
		{
			if (obj is CustomGzipFooter otherFooter)
			{

				// TODO: the collections are considered to be equal even when not in the same order
				return this.OriginalSize == otherFooter.OriginalSize &&
					Enumerable.SequenceEqual(this.Chunks, otherFooter.Chunks);
			}

			return false;
		}
	}
}

