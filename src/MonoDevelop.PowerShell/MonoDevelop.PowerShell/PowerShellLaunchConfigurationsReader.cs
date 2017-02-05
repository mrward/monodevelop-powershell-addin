//
// PowerShellLaunchConfigurationsReader.cs
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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MonoDevelop.PowerShell
{
	static class PowerShellLaunchConfigurationsReader
	{
		public static List<PowerShellLaunchConfiguration> Read (string directory)
		{
			if (!Directory.Exists (directory))
				return null;

			string launchFileName = FindLaunchFile (directory);
			if (launchFileName == null)
				return null;

			return ReadFile (launchFileName);
		}

		static string FindLaunchFile (string directory)
		{
			string launchFileName = Path.Combine (directory, "launch.json");
			if (File.Exists (launchFileName))
				return launchFileName;

			launchFileName = Path.Combine (directory, ".vscode", "launch.json");
			if (File.Exists (launchFileName))
				return launchFileName;

			return null;
		}

		static List<PowerShellLaunchConfiguration> ReadFile (string fileName)
		{
			JObject jsonObject = ReadJsonFile (fileName);
			return GetLaunchConfigurations (jsonObject);
		}

		static JObject ReadJsonFile (string fileName)
		{
			using (var fileStream = File.OpenRead (fileName)) {
				using (var reader = new StreamReader (fileStream)) {
					using (var jsonReader = new JsonTextReader (reader)) {
						return JObject.Load (jsonReader);
					}
				}
			}
		}

		static List<PowerShellLaunchConfiguration> GetLaunchConfigurations (JObject jsonObject)
		{
			JsonSerializer serializer = new JsonSerializer();
			var launchConfiguration = (LaunchConfiguration)serializer.Deserialize (
				new JTokenReader (jsonObject),
				typeof(LaunchConfiguration));

			var configurations = new List<PowerShellLaunchConfiguration> ();

			if (launchConfiguration.Configurations != null) {
				foreach (PowerShellLaunchConfiguration configuration in launchConfiguration.Configurations) {
					if (configuration.IsValid ()) {
						configurations.Add (configuration);
					}
				}
			}

			return configurations;
		}
	}
}
