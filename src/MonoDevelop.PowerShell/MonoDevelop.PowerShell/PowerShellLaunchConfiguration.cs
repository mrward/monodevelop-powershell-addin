//
// PowerShellLaunchConfiguration.cs
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
using Newtonsoft.Json;
using MonoDevelop.Core;

namespace MonoDevelop.PowerShell
{
	class PowerShellLaunchConfiguration
	{
		public static PowerShellLaunchConfiguration None =
			new PowerShellLaunchConfiguration (GettextCatalog.GetString ("None"), active: true);

		string script;

		public PowerShellLaunchConfiguration (string name, bool active)
		{
			Name = name;
			IsActive = true;
		}

		public PowerShellLaunchConfiguration ()
		{
		}

		[JsonProperty ("type")]
		public string Type { get; set; }

		[JsonProperty ("request")]
		public string Request { get; set; }

		[JsonProperty ("name")]
		public string Name { get; set; }

		[JsonIgnore]
		public bool IsActive { get; set; }

		[JsonProperty ("script")]
		public string Script {
			get {
				return script ?? Program;
			}
			set { script = value; }
		}

		[JsonProperty ("program")]
		public string Program { get; set; }

		[JsonProperty ("args")]
		public string[] Arguments { get; set; }

		[JsonProperty ("cwd")]
		public string WorkingDirectory { get; set; }

		public bool IsValid ()
		{
			return !string.IsNullOrEmpty (Type) &&
				StringComparer.OrdinalIgnoreCase.Equals (Type, "PowerShell") &&
				StringComparer.OrdinalIgnoreCase.Equals (Request, "launch") &&
				!string.IsNullOrEmpty (Name) &&
				!string.IsNullOrEmpty (Script);
		}
	}
}
