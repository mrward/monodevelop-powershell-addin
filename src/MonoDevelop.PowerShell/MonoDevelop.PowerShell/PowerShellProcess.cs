//
// PowerShellProcess.cs
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
using System;
using System.Diagnostics;
using System.Text;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core;

namespace MonoDevelop.PowerShell
{
	class PowerShellProcess
	{
		ProcessWrapper process;
		PowerShellStandardOutputParser outputParser;

		public event EventHandler Started;

		void OnStarted ()
		{
			Started?.Invoke (this, new EventArgs ());
		}

		public SessionDetailsMessage SessionDetails {
			get { return outputParser?.SessionDetails; }
		}

		public void Start ()
		{
			outputParser = new PowerShellStandardOutputParser ();
			string powerShellExePath = PowerShellPathLocator.PowerShellPath;

			var argumentBuilder = new PowerShellCommandLineArgumentsBuilder ();
			argumentBuilder.Build ();

			var startInfo = new ProcessStartInfo {
				FileName = powerShellExePath,
				Arguments = argumentBuilder.Arguments,
				CreateNoWindow = true,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				UseShellExecute = false,
				RedirectStandardInput = true,
				StandardOutputEncoding = Encoding.UTF8
			};

			process = new ProcessWrapper ();
			process.StartInfo = startInfo;
			process.OutputStreamChanged += ProcessOutputStreamChanged;
			process.ErrorStreamChanged += ProcessErrorStreamChanged;

			process.Start ();
		}

		void ProcessOutputStreamChanged (object sender, string message)
		{
			try {
				if (outputParser.Parse (message)) {
					OnStarted ();
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Unable to parse PowerShell output.", ex);
			}
		}

		void ProcessErrorStreamChanged (object sender, string message)
		{
			Console.WriteLine ("Error: " + message);
		}

		public void Stop ()
		{
			if (process != null) {
				process.OutputStreamChanged -= ProcessOutputStreamChanged;
				process.ErrorStreamChanged -= ProcessErrorStreamChanged;

				if (!process.HasExited) {
					process.Kill ();
				}
				process = null;
			}
		}
	}
}
