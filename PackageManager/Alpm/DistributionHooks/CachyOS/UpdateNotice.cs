using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using PackageManager.Alpm.Questions;
using static PackageManager.Alpm.AlpmReference;

namespace PackageManager.Alpm.DistributionHooks.CachyOS;

public sealed class UpdateNotice
{
    private const string NoticeUrl = "https://iso-stats.cachyos.org/api/v2/last_update_notice";
    private const string NoNotice = "No notice found";
    private const string NoticeFileName = "CACHY_UPDATE_NOTICE";

    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    public async Task<bool> CheckAsync(
        IntPtr handle,
        string dbPath,
        Func<AlpmQuestionEventArgs, bool> raiseQuestion)
    {
        try
        {
            if (!IsCachyOs(handle))
                return true;

            var notice = await FetchNoticeAsync();
            if (notice is null || string.IsNullOrWhiteSpace(notice.Body))
                return true;

            var statePath = Path.Combine(dbPath, "local", NoticeFileName);

            if (File.Exists(statePath) &&
                string.Equals(File.ReadAllText(statePath).Trim(), notice.Id, StringComparison.Ordinal))
            {
                return true;
            }

            var args = new AlpmQuestionEventArgs(AlpmQuestionType.UpdateNotice, notice.Body)
            {
                Response = new QuestionResponse(0, null)
            };

            if (!raiseQuestion(args))
                return false;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(statePath)!);
                File.WriteAllText(statePath, notice.Id);
            }
            catch
            {
            }

            return true;
        }
        catch
        {
            return true;
        }
    }

    private static bool IsCachyOs(IntPtr handle)
    {
        var current = GetSyncDbs(handle);
        while (current != IntPtr.Zero)
        {
            var node = Marshal.PtrToStructure<AlpmList>(current);
            if (node.Data != IntPtr.Zero)
            {
                var name = Marshal.PtrToStringUTF8(DbGetName(node.Data));
                if (name is not null && name.Contains("cachyos", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            current = node.Next;
        }

        return false;
    }

    private static async Task<NoticeDto?> FetchNoticeAsync()
    {
        using var response = await Client.GetAsync(NoticeUrl);
        if (!response.IsSuccessStatusCode)
            return null;

        var payload = (await response.Content.ReadAsStringAsync()).Trim();
        if (payload.Length == 0 || string.Equals(payload, NoNotice, StringComparison.Ordinal))
            return null;

        return JsonSerializer.Deserialize(payload, NoticeJsonContext.Default.NoticeDto);
    }
}
