//
// DebugApplicationHandler.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2005 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.PowerShell;

namespace MonoDevelop.Debugger
{
	class DebugApplicationHandler
	{
		public DebugApplicationInfo DebugInfo { get; set; }

		public bool Run ()
		{
			var dlg = new DebugApplicationDialog ();
			dlg.Title = GettextCatalog.GetString ("Debug PowerShell Script");
			if (DebugInfo != null) {
				dlg.Arguments = DebugInfo.Arguments;
				dlg.EnvironmentVariables = DebugInfo.EnvironmentVariables;
				dlg.SelectedFile = DebugInfo.SelectedFile;
				dlg.WorkingDirectory = DebugInfo.WorkingDirectory;
			}

			try {
				bool isOK;

				while ((isOK = (MessageService.RunCustomDialog (dlg) == (int)Gtk.ResponseType.Ok)) 
					&& !Validate (dlg));

				if (isOK) {
					DebugInfo = new DebugApplicationInfo (dlg);
					IdeApp.ProjectOperations.DebugApplication (dlg.SelectedFile, dlg.Arguments, dlg.WorkingDirectory, dlg.EnvironmentVariables);
				}

				return isOK;

			} finally {
				dlg.Destroy ();
				dlg.Dispose ();
			}
		}

		bool Validate (DebugApplicationDialog dlg)
		{
			if (String.IsNullOrEmpty (dlg.SelectedFile)) {
				MessageService.ShowError (GettextCatalog.GetString ("Please select the application to debug"));
				return false;
			}

			if (!File.Exists (dlg.SelectedFile)) {
				MessageService.ShowError (GettextCatalog.GetString ("The file '{0}' does not exist", dlg.SelectedFile));
				return false;
			}

			if (!IdeApp.ProjectOperations.CanDebugFile (dlg.SelectedFile)) {
				MessageService.ShowError (GettextCatalog.GetString ("The file '{0}' can't be debugged", dlg.SelectedFile));
				return false;
			}

			return true;
		}
	}
}
