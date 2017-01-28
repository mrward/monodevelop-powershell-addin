//
// PowerShellCommandLineArgumentsBuilder.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Text;
using System.IO;
using MonoDevelop.Core;

namespace MonoDevelop.PowerShell
{
	public class PowerShellCommandLineArgumentsBuilder
	{
		StringBuilder arguments = new StringBuilder ();

		public void Build ()
		{
			if (string.IsNullOrEmpty (BundlesModulesPath))
				BundlesModulesPath = GetDefaultBundlesModulesPath ();

			if (string.IsNullOrEmpty (PowerShellScriptPath))
				PowerShellScriptPath = GetDefaultPowerShellScriptPath ();

			if (string.IsNullOrEmpty (LogPath))
				LogPath = GetDefaultLogPath ();

			arguments.Append ("-NoProfile ");
			arguments.Append ("-NonInteractive ");

			if (Platform.IsWindows) {
				arguments.Append ("-ExecutionPolicy Unrestricted ");
			}

			AppendSingleQuotedParameter ("-Command &", PowerShellScriptPath);

			AppendSingleQuotedParameter ("-EditorServicesVersion", RequiredEditorServicesVersion);
			AppendSingleQuotedParameter ("-HostName", HostName);
			AppendSingleQuotedParameter ("-HostProfileId", HostProfileId);
			AppendSingleQuotedParameter ("-HostVersion", HostVersion);
			AppendSingleQuotedParameter ("-BundledModulesPath", BundlesModulesPath);
			arguments.Append ("-LogLevel " + LogLevel + " ");
			AppendSingleQuotedParameter ("-LogPath", LogPath);

			Arguments = arguments.ToString ();
		}

		public string BundlesModulesPath { get; set; }
		public string PowerShellScriptPath { get; set; }
		public string RequiredEditorServicesVersion { get; set; } = "0.9.0";
		public string HostName { get; set; } = "MonoDevelopHost";
		public string HostProfileId { get; set; } = "MonoDevelop";
		public string HostVersion { get; set; } = "0.1";
		public string LogPath { get; set; }
		public string LogLevel { get; set; } = "Normal";

		public string Arguments { get; private set; }

		static string GetDefaultBundlesModulesPath ()
		{
			return Path.GetFullPath (Path.Combine (GetAssemblyLocation (), "modules"));
		}

		static string GetAssemblyLocation ()
		{
			return Path.GetDirectoryName (typeof (PowerShellCommandLineArgumentsBuilder).Assembly.Location);
		}

		static string GetDefaultPowerShellScriptPath ()
		{
			return Path.GetFullPath (Path.Combine (GetAssemblyLocation (), "scripts", "Start-EditorServices.ps1"));
		}

		static string GetDefaultLogPath ()
		{
			return Path.GetFullPath (Path.Combine (UserProfile.Current.LogDir, "powershell-output.log"));
		}

		void AppendSingleQuotedParameter (string name, string value)
		{
			arguments.AppendFormat ("{0} '{1}' ", name, value);
		}
	}
}
