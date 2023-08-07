using GzipTool.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

namespace GZipTool.Test;

[TestClass]
public class GZipWrapperTests
{
	[TestMethod]
	public void TestCompressAndDecompress()
	{
		const string data = "ABAB AB ABAB AB ABAB xx";
		var originalBytes = Encoding.UTF8.GetBytes(data);
		var compressed = GZipWrapper.Compress(originalBytes);
		var decompressed = GZipWrapper.Decompress(compressed);

		CollectionAssert.AreEqual(originalBytes, decompressed);
	}
}