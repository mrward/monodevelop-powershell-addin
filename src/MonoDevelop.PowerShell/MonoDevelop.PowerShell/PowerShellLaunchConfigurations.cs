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
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.PowerShell
{
	class PowerShellLaunchConfigurations
	{
		Dictionary<string, List<PowerShellLaunchConfiguration>> configurationsCache =
			new Dictionary<string, List<PowerShellLaunchConfiguration>> ();

		public IEnumerable<PowerShellLaunchConfiguration> GetConfigurations (Document document)
		{
			List<PowerShellLaunchConfiguration> configurations = GetExistingConfigurations (document);
			if (configurations != null)
				return configurations;

			return ReadConfigurations (document);
		}

		List<PowerShellLaunchConfiguration> GetExistingConfigurations (Document document)
		{
			string directory = document.FileName.ParentDirectory;
			List<PowerShellLaunchConfiguration> configurations = null;
			if (configurationsCache.TryGetValue (directory, out configurations)) {
				return configurations;
			}
			return null;
		}

		public void SetActiveLaunchConfiguration (PowerShellLaunchConfiguration config, Document document)
		{
			List<PowerShellLaunchConfiguration> configurations = GetExistingConfigurations (document);
			if (configurations != null) {
				foreach (var existingConfig in configurations) {
					existingConfig.IsActive = false;
				}
				config.IsActive = true;
			}

			LoggingService.LogWarning ("PowerShell launch configuration not found. Unable to set active configuration. '{0}'", document.FileName);
		}

		List<PowerShellLaunchConfiguration> ReadConfigurations (Document document)
		{
			string directory = document.FileName.ParentDirectory;
			try {
				List<PowerShellLaunchConfiguration> foundConfigurations =
					PowerShellLaunchConfigurationsReader.Read (directory);
				if (foundConfigurations != null) {
					foundConfigurations.Insert (0, PowerShellLaunchConfiguration.None);
					configurationsCache[directory] = foundConfigurations;
					return foundConfigurations;
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Unable to read launch configurations.", ex);
			}

			return new List<PowerShellLaunchConfiguration> ();
		}
	}
}
