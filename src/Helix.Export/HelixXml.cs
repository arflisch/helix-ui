using System.IO.Compression;
using System.Text;

namespace Helix.Export;

/// <summary>Small helpers to write Open XML packages (a zip of XML parts).</summary>
internal static class HelixXml
{
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>Text escaped for XML; characters XML cannot hold (control characters) are dropped.</summary>
    public static string Esc(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var sb = new StringBuilder(text.Length + 8);
        foreach (var c in text)
        {
            switch (c)
            {
                case '&': sb.Append("&amp;"); break;
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                case '"': sb.Append("&quot;"); break;
                case '\t' or '\n' or '\r': sb.Append(c); break;
                case < ' ': break;
                case '￾' or '￿': break;
                default: sb.Append(c); break;
            }
        }

        return sb.ToString();
    }

    public static void Write(ZipArchive zip, string path, string xml)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, Utf8);
        writer.Write(xml);
    }

    /// <summary>"RRGGBB" from "#4f46e5", "4F46E5"…</summary>
    public static string Hex(string color) => color.Trim().TrimStart('#').ToUpperInvariant();

    public const string Declaration = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n";
}
