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
        public long OriginalStart { get; }
        public long CompressedStart { get; }

        public long OriginalLength { get; }
        public long CompressedLength { get; }

        internal static readonly long ChunkInfoSize = sizeof(long) * 4; // number of fields 

        public CompressedChunkInfo(long originalStart, long compressedStart, long originalLength, long compressedLength)
        {
            OriginalStart = originalStart;
            CompressedStart = compressedStart;
            OriginalLength = originalLength;
            CompressedLength = compressedLength;
        }

        private string GetDebuggerDisplay()
        {
            return
                $"{nameof(OriginalStart)}= {OriginalStart}; " +
                $"{nameof(CompressedStart)}= {CompressedStart}; " +
                $"{nameof(OriginalLength)}= {OriginalLength}; " +
                $"{nameof(CompressedLength)}= {CompressedLength};";
        }

        #region Equality members

        public bool Equals(CompressedChunkInfo? other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return OriginalStart == other.OriginalStart &&
                   CompressedStart == other.CompressedStart &&
                   OriginalLength == other.OriginalLength &&
                   CompressedLength == other.CompressedLength;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(OriginalStart, CompressedStart, OriginalLength, CompressedLength);
        }

        #endregion
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
               sizeof(long); // footer size
    }

    public void AddChunkInfo(long originalStart, long compressedStart, long originalLength, long compressedLength)
    {
        // TODO: checks args
        _chunks.Add(new CompressedChunkInfo(originalStart, compressedStart, originalLength, compressedLength));
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

    #region Equality members

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is CustomGZipFooter other && Equals(other);
    }

    private bool Equals(CustomGZipFooter other)
    {
        return OriginalFileSize == other.OriginalFileSize && _chunks.SequenceEqual(other._chunks);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (_chunks.GetHashCode() * 397);
        }
    }

    #endregion
}