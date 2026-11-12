using Microsoft.JSInterop;

namespace Helix.Blazor;

/// <summary>Sends a file to the browser from C#: <c>await JS.HelixDownloadAsync("report.xlsx", bytes);</c></summary>
public static class HelixDownloadExtensions
{
    /// <summary>Downloads the bytes as a file; the content type is guessed from the extension when not given.</summary>
    public static async Task HelixDownloadAsync(this IJSRuntime js, string fileName, byte[] content, string? contentType = null)
    {
        var module = await js.InvokeAsync<IJSObjectReference>("import", HelixJs.Path);
        try
        {
            await module.InvokeVoidAsync("downloadBytes", fileName, content, contentType ?? ContentTypeOf(fileName));
        }
        finally
        {
            await module.DisposeAsync();
        }
    }

    /// <summary>Downloads a text as a UTF-8 file (with a BOM, so that Excel reads accents in CSV).</summary>
    public static Task HelixDownloadAsync(this IJSRuntime js, string fileName, string text, string? contentType = null) =>
        js.HelixDownloadAsync(fileName, [.. System.Text.Encoding.UTF8.GetPreamble(), .. System.Text.Encoding.UTF8.GetBytes(text)], contentType);

    private static string ContentTypeOf(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".xlsx" => Helix.Export.HelixExport.ExcelContentType,
        ".docx" => Helix.Export.HelixExport.WordContentType,
        ".csv" => Helix.Export.HelixExport.CsvContentType,
        ".json" => "application/json",
        ".txt" or ".log" => "text/plain;charset=utf-8",
        ".pdf" => "application/pdf",
        _ => "application/octet-stream",
    };
}
