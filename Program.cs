using System.Diagnostics;

Console.WriteLine("Starting SRVS web project...");

var webProjectPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "frontend", "SRVS.Web"));
StopStaleWebProcesses(webProjectPath);

var psi = new ProcessStartInfo
{
    FileName = "dotnet",
    Arguments = "run --no-restore --project frontend/SRVS.Web --urls http://localhost:5300",
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    RedirectStandardInput = false,
    CreateNoWindow = false,
};
psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";

Console.WriteLine("========================================");
Console.WriteLine("Web Application: http://localhost:5300");
Console.WriteLine("API Endpoints:   http://localhost:5300/api");
Console.WriteLine("API Swagger UI:  http://localhost:5300/swagger");
Console.WriteLine("========================================");

using var proc = new Process { StartInfo = psi };

proc.OutputDataReceived += (s, e) => { if (e.Data is not null) Console.WriteLine(e.Data); };
proc.ErrorDataReceived += (s, e) => { if (e.Data is not null) Console.Error.WriteLine(e.Data); };

proc.Start();
proc.BeginOutputReadLine();
proc.BeginErrorReadLine();

await proc.WaitForExitAsync();

return proc.ExitCode;

static void StopStaleWebProcesses(string webProjectPath)
{
    foreach (var process in Process.GetProcessesByName("SRVS.Web"))
    {
        try
        {
            var processPath = process.MainModule?.FileName;
            if (string.IsNullOrWhiteSpace(processPath) ||
                !processPath.StartsWith(webProjectPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Console.WriteLine($"Stopping existing SRVS.Web process {process.Id}...");
            process.Kill(entireProcessTree: true);
            process.WaitForExit(5000);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            Console.WriteLine($"Skipped existing SRVS.Web process {process.Id}: {ex.Message}");
        }
        finally
        {
            process.Dispose();
        }
    }
}
