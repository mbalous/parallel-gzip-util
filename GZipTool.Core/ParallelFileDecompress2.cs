using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace GzipTool.Core
{
	public class ParallelFileDecompress2 : ParallelFileIO
	{
		public ParallelFileDecompress2(string inputFileName, string outputFileName) : base(inputFileName, outputFileName)
		{
		}

		public ParallelFileDecompress2(Stream input, Stream output) : base(input, output)
		{
			if (!output.CanSeek)
				throw new ArgumentException("Output stream must support seeking.", nameof(output));
		}

		public override void Run(CancellationToken cancellationToken = default)
		{
			Run(GetIdealThreadCount(this._readerStream.Length), cancellationToken);
		}

		public void Run(int threads, CancellationToken cancellationToken)
		{
			var footer = CustomGzipFooter.FromStream(this._readerStream);

			int chunkIndex = 0;
			long currentBlockOffset;

			this._readerStream.Seek(0, SeekOrigin.Begin);
			while (chunkIndex < footer.Chunks.Count)
			{
				if (cancellationToken.IsCancellationRequested)
					return;

				long compressedChunksBlockSize = 0, originalChunksBlockSize = 0;

				var chunkInfosInCurrentBlock = footer.Chunks.Skip(chunkIndex).TakeWhile(x =>
				{
					compressedChunksBlockSize += x.CompressedLength;
					originalChunksBlockSize += x.OriginalLength;

					if (originalChunksBlockSize <= MAX_MEMORY_BLOCK_SIZE)
						return true;
					else
					{
						compressedChunksBlockSize -= x.CompressedLength;
						originalChunksBlockSize -= x.OriginalLength;
						return false;
					}

				}).ToArray();

				int threadsForCurrentBlock = Math.Min(chunkInfosInCurrentBlock.Length, threads);

				chunkIndex += chunkInfosInCurrentBlock.Length;
				currentBlockOffset = chunkInfosInCurrentBlock[0].CompressedStart;

				int bytesRead;
				byte[] buffer = new byte[compressedChunksBlockSize];

				bytesRead = this._readerStream.Read(buffer, 0, (int)compressedChunksBlockSize);
				List<CustomGzipFooter.CompressedChunkInfo>[] threadChunkAssignations
					= new List<CustomGzipFooter.CompressedChunkInfo>[threadsForCurrentBlock];

				for (int i = 0; i < chunkInfosInCurrentBlock.Length; i++)
				{
					if (threadChunkAssignations[i % threadsForCurrentBlock] == null)
						threadChunkAssignations[i % threadsForCurrentBlock] = new List<CustomGzipFooter.CompressedChunkInfo>();
					threadChunkAssignations[i % threadsForCurrentBlock].Add(chunkInfosInCurrentBlock[i]);
				}

				if (threadsForCurrentBlock == 1)
				{
					StartWorker(0, currentBlockOffset, buffer, threadChunkAssignations[0].AsReadOnly(), cancellationToken);
				}
				else
				{
					using (CountdownEvent threadFinisher = new CountdownEvent(1))
					{
						for (int i = 0; i < threadsForCurrentBlock; i++)
						{
							var thread = new Thread((object param) =>
							{
								int threadIndex = (int)param;
								Thread.CurrentThread.Name = $"Decompress ThreadWorker: {threadIndex}";
								StartWorker(threadIndex, currentBlockOffset, buffer, threadChunkAssignations[threadIndex].AsReadOnly(), cancellationToken);
								threadFinisher.Signal();
							});

							threadFinisher.AddCount();
							thread.Start(i);
						}

						threadFinisher.Signal();
						threadFinisher.Wait();
					}
				}
				this._writerStream.Flush();
			}
		}

		private void StartWorker(
			int threadIndex,
			long currentBlockOffset,
			byte[] dataToProcess,
			IReadOnlyList<CustomGzipFooter.CompressedChunkInfo> chunksToProcess,
			CancellationToken cancellationToken)
		{
			for (int i = 0; i < chunksToProcess.Count; i++)
			{
				if (cancellationToken.IsCancellationRequested)
					return;
				var currentChunk = chunksToProcess[i];

				var currentChunkStart = currentChunk.CompressedStart - currentBlockOffset;

				using (var input = new MemoryStream(dataToProcess, (int)currentChunkStart, (int)currentChunk.CompressedLength, false))
				{
					// do not write directly to output...
					using (var output = new MemoryStream())
					{
						GzipWrapper.Decompress(input, output);
						lock (_writerLocker)
						{
							this._writerStream.Seek(currentChunk.OriginalStart, SeekOrigin.Begin);
							output.WriteTo(_writerStream);
						}
					}
				}
			}
		}
	}
}
