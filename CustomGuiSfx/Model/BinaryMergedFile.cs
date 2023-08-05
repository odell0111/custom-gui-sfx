using System.Diagnostics;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CustomGuiSfx.Model;

public class BinaryMergedFile
{
	public string? FullName { get; set; }
	public byte[]? Data { get; set; }
	public long Index { get; set; } = -1;

	public Version? Version
	{
		get {
			if (!Exists())
				return null;

			var fvi = FileVersionInfo.GetVersionInfo(FullName!);
			return new Version(fvi.ProductMajorPart, fvi.ProductMinorPart);
		}
	}

	public Task CreateFile(bool disposeData = false) => Task.Run(() =>
	{
		File.WriteAllBytes(FullName!, Data!);
		if (disposeData)
			Data = null;
	});

	public bool Exists() => File.Exists(FullName);

	public void DeleteFile()
	{
		if (File.Exists(FullName))
			File.Delete(FullName);
	}
}
