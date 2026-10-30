using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace FreshDay.LeadBot.Leads;

/// <summary>
/// Резервная копия каждой заявки в файл (строка JSON на заявку) — на случай, если Telegram
/// был недоступен. Файл содержит персональные данные: храните его только на своём сервере.
/// </summary>
public sealed class LeadArchive(IOptions<LeadBotOptions> options, IHostEnvironment env, ILogger<LeadArchive> log)
{
    // Кириллица в файле остаётся читаемой, а не \u041e…
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task AppendAsync(Lead lead, DateTimeOffset receivedAt, bool delivered, CancellationToken ct)
    {
        var path = options.Value.ArchivePath;
        if (string.IsNullOrWhiteSpace(path)) return;
        if (!Path.IsPathRooted(path)) path = Path.Combine(env.ContentRootPath, path);

        var line = JsonSerializer.Serialize(new
        {
            receivedAt = receivedAt.ToOffset(LeadFormatter.MinskOffset),
            delivered,
            lead,
        }, Json) + Environment.NewLine;

        await _lock.WaitAsync(ct);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.AppendAllTextAsync(path, line, ct);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            log.LogError("Не удалось сохранить заявку в {Path}: {Error}", path, ex.Message);
        }
        finally
        {
            _lock.Release();
        }
    }
}
