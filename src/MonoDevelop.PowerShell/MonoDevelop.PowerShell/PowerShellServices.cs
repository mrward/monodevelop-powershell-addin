//
// PowerShellServices.cs
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
using MonoDevelop.Core;

namespace MonoDevelop.PowerShell
{
	class PowerShellServices
	{
		static bool active;
		static PowerShellWorkspace workspace;
		static readonly StatusBarErrorReporter errorReporter = new StatusBarErrorReporter ();

		public static void Activate ()
		{
			if (active)
				return;

			try {
				workspace = new PowerShellWorkspace ();
				workspace.Initialize ();

				PowerShellLoggingService.Initialize ();

				active = true;
			} catch (Exception ex) {
				PowerShellLoggingService.LogError ("PowerShellServices activation error.", ex);
				errorReporter.ReportError (GettextCatalog.GetString ("Could not run PowerShell editor services."));
			}
		}

		public static PowerShellWorkspace Workspace {
			get { return workspace; }
		}

		public static StatusBarErrorReporter ErrorReporter {
			get { return errorReporter; }
		}
	}
}
