using System.Diagnostics;

Console.WriteLine("Starting SRVS web project...");

var psi = new ProcessStartInfo
{
    FileName = "dotnet",
    Arguments = "run --project frontend/SRVS.Web --launch-profile http",
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    RedirectStandardInput = false,
    CreateNoWindow = false,
};

Console.WriteLine("========================================");
Console.WriteLine("Web Application: http://localhost:5299");
Console.WriteLine("API Endpoints:   http://localhost:5299/api");
Console.WriteLine("API Swagger UI:  http://localhost:5299/swagger");
Console.WriteLine("========================================");

using var proc = new Process { StartInfo = psi };

proc.OutputDataReceived += (s, e) => { if (e.Data is not null) Console.WriteLine(e.Data); };
proc.ErrorDataReceived += (s, e) => { if (e.Data is not null) Console.Error.WriteLine(e.Data); };

proc.Start();
proc.BeginOutputReadLine();
proc.BeginErrorReadLine();

await proc.WaitForExitAsync();

return proc.ExitCode;

//TEST TEST TEST TEST TEST TEST TEST TEST TEST TEST TEST TEST TEST TEST TEST
