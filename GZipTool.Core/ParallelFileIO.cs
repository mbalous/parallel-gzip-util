using System;
using System.IO;
using System.Threading;

namespace GzipTool.Core
{
	public abstract class ParallelFileIO
	{
		protected const long MAX_CHUNK_SIZE = 8_000_000; // 8 MB
		protected const long MAX_MEMORY_BLOCK_SIZE = 512_000_000; // 512 MB

		protected readonly Stream _readerStream;
		protected readonly Stream _writerStream;

		protected readonly object _readerLocker = new object();
		protected readonly object _writerLocker = new object();		

		protected int GetIdealThreadCount(long fileSize = -1)
		{
			if (fileSize < 500_000) // overhead is larger than the speedup
				return 1;
			return Environment.ProcessorCount;
		}

		public ParallelFileIO(string inputFileName, string outputFileName)
		{
			if (string.IsNullOrWhiteSpace(inputFileName))
				throw new ArgumentException($"'{nameof(inputFileName)}' cannot be null or whitespace", nameof(inputFileName));

			if (string.IsNullOrWhiteSpace(outputFileName))
				throw new ArgumentException($"'{nameof(outputFileName)}' cannot be null or whitespace", nameof(outputFileName));

			this._readerStream = new FileStream(inputFileName, FileMode.Open, FileAccess.Read);
			this._writerStream = new FileStream(outputFileName, FileMode.Create, FileAccess.Write);
		}

		public ParallelFileIO(Stream input, Stream output)
		{
			this._readerStream = input;
			this._writerStream = output;
		}

		public abstract void Run(CancellationToken cancellationToken = default);
	}
}
