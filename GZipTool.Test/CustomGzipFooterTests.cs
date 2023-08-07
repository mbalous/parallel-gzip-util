using GzipTool.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace GzipTool.Test;

[TestClass]
public class CustomGZipFooterTests
{
	[TestMethod]
	public void SerializationTest()
	{
		CustomGZipFooter deserializedFooter, originalFooter = new CustomGZipFooter();
		originalFooter.AddChunkInfo(5, 5, 10, 10);
		originalFooter.AddChunkInfo(5, 5, 10, 10);
		originalFooter.AddChunkInfo(100, 200, 300, 400);
		using (var ms = new MemoryStream())
		{
			originalFooter.WriteToStream(ms);
			Assert.AreEqual(originalFooter.GetFooterSize(), ms.Position);
			ms.Seek(0, SeekOrigin.Begin);
			deserializedFooter = CustomGZipFooter.FromStream(ms);
		}
		Assert.AreEqual(originalFooter, deserializedFooter);
	}

	[TestMethod]
	public void EqualsTest()
	{
		CustomGZipFooter
			footer1 = new CustomGZipFooter(),
			footer2 = new CustomGZipFooter();

		footer1.AddChunkInfo(5, 5, 10, 10);
		footer2.AddChunkInfo(5, 5, 10, 10);

		Assert.AreEqual(footer1, footer2);

		footer1.AddChunkInfo(5, 5, 10, 10);
		footer2.AddChunkInfo(5, 5, 10, 11);

		Assert.AreNotEqual(footer1, footer2);
	}
}