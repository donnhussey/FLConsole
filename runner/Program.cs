using System.Diagnostics;

var startInfo = new ProcessStartInfo("dotnet")
{
    UseShellExecute = false
};

startInfo.ArgumentList.Add("run");
startInfo.ArgumentList.Add("--project");
startInfo.ArgumentList.Add("flconsole/flconsole.csproj");
startInfo.ArgumentList.Add("--");

foreach (var argument in args)
{
    startInfo.ArgumentList.Add(argument);
}

using var process = Process.Start(startInfo);
if (process is null)
{
    return 1;
}

await process.WaitForExitAsync();
return process.ExitCode;
