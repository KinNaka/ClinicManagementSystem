using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace ClinicManagementSystem.Web.Data;

/// <summary>
/// Connects to the verified active LocalDB pipe, using its CLI when startup is needed.
/// Only used in Development for local, Windows-authenticated connections.
/// </summary>
public static class DevelopmentLocalDbConnection
{
    public static async Task<string> ResolveAsync(string connectionString)
    {
        var connection = new SqlConnectionStringBuilder(connectionString);
        const string prefix = @"(localdb)\";
        if (!OperatingSystem.IsWindows() || !connection.IntegratedSecurity ||
            !connection.DataSource.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return connectionString;

        var instance = connection.DataSource[prefix.Length..];
        if (!Regex.IsMatch(instance, @"^[A-Za-z0-9_-]+$")) return connectionString;

        // The debugger can observe different LocalDB registry state from the CLI
        // run in a regular terminal. The engine log records the real active pipe.
        var loggedPipe = ReadLoggedPipe(instance);
        if (loggedPipe != null && await CanConnectAsync(connection, loggedPipe))
            return ForPipe(connection, loggedPipe);

        var information = await RunAsync("info", instance);
        var pipe = FindPipe(information);
        if (pipe == null)
        {
            await RunAsync("start", instance);
            pipe = FindPipe(await RunAsync("info", instance));
        }
        if (pipe == null)
            throw new InvalidOperationException($"LocalDB '{instance}' chưa chạy. Chạy 'sqllocaldb start {instance}' trong PowerShell rồi thử lại.");

        return ForPipe(connection, pipe);
    }

    private static string ForPipe(SqlConnectionStringBuilder connection, string pipe)
    {
        connection.DataSource = pipe;
        // A private, same-machine LocalDB pipe has a self-signed server certificate.
        connection.TrustServerCertificate = true;
        return connection.ConnectionString;
    }

    private static string? ReadLoggedPipe(string instance)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "Microsoft SQL Server Local DB", "Instances", instance, "error.log");
        try
        {
            using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            if (file.Length > 20 * 1024 * 1024) return null;
            using var reader = new StreamReader(file, detectEncodingFromByteOrderMarks: true);
            var matches = Regex.Matches(reader.ReadToEnd(), @"\\\\\.\\pipe\\LOCALDB#[A-Za-z0-9]+\\tsql\\query", RegexOptions.IgnoreCase);
            return matches.Count > 0 ? "np:" + matches[^1].Value : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return null; }
    }

    private static async Task<bool> CanConnectAsync(SqlConnectionStringBuilder original, string pipe)
    {
        var probe = new SqlConnectionStringBuilder(original.ConnectionString) {
            DataSource = pipe, InitialCatalog = "master", AttachDBFilename = "",
            TrustServerCertificate = true, ConnectTimeout = 3, ConnectRetryCount = 0
        };
        try
        {
            await using var connection = new SqlConnection(probe.ConnectionString);
            await connection.OpenAsync();
            return true;
        }
        catch (SqlException) { return false; }
    }

    private static string? FindPipe(string output) =>
        Regex.Match(output, @"np:\\\\.\\pipe\\LOCALDB#[A-Za-z0-9]+\\tsql\\query", RegexOptions.IgnoreCase)
            is { Success: true } match ? match.Value : null;

    private static async Task<string> RunAsync(string command, string instance)
    {
        var start = new ProcessStartInfo("SqlLocalDB.exe") {
            UseShellExecute = false, RedirectStandardOutput = true,
            RedirectStandardError = true, CreateNoWindow = true
        };
        start.ArgumentList.Add(command);
        start.ArgumentList.Add(instance);
        try
        {
            using var process = Process.Start(start)
                ?? throw new InvalidOperationException("Không khởi chạy được công cụ SqlLocalDB.");
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var error = process.StandardError.ReadToEndAsync(timeout.Token);
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException)
            {
                if (!process.HasExited) process.Kill(); // Only the CLI process started above.
                throw new InvalidOperationException("Công cụ LocalDB không phản hồi trong 30 giây.");
            }
            var text = await output;
            var errorText = await error;
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"Lệnh 'sqllocaldb {command} {instance}' thất bại: {errorText.Trim()}");
            return text;
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            throw new InvalidOperationException("Không tìm thấy SqlLocalDB.exe. Kiểm tra SQL Server Express LocalDB và PATH trên máy.", ex);
        }
    }
}
