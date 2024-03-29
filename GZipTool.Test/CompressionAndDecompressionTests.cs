﻿using GzipTool.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;
using System.Threading;

namespace GZipTool.Test;

[TestClass]
public class CompressionAndDecompressionTests
{
    [TestMethod]
    [DataRow(1, false, DisplayName = "Single threaded")]
    [DataRow(1, true, DisplayName = "Single threaded, alternative algo for decompression")]
    [DataRow(2, false, DisplayName = "Multi threaded")]
    [DataRow(2, true, DisplayName = "Multi threaded, alternative algo for decompression")]
    public void TestCompressionAndDecompression(int threadCount, bool useAlternativeDecompression)
    {
        const string data = "ABAB ABAB ABAB ABAB ABAB ABAB";
        byte[] bytes = Encoding.UTF8.GetBytes(data);

        using (var input = new MemoryStream(bytes))
        {
            using (var output = new MemoryStream())
            {
                var compress = new ParallelFileCompress(input, output);
                compress.Run(5, threadCount, CancellationToken.None);

                // seek compressed to beginning
                output.Seek(0, SeekOrigin.Begin);

                // reset original stream
                input.Seek(0, SeekOrigin.Begin);
                input.SetLength(0);

                ParallelFileIO decompressor;
                if (useAlternativeDecompression)
                    decompressor = new ParallelFileDecompress2(output, input);
                else
                    decompressor = new ParallelFileDecompress(output, input);
                decompressor.Run(threadCount, CancellationToken.None);

                input.Seek(0, SeekOrigin.Begin);
                var decompressedBytes = input.ToArray();
                CollectionAssert.AreEqual(bytes, decompressedBytes);
            }
        }
    }
}