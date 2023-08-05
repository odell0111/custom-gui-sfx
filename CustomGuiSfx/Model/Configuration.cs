using System;
using System.CodeDom;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace CustomGuiSfx.Model;

public static class Configuration
{
	public static string? DisplayName { get; set; }
	public static string? DisplayNameLink { get; set; }
	public static long ArchiveSize { get; set; }
	public static string? ArchiveExtension { get; set; }
	public static string? Link1Label { get; set; }
	public static string? Link1 { get; set; }
	public static string? Link2Label { get; set; }
	public static string? Link2 { get; set; }
	public static string? Link3Label { get; set; }
	public static string? Link3 { get; set; }
	public static string? FileToExecute { get; set; }
	public static int FirstDisplayImageSize { get; set; }
	public static int SecondDisplayImageSize { get; set; }
	public static int ThirdDisplayImageSize { get; set; }
	public static int DisplayImageHeightToShowOnUI { get; set; } = 200;
	// Comment entry must be the last one
	public static string? Comment { get; set; }

	/// <summary>
	/// Generates a configuration content template with the current configuration
	/// </summary>
	/// <returns></returns>
	public static string GetTemplate()
	{
		var sb = new StringBuilder();
		sb.AppendLine(App.CFS);
		foreach(var prop in typeof(Configuration).GetProperties())
		{
			sb.Append(prop.Name + "=");
			var value = prop.GetValue(null, null);
			if (value is null)
				sb.Append(Environment.NewLine);
			// else if the property has a value different from its default value
			else
			{
				var hasDefaultValue = value.Equals(Activator.CreateInstance(prop.PropertyType));
				if (!hasDefaultValue)
					sb.AppendLine(value.ToString());
				else
					sb.Append(Environment.NewLine);
			}
		}
		sb.Append(App.CFE);

		return sb.ToString();
	}
}
