using System.Globalization;
using System.Xml.Linq;

namespace Framework.ClickOnce
{
	// This is a partial replacement for ApplicationDeployment which is not available in .NET 6
	// We use a custom Launcher.exe which will set various "CLICKONCE_*" local environment variables
	public class ClickOnceInfo
	{
		public ClickOnceInfo()
		{
			BaseDirectory = AppContext.BaseDirectory;
			TargetFrameworkName = AppContext.TargetFrameworkName;

			if (Environment.GetEnvironmentVariable("CLICKONCE_ISNETWORKDEPLOYED") == bool.TrueString)
			{
				IsNetworkDeployed = true;
			}

			if (Environment.GetEnvironmentVariable("CLICKONCE_CURRENTVERSION") is {} currentVersionString && Version.TryParse(currentVersionString, out var currentVersion))
			{
				CurrentVersion = currentVersion;
			}

			if (Environment.GetEnvironmentVariable("CLICKONCE_UPDATEDVERSION") is {} updatedVersionString && Version.TryParse(updatedVersionString, out var updatedVersion))
			{
				UpdatedVersion = updatedVersion;
			}

			if (Environment.GetEnvironmentVariable("CLICKONCE_UPDATELOCATION") is {} updateLocationString && Uri.TryCreate(updateLocationString, UriKind.RelativeOrAbsolute, out var updateLocation))
			{
				UpdateLocation = updateLocation;

				ApplicationName = UpdateLocation?.Segments[^1].Replace(".application", null, StringComparison.OrdinalIgnoreCase);
			}

			if (Environment.GetEnvironmentVariable("CLICKONCE_UPDATEDAPPLICATIONFULLNAME") is {} updatedApplicationFullName)
			{
				UpdatedApplicationFullName = updatedApplicationFullName;
			}

			if (Environment.GetEnvironmentVariable("CLICKONCE_TIMEOFLASTUPDATECHECK") is {} timeOfLastUpdateCheckString && DateTime.TryParse(timeOfLastUpdateCheckString, null, DateTimeStyles.RoundtripKind, out var timeOfLastUpdateCheck))
			{
				TimeOfLastUpdateCheck = timeOfLastUpdateCheck;
			}

			if (Environment.GetEnvironmentVariable("CLICKONCE_ACTIVATIONURI") is {} activationUriString && Uri.TryCreate(activationUriString, UriKind.RelativeOrAbsolute, out var activationUri))
			{
				ActivationUri = activationUri;
			}

			if (Environment.GetEnvironmentVariable("CLICKONCE_DATADIRECTORY") is {} dataDirectory)
			{
				DataDirectory = dataDirectory;
			}

			// Not 100%e sure what this is but it is mentioned at https://github.com/dotnet/deployment-tools/pull/135 and https://github.com/dotnet/deployment-tools/issues/113
			// so we can include it. Think it might be about passing command line arguments and/or FileAssociation arguments.
			if (Environment.GetEnvironmentVariable("CLICKONCE_ACTIVATIONDATA_1") is {} activationDataItem)
			{
				var items = new List<string>();
				var index = 1;

				do
				{
					items.Add(activationDataItem);

					activationDataItem = Environment.GetEnvironmentVariable($"CLICKONCE_ACTIVATIONDATA_{++index}");
				}
				while (activationDataItem != null);

				ActivationData = items.ToArray();
			}
		}

		public string BaseDirectory { get; init; }

		public string TargetFrameworkName { get; init; }

		public bool IsNetworkDeployed { get; init; }

		public Version CurrentVersion { get; init; }

		public Version UpdatedVersion { get; init; }

		public Uri UpdateLocation { get; init; }

		public string UpdatedApplicationFullName { get; init; }

		public DateTime TimeOfLastUpdateCheck { get; init; }

		public Uri ActivationUri { get; init; }

		public string DataDirectory { get; init; }

		public string[] ActivationData { get; init; }

		public string ApplicationName { get; init; }

		public async Task<ClickOnceUpdateInfo> GetLatestVersionInfo()
		{
			if (!IsNetworkDeployed) return null;

			// TODO: Not tested as yet
			if (UpdateLocation.Segments[0].StartsWith("http", StringComparison.OrdinalIgnoreCase))
			{
				using var client = new HttpClient { BaseAddress = UpdateLocation };
				await using var stream = await client.GetStreamAsync(UpdateLocation);

				return await ReadServerManifest(stream);
			}

			if (UpdateLocation.IsFile)
			{
				await using var stream = File.OpenRead(UpdateLocation.LocalPath);

				return await ReadServerManifest(stream);
			}

			return null;
		}

		// Based on code from https://github.com/derskythe/WpfSettings/blob/master/PureManApplicationDevelopment/PureManClickOnce.cs
		async Task<ClickOnceUpdateInfo> ReadServerManifest(Stream stream)
		{
			XNamespace nsV1 = "urn:schemas-microsoft-com:asm.v1";
			XNamespace nsV2 = "urn:schemas-microsoft-com:asm.v2";

			var xmlDoc = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);

			var xmlElement = xmlDoc.Descendants(nsV1 + "assemblyIdentity").FirstOrDefault();
			if (xmlElement == null) throw new ClickOnceDeploymentException($"Invalid manifest document for {ApplicationName}.application");

			var version = xmlElement.Attribute("version")?.Value;
			if (string.IsNullOrEmpty(version)) throw new ClickOnceDeploymentException("Version info is empty!");

			// Minimum version is optional
			var minimumVersion = xmlDoc.Descendants(nsV2 + "deployment").FirstOrDefault()?.Attribute("minimumRequiredVersion")?.Value;

			return new ClickOnceUpdateInfo
				   {
					   CurrentVersion = CurrentVersion,
					   LatestVersion = new Version(version),
					   MinimumVersion = string.IsNullOrEmpty(minimumVersion) ? null : new Version(minimumVersion)
				   };
		}
	}

	public class ClickOnceUpdateInfo
	{
		public Version CurrentVersion { get; init; }
		public Version LatestVersion { get; init; }
		public Version MinimumVersion { get; init; }

		public bool IsUpdateAvailable
		{
			get { return LatestVersion > CurrentVersion; }
		}

		public bool IsMandatoryUpdate
		{
			get { return IsUpdateAvailable && MinimumVersion != null && MinimumVersion > CurrentVersion; }
		}
	}

	public class ClickOnceDeploymentException: Exception
	{
		public ClickOnceDeploymentException(string message): base(message)
		{}
	}
}
