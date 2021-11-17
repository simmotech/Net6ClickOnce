# Net6ClickOnce
Get ClickOnce working under .NET 6

**Included Projects:**


1. ClickOnceLauncher - a custom version of Launcher.exe
2. ClickOnceCommandLineTestApp - a simple ClickOnce test application

**ClickOnceLauncher**

Since this is a .NET Framework app, it has access to the ApplicationDeployment class that .NET 6 doesn't have.
It puts the information from ApplicationDeployment into local environment variables which can then be read by the launched ClickOnce app.
(original idea by @e-master)

**ClickOnceCommandLineTestApp**

As a console test application, this basically shows the information obtained from ClickOnceLauncher and gives an option to test in-app updating to the latest version (if there is one)

`ClickOnceInfo` is the class that holds the information read from the environment variables set by ClickOnceLauncher.
It also has a `GetLatestVersionInfo()` method which returns a `ClickOnceUpdateInfo` instance read directly from the ClickOnce deployment server/share to get the LatestVersion of the app (and MinimumVersion too if it has one).

These two classes should allow the ClickOnce app to do most things in .NET 6 that were available in .NET Framework previously.



Notes:
- I am new to Github (I won't be offended if you point out something I've done or not done)
- I am new to MarkDown
- I have only tested using a share on my PC (\\beast\deploy) pointing at the deploy output folder; deploying via FTP and installing via HTTP is written but untested as yet
- There is no testing for command-line parameters and/or file association double clicks but I am hoping all the information is available for someone (hint, hint) to try it out
- There is ClickOnceProfile.pubxml included in ClickOnceCommandLineTestApp and I had to manually override .gitignore to include it - it will need changing to point to a new share on your machine of course
