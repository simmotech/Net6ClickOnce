using System.Collections;
using System.Diagnostics;

using Framework.ClickOnce;

Console.WriteLine("---- Args"); // Not sure if these are possible in ClickOnce - is that what ActivationData is about?
foreach (var arg in args)
{
	Console.WriteLine(arg);
}
Console.WriteLine("----\r\n");

Console.WriteLine("---- Environment");
foreach (var environmentVariable in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>().Where(de => ((string) de.Key).StartsWith("CLICKONCE_")).OrderBy(de => de.Key))
{
	Console.WriteLine($"{environmentVariable.Key}={environmentVariable.Value}");
}
Console.WriteLine("----\r\n");

var clickOnceInfo = new ClickOnceInfo();
Console.WriteLine("---- ClickOnceInfo");
Console.WriteLine($"BaseDirectory              : {clickOnceInfo.BaseDirectory}");
Console.WriteLine($"TargetFrameworkName        : {clickOnceInfo.TargetFrameworkName}");
Console.WriteLine($"IsNetworkDeployed          : {clickOnceInfo.IsNetworkDeployed}");
Console.WriteLine($"CurrentVersion             : {clickOnceInfo.CurrentVersion}");
Console.WriteLine($"UpdatedVersion             : {clickOnceInfo.UpdatedVersion}");
Console.WriteLine($"UpdateLocation             : {clickOnceInfo.UpdateLocation}");
Console.WriteLine($"UpdatedApplicationFullName : {clickOnceInfo.UpdatedApplicationFullName}");
Console.WriteLine($"TimeOfLastUpdateCheck      : {clickOnceInfo.TimeOfLastUpdateCheck}");
Console.WriteLine($"ActivationUri              : {clickOnceInfo.ActivationUri}");
Console.WriteLine($"DataDirectory              : {clickOnceInfo.DataDirectory}");

if (clickOnceInfo.ActivationData != null)
{
	for (var i = 0; i < clickOnceInfo.ActivationData.Length; i++)
	{
		Console.WriteLine($"ActivationData[{i}]        : {clickOnceInfo.ActivationData[i]}");
	}
}
else
{
	Console.WriteLine($"ActivationData             : ");
}
Console.WriteLine("----\r\n");

var latestVersionInfo = clickOnceInfo.GetLatestVersionInfo().Result;
if (latestVersionInfo != null)
{
	Console.WriteLine("---- LatestVersionInfo");
	Console.WriteLine($"CurrentVersion             : {latestVersionInfo.CurrentVersion}");
	Console.WriteLine($"LatestVersion              : {latestVersionInfo.LatestVersion}");
	Console.WriteLine($"MinimumVersion             : {latestVersionInfo.MinimumVersion}");
	Console.WriteLine($"IsUpdateAvailable          : {latestVersionInfo.IsUpdateAvailable}");
	Console.WriteLine($"IsMandatoryUpdate          : {latestVersionInfo.IsMandatoryUpdate}");
	Console.WriteLine("----\r\n");

	if (latestVersionInfo.IsUpdateAvailable) Console.WriteLine("(Enter 'u' to update to the latest version)");
}

var input = Console.ReadLine();
if (input == "u")
{
	Console.WriteLine();
	Console.WriteLine("Updating....");

	try
	{
		// From https://www.mking.net/blog/programmatically-launching-clickonce-applications
		Process.Start("rundll32.exe", "dfshim.dll,ShOpenVerbApplication " + clickOnceInfo.UpdateLocation);
	}
	catch (Exception ex)
	{
		Console.WriteLine(ex);

		Console.ReadLine();
	}
}
