//
// PowerShellExecutionCommand.cs
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
using System.IO;
using System.Text;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.PowerShell
{
	class PowerShellExecutionCommand : NativeExecutionCommand
	{
		public PowerShellExecutionCommand (string scriptFileName, params string[] parameters)
		{
			Arguments = GetArguments (scriptFileName, parameters);
			PowerShellArguments = Arguments;
			Command = PowerShellPathLocator.PowerShellPath;
			ScriptFileName = scriptFileName;
			WorkingDirectory = Path.GetDirectoryName (scriptFileName);
		}

		string GetArguments (string scriptFileName, params string[] parameters)
		{
			var arguments = new StringBuilder ();

			if (Platform.IsWindows) {
				arguments.Append ("-ExecutionPolicy Unrestricted ");
			}

			arguments.Append ("-Command '& \"" + scriptFileName + "\"'");
			foreach (string parameter in parameters) {
				arguments.Append (' ');
				arguments.Append (parameter);
			}

			return arguments.ToString ();
		}

		public string ScriptFileName { get; private set; }
		public string PowerShellArguments { get; private set; }

		/// <summary>
		/// If the PowerShellArguments have been overridden in the Arguments
		/// then use these arguments when debugging the PowerShell script. These
		/// will be passed to the script as parameters.
		/// </summary>
		public string DebuggerArguments {
			get {
				if (Arguments != PowerShellArguments)
					return Arguments;

				return null;
			}
		}

		/// <summary>
		/// Returns false if Run - Debug Application has been used to change the
		/// arguments.
		/// </summary>
		public bool UsingOriginalArguments ()
		{
			return Arguments == PowerShellArguments;
		}

		public static bool CanExecute (string path)
		{
			if (!PowerShellPathLocator.Exists)
				return false;

			if (path == null)
				return false;

			string extension = Path.GetExtension (path);
			return StringComparer.OrdinalIgnoreCase.Equals (extension, ".ps1");
		}
	}
}
