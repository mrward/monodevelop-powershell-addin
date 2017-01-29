//
// DebugApplicationInfo.cs
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

using System.Collections.Generic;
using MonoDevelop.Debugger;

namespace MonoDevelop.PowerShell
{
	class DebugApplicationInfo
	{
		public DebugApplicationInfo (DebugApplicationDialog dialog)
		{
			Arguments = dialog.Arguments;
			EnvironmentVariables = dialog.EnvironmentVariables;
			SelectedFile = dialog.SelectedFile;
			WorkingDirectory = dialog.WorkingDirectory;
		}

		public DebugApplicationInfo ()
		{
			Arguments = string.Empty;
			EnvironmentVariables = new Dictionary<string, string> ();
			SelectedFile = string.Empty;
			WorkingDirectory = string.Empty;
		}

		public string Arguments { get; set; }
		public Dictionary<string,string> EnvironmentVariables { get; set; }
		public string SelectedFile { get; set; }
		public string WorkingDirectory { get; set; }
	}
}
