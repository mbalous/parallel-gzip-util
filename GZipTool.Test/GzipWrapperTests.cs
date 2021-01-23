using GzipTool.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

namespace GzipTool.Test
{
	[TestClass]
	public class GzipWrapperTests
	{
		[TestMethod]
		public void TestCompressAndDecompress()
		{
			const string data = "ABAB AB ABAB AB ABAB xx";
			var originalBytes = Encoding.UTF8.GetBytes(data);
			var compressed = GzipWrapper.Compress(originalBytes);
			var decompressed = GzipWrapper.Decompress(compressed);

			CollectionAssert.AreEqual(originalBytes, decompressed);
		}
	}
}
