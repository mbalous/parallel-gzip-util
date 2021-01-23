using GzipTool.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;
using System.Threading;

namespace GzipTool.Test
{
	[TestClass]
	public class ParallelFileIOTest
	{
		[TestMethod]
		public void TestSingleThread()
		{
			const string data = "ABAB ABAB ABAB ABAB ABAB ABAB";
			byte[] bytes = Encoding.UTF8.GetBytes(data);

			using (var input = new MemoryStream(bytes))
			{
				using (var output = new MemoryStream())
				{
					var compress = new ParallelFileCompress(input, output);
					compress.Run(5, 1, CancellationToken.None);

					// seek compressed to beginning
					output.Seek(0, SeekOrigin.Begin);

					// reset original stream
					input.Seek(0, SeekOrigin.Begin);
					input.SetLength(0);

					var decompressor = new ParallelFileDecompress(output, input);
					decompressor.Run(1, CancellationToken.None);

					input.Seek(0, SeekOrigin.Begin);
					var decompressedBytes = input.ToArray();
					CollectionAssert.AreEqual(bytes, decompressedBytes);
				}
			}
		}

		[TestMethod]
		public void TestSingleThreadAlternative()
		{
			const string data = "ABAB ABAB ABAB ABAB ABAB ABAB";
			byte[] bytes = Encoding.UTF8.GetBytes(data);

			using (var input = new MemoryStream(bytes))
			{
				using (var output = new MemoryStream())
				{
					var compress = new ParallelFileCompress(input, output);
					compress.Run(5, 1, CancellationToken.None);

					// seek compressed to beginning
					output.Seek(0, SeekOrigin.Begin);

					// reset original stream
					input.Seek(0, SeekOrigin.Begin);
					input.SetLength(0);

					var decompressor = new ParallelFileDecompress2(output, input);
					decompressor.Run(1, CancellationToken.None);

					input.Seek(0, SeekOrigin.Begin);
					var decompressedBytes = input.ToArray();
					CollectionAssert.AreEqual(bytes, decompressedBytes);
				}
			}

		}

		[TestMethod]
		public void TestMultiThread()
		{
			const string data = "ABAB ABAB ABAB ABAB ABAB ABAB";
			byte[] bytes = Encoding.UTF8.GetBytes(data);

			using (var input = new MemoryStream(bytes))
			{
				using (var output = new MemoryStream())
				{
					var compress = new ParallelFileCompress(input, output);
					compress.Run(5, 2, CancellationToken.None);

					// seek compressed to beginning
					output.Seek(0, SeekOrigin.Begin);

					// reset original stream
					input.Seek(0, SeekOrigin.Begin);
					input.SetLength(0);

					var decompressor = new ParallelFileDecompress(output, input);
					decompressor.Run(1, CancellationToken.None);

					input.Seek(0, SeekOrigin.Begin);
					var decompressedBytes = input.ToArray();
					CollectionAssert.AreEqual(bytes, decompressedBytes);
				}
			}
		}

		[TestMethod]
		public void TestMultiThreadAlternative()
		{
			const string data = "ABAB ABAB ABAB ABAB ABAB ABAB";
			byte[] bytes = Encoding.UTF8.GetBytes(data);

			using (var input = new MemoryStream(bytes))
			{
				using (var output = new MemoryStream())
				{
					var compress = new ParallelFileCompress(input, output);
					compress.Run(5, 2, CancellationToken.None);

					// seek compressed to beginning
					output.Seek(0, SeekOrigin.Begin);

					// reset original stream
					input.Seek(0, SeekOrigin.Begin);
					input.SetLength(0);

					var decompressor = new ParallelFileDecompress2(output, input);
					decompressor.Run(1, CancellationToken.None);

					input.Seek(0, SeekOrigin.Begin);
					var decompressedBytes = input.ToArray();
					CollectionAssert.AreEqual(bytes, decompressedBytes);
				}
			}
		}
	}
}
