﻿using System;
using System.IO;
using System.Threading;

namespace GzipTool.Core;

public class ParallelFileCompress : ParallelFileIO
{
	private readonly CustomGZipFooter _footer = new CustomGZipFooter();

	public ParallelFileCompress(string inputFileName, string outputFileName) : base(inputFileName, outputFileName)
	{
	}

	public ParallelFileCompress(Stream input, Stream output) : base(input, output)
	{
	}

	public override void Run(int threadCount, CancellationToken cancellationToken = default)
	{
		var threads = GetIdealThreadCount(_readerStream.Length);

		int chunkSize = 8_000_000;
		if (threads > 1)
		{
			long idealChunkSize = _readerStream.Length / threads;
			chunkSize = (int)Math.Min(MAX_CHUNK_SIZE, idealChunkSize);
		}

		Run(chunkSize, threads, cancellationToken);
	}

	public void Run(int chunkSize, int threads, CancellationToken cancellationToken)
	{
		using (var threadFinisher = new CountdownEvent(1))
		{
			for (int i = 0; i < threads; i++)
			{
				var thread = new Thread((object o) =>
				{
					Thread.CurrentThread.Name = $"Compress ThreadWorker: {(int)o}";
					StartWorker(chunkSize, cancellationToken);
					threadFinisher.Signal();
				});
				threadFinisher.AddCount();
				thread.Start(i);
				if (cancellationToken.IsCancellationRequested)
					return;
			}

			threadFinisher.Signal();
			threadFinisher.Wait();
		}

		_footer.WriteToStream(_writerStream);
		_writerStream.Flush();
	}

	private void StartWorker(int chunkSize, CancellationToken cancellationToken)
	{
		byte[] buffer = new byte[chunkSize];
		int bytesRead;

		while ((bytesRead = ReadChunk(buffer, chunkSize, out long chunkStartPosition)) > 0)
		{
			if (cancellationToken.IsCancellationRequested)
				return;
			var compressed = GZipWrapper.Compress(buffer, 0, bytesRead);
			lock (_writerLocker)
			{
				_footer.AddChunkInfo(
					originalStart: chunkStartPosition,
					compressedStart: _writerStream.Position,
					originalLength: bytesRead,
					compressedLength: compressed.Length);

				_writerStream.Write(compressed, 0, compressed.Length);
			}
		}
	}

	private int ReadChunk(byte[] buffer, int length, out long chunkStartPosition)
	{
		lock (_readerLocker)
		{
			chunkStartPosition = _readerStream.Position;
			int bytesRead = _readerStream.Read(buffer, 0, length);
			return bytesRead;
		}
	}
}