//
// PowerShellLaunchConfigurations.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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

using System;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.PowerShell
{
	class PowerShellLaunchConfigurations
	{
		Dictionary<string, PowerShellLaunchConfigurationCacheInfo> configurationsCache =
			new Dictionary<string, PowerShellLaunchConfigurationCacheInfo> ();

		public IEnumerable<PowerShellLaunchConfiguration> GetConfigurations (Document document)
		{
			return GetConfigurations (document.FileName);
		}

		public IEnumerable<PowerShellLaunchConfiguration> GetConfigurations (FilePath scriptFileName)
		{
			PowerShellLaunchConfigurationCacheInfo cacheInfo = GetExistingConfigurations (scriptFileName);
			if (cacheInfo != null) {
				if (cacheInfo.IsOutOfDate ()) {
					return ReadConfigurations (scriptFileName, cacheInfo.GetActiveConfigurationName ());
				}
				return cacheInfo.Configurations;
			}

			return ReadConfigurations (scriptFileName);
		}

		PowerShellLaunchConfigurationCacheInfo GetExistingConfigurations (FilePath scriptFileName)
		{
			string directory = scriptFileName.ParentDirectory;
			PowerShellLaunchConfigurationCacheInfo cacheInfo = null;
			if (configurationsCache.TryGetValue (directory, out cacheInfo)) {
				return cacheInfo;
			}
			return null;
		}

		public void SetActiveLaunchConfiguration (PowerShellLaunchConfiguration config, Document document)
		{
			PowerShellLaunchConfigurationCacheInfo cacheInfo = GetExistingConfigurations (document.FileName);
			if (cacheInfo != null) {
				foreach (var existingConfig in cacheInfo.Configurations) {
					existingConfig.IsActive = false;
				}
				config.IsActive = true;
			}

			LoggingService.LogWarning ("PowerShell launch configuration not found. Unable to set active configuration. '{0}'", document.FileName);
		}

		List<PowerShellLaunchConfiguration> ReadConfigurations (FilePath scriptFileName, string activeConfiguration = "None")
		{
			string directory = scriptFileName.ParentDirectory;
			try {
				var reader = new PowerShellLaunchConfigurationsReader ();
				List<PowerShellLaunchConfiguration> foundConfigurations = reader.Read (directory);
				if (foundConfigurations != null) {
					AddNoneConfiguration (foundConfigurations);
					MarkActiveConfiguration (foundConfigurations, activeConfiguration);

					var cacheInfo = new PowerShellLaunchConfigurationCacheInfo (
						foundConfigurations,
						reader.FileName,
						reader.LastWriteTime.Value);
					configurationsCache[directory] = cacheInfo;

					return foundConfigurations;
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Unable to read launch configurations.", ex);
			}

			return new List<PowerShellLaunchConfiguration> ();
		}

		void AddNoneConfiguration (List<PowerShellLaunchConfiguration> configurations)
		{
			var noneConfiguration = PowerShellLaunchConfiguration.CreateNoneConfiguration ();
			configurations.Insert (0, noneConfiguration);
		}

		void MarkActiveConfiguration (IEnumerable<PowerShellLaunchConfiguration> configurations, string name)
		{
			var matchedConfiguration = configurations.FirstOrDefault (configuration => StringComparer.OrdinalIgnoreCase.Equals (configuration.Name, name));
			if (matchedConfiguration != null) {
				matchedConfiguration.IsActive = true;
				return;
			}

			var defaultActiveConfiguration = configurations.FirstOrDefault ();
			if (defaultActiveConfiguration != null)
				defaultActiveConfiguration.IsActive = true;
		}

		public PowerShellLaunchConfiguration GetActiveLaunchConfiguration (FilePath scriptFileName)
		{
			return GetConfigurations (scriptFileName)
				.FirstOrDefault (configuration => configuration.IsActive);
		}
	}
}
