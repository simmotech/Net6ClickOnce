// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copy the Launcher.exe from this solution to C:\Program Files (x86)\Microsoft SDKs\ClickOnce Bootstrapper\Engine
// It will then be used for future ClickOnce deployments

using System;
using System.Deployment.Application;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Deployment.Launcher
{
    internal class ProcessHelper
    {
        private readonly ProcessStartInfo psi;

        /// <summary>
        /// ProcessHelper constructor
        /// </summary>
        /// <param name="exe">Executable name</param>
        /// <param name="args">Arguments</param>
        public ProcessHelper(string exe, string args)
        {
	        psi = new ProcessStartInfo(exe, args)
	              {
		              UseShellExecute = false
	              };

	        // From ApplicationDeployment which is not available in .NET 6!
	        AddLocalEnvironmentVariable("CLICKONCE_ISNETWORKDEPLOYED", ApplicationDeployment.IsNetworkDeployed.ToString());
	        AddLocalEnvironmentVariable("CLICKONCE_CURRENTVERSION", ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString());
	        AddLocalEnvironmentVariable("CLICKONCE_UPDATEDVERSION", ApplicationDeployment.CurrentDeployment.UpdatedVersion?.ToString());
	        AddLocalEnvironmentVariable("CLICKONCE_UPDATELOCATION", ApplicationDeployment.CurrentDeployment.UpdateLocation.ToString());
	        AddLocalEnvironmentVariable("CLICKONCE_UPDATEDAPPLICATIONFULLNAME", ApplicationDeployment.CurrentDeployment.UpdatedApplicationFullName);
	        AddLocalEnvironmentVariable("CLICKONCE_TIMEOFLASTUPDATECHECK", ApplicationDeployment.CurrentDeployment.TimeOfLastUpdateCheck.ToString("u"));
	        AddLocalEnvironmentVariable("CLICKONCE_ACTIVATIONURI", ApplicationDeployment.CurrentDeployment.ActivationUri?.ToString()); // Should this be AbsoluteUri?
	        AddLocalEnvironmentVariable("CLICKONCE_DATADIRECTORY", ApplicationDeployment.CurrentDeployment.DataDirectory);

	        // @e-master: https://github.com/dotnet/deployment-tools/pull/135
	        if (AppDomain.CurrentDomain.SetupInformation.ActivationArguments?.ActivationData is string[] activationData)
	        {
		        for (var i = 0; i < activationData.Length; i++)
		        {
			        AddLocalEnvironmentVariable($"CLICKONCE_ACTIVATIONDATA_{i + 1}", activationData[i]);
		        }
	        }

	        void AddLocalEnvironmentVariable(string name, string value)
	        {
		        if (value == null) return;

		        psi.EnvironmentVariables[name] = value;
	        }
        }

        /// <summary>
        /// Starts the process, with retries.
        /// Number of attempts and delay are specified in Constants class.
        /// </summary>
        public void StartProcessWithRetries()
        {
            Logger.LogInfo(Constants.InfoProcessToLaunch, psi.FileName, psi.Arguments);
            int count = 1;
            while (true)
            {
                try
                {
                    StartProcess();
                    return;
                }
                catch (Exception e)
                {
                    // Log each failure attempt
                    Logger.LogError(Constants.ErrorProcessStart, e.Message);

                    if (count++ < Constants.NumberOfProcessStartAttempts)
                    {
                        Logger.LogInfo(Constants.InfoProcessStartWaitRetry, Constants.DelayBeforeRetryMiliseconds);
                        Thread.Sleep(Constants.DelayBeforeRetryMiliseconds);
                        continue;
                    }
                    else
                    {
                        Logger.LogError(Constants.ErrorProcessFailedToLaunch, psi.FileName, psi.Arguments);
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Starts the process.
        /// </summary>
        private void StartProcess()
        {
            if (null == Process.Start(psi))
            {
                throw new LauncherException(Constants.ErrorProcessNotStarted);
            }
        }
    }
}
