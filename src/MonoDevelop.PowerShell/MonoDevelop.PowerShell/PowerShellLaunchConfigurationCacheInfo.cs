//
// PowerShellLaunchConfigurationCacheInfo.cs
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
using System.IO;
using MonoDevelop.Core;
using System.Linq;

namespace MonoDevelop.PowerShell
{
	class PowerShellLaunchConfigurationCacheInfo
	{
		public PowerShellLaunchConfigurationCacheInfo (
			List<PowerShellLaunchConfiguration> configurations,
			FilePath launchJsonFileName,
			DateTime lastWriteTime)
		{
			Configurations = configurations;
			LaunchJsonFileName = launchJsonFileName;
			LaunchJsonLastWriteTime = lastWriteTime;
		}

		public List<PowerShellLaunchConfiguration> Configurations { get; private set; }
		public FilePath LaunchJsonFileName { get; private set; }
		public DateTime LaunchJsonLastWriteTime { get; private set; }

		public bool IsOutOfDate ()
		{
			if (!File.Exists (LaunchJsonFileName))
				return true;

			DateTime lastWriteTime = File.GetLastWriteTime (LaunchJsonFileName);
			return lastWriteTime > LaunchJsonLastWriteTime;
		}

		public string GetActiveConfigurationName ()
		{
			var activeConfiguration = Configurations.FirstOrDefault (c => c.IsActive);
			if (activeConfiguration != null)
				return activeConfiguration.Name;

			return null;
		}
	}
}
