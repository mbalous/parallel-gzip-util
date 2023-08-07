using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace GzipTool.Core;

public class ParallelFileDecompress : ParallelFileIO
{
	public ParallelFileDecompress(string inputFileName, string outputFileName) : base(inputFileName, outputFileName)
	{
	}

	public ParallelFileDecompress(Stream input, Stream output) : base(input, output)
	{
		if (!output.CanSeek)
			throw new ArgumentException("Output stream must support seeking.", nameof(output));
	}

	private ConcurrentBag<CustomGZipFooter.CompressedChunkInfo> _chunkInfos;

	public override void Run(CancellationToken cancellationToken = default)
	{
		Run(GetIdealThreadCount(_readerStream.Length), cancellationToken);
	}

	public void Run(int threads, CancellationToken cancellationToken)
	{
		var footer = CustomGZipFooter.FromStream(_readerStream);
		_chunkInfos = new ConcurrentBag<CustomGZipFooter.CompressedChunkInfo>(footer.Chunks);

		List<Thread> threadList = new List<Thread>(threads);
		using (CountdownEvent threadFinisher = new CountdownEvent(1))
		{
			for (int i = 0; i < threads; i++)
			{
				var thread = new Thread((object param) =>
				{
					Thread.CurrentThread.Name = $"Decompress ThreadWorker: {(int)param}";
					StartWorker();
					threadFinisher.Signal();
				});

				threadFinisher.AddCount();
				thread.Start(i);
			}

			threadFinisher.Signal();
			threadFinisher.Wait();
		}
	}

	private void StartWorker()
	{
		while (_chunkInfos.TryTake(out CustomGZipFooter.CompressedChunkInfo chunkInfo))
		{
			byte[] buffer = new byte[chunkInfo.CompressedLength];
			int bytesRead;
			lock (_readerLocker)
			{
				_readerStream.Seek(chunkInfo.CompressedStart, SeekOrigin.Begin);
				bytesRead = _readerStream.Read(buffer, 0, (int)chunkInfo.CompressedLength);
			}

			var decompressed = GZipWrapper.Decompress(buffer, 0, bytesRead);

			lock (_writerLocker)
			{
				_writerStream.Seek(chunkInfo.OriginalStart, SeekOrigin.Begin);
				// decompresseed.Length should be the same as chunkInfo.OriginalLength
				_writerStream.Write(decompressed, 0, decompressed.Length);
			}
		}
	}
}