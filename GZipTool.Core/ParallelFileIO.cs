using System;
using System.IO;
using System.Threading;

namespace GzipTool.Core;

public abstract class ParallelFileIO
{
	protected const long MAX_CHUNK_SIZE = 8_000_000; // 8 MB
	protected const long MAX_MEMORY_BLOCK_SIZE = 512_000_000; // 512 MB

	protected readonly Stream _readerStream;
	protected readonly Stream _writerStream;

	protected readonly object _readerLocker = new object();
	protected readonly object _writerLocker = new object();		

	protected int GetIdealThreadCount(long fileSize)
	{
		if (fileSize < 500_000) // 5 kB, overhead is larger than the speedup
			return 1;
		return Environment.ProcessorCount;
	}

	public ParallelFileIO(string inputFileName, string outputFileName)
	{
		if (string.IsNullOrWhiteSpace(inputFileName))
			throw new ArgumentException($"'{nameof(inputFileName)}' cannot be null or whitespace", nameof(inputFileName));

		if (string.IsNullOrWhiteSpace(outputFileName))
			throw new ArgumentException($"'{nameof(outputFileName)}' cannot be null or whitespace", nameof(outputFileName));

		_readerStream = new FileStream(inputFileName, FileMode.Open, FileAccess.Read);
		_writerStream = new FileStream(outputFileName, FileMode.Create, FileAccess.Write);
	}

	public ParallelFileIO(Stream input, Stream output)
	{
		_readerStream = input;
		_writerStream = output;
	}

	public void Run(CancellationToken cancellationToken)
	{
		var threadCount = GetIdealThreadCount(_readerStream.Length);
		Run(threadCount, cancellationToken);
	}
	
	public abstract void Run(int threadCount, CancellationToken cancellationToken);
}