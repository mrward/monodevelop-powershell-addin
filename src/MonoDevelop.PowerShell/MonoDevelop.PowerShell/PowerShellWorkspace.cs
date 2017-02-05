//
// PowerShellWorkspace.cs
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
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

namespace MonoDevelop.PowerShell
{
	class PowerShellWorkspace
	{
		PowerShellSession session;
		PowerShellLaunchConfigurations launchConfigurations = new PowerShellLaunchConfigurations ();

		public void Initialize ()
		{
			IdeApp.Workbench.DocumentOpened += WorkbenchDocumentOpened;
			IdeApp.Workbench.DocumentClosed += WorkbenchDocumentClosed;
			IdeApp.Workspace.SolutionUnloaded += SolutionUnloaded;
		}

		public void Dispose ()
		{
			IdeApp.Workbench.DocumentOpened -= WorkbenchDocumentOpened;
			IdeApp.Workbench.DocumentClosed -= WorkbenchDocumentClosed;
			IdeApp.Workspace.SolutionUnloaded -= SolutionUnloaded;
		}

		static bool IsSupported (Document document)
		{
			return IsSupported (document.FileName);
		}

		public static bool IsSupported (FilePath fileName)
		{
			return fileName.HasExtension (".ps1") ||
				fileName.HasExtension (".psd1") ||
				fileName.HasExtension (".psm1");
		}

		void WorkbenchDocumentOpened (object sender, DocumentEventArgs e)
		{
			if (IsSupported (e.Document))
				PowerShellDocumentOpened (e.Document);
		}

		void WorkbenchDocumentClosed (object sender, DocumentEventArgs e)
		{
			if (IsSupported (e.Document))
				PowerShellDocumentClosed (e.Document);
		}

		void PowerShellDocumentOpened (Document document)
		{
			try {
				PowerShellSession currentSession = GetSession ();
				currentSession.OpenDocument (document);
			} catch (Exception ex) {
				PowerShellLoggingService.LogError ("Error opening PowerShell document.", ex);
			}
		}

		void PowerShellDocumentClosed (Document document)
		{
			if (session != null) {
				if (IsAnyPowerShellDocumentOpen ()) {
					session.CloseDocument (document);
				} else {
					ShutdownSession ();
				}
			}
		}

		public bool IsReady { get; private set; }

		public PowerShellSession GetSession ()
		{
			Runtime.AssertMainThread ();

			if (session == null)
				session = CreateSession ();

			return session;
		}

		bool IsAnyPowerShellDocumentOpen ()
		{
			return GetOpenPowerShellDocuments ().Any ();
		}

		IEnumerable<Document> GetOpenPowerShellDocuments ()
		{
			return IdeApp.Workbench.Documents.Where (document => IsSupported (document.FileName));
		}

		PowerShellSession CreateSession ()
		{
			session = new PowerShellSession ();

			if (PowerShellPathLocator.Exists) {
				session.Started += SessionStarted;
				session.Start ();
			} else {
				PowerShellLoggingService.LogError ("PowerShell is not installed. Please download and install PowerShell from https://github.com/PowerShell/PowerShell");
				PowerShellServices.ErrorReporter.ReportError (GettextCatalog.GetString ("PowerShell is not installed."));
			}
			return session;
		}

		void SolutionUnloaded (object sender, SolutionEventArgs e)
		{
			ShutdownSession ();
		}

		void ShutdownSession ()
		{
			try {
				launchConfigurations = new PowerShellLaunchConfigurations ();

				IsReady = false;
				session.Started -= SessionStarted;
				session.Stop ();
				session = null;
				PowerShellOutputPad.LogView.Clear ();
			} catch (Exception ex) {
				PowerShellLoggingService.LogError ("Error stopping PowerShell session.", ex);
			}
		}

		void SessionStarted (object sender, EventArgs e)
		{
			if (Runtime.IsMainThread) {
				AddOpenPowerShellDocumentsToSession ();
			} else {
				Runtime.RunInMainThread (() => {
					AddOpenPowerShellDocumentsToSession ();
				}).Wait ();
			}
		}

		void AddOpenPowerShellDocumentsToSession ()
		{
			try {
				IsReady = true;
				foreach (Document document in GetOpenPowerShellDocuments ()) {
					session.OpenDocument (document);
				}
			} catch (Exception ex) {
				PowerShellLoggingService.LogError ("Error processing after session started.", ex);
			}
		}

		public IEnumerable<PowerShellLaunchConfiguration> GetLaunchConfigurations (Document document)
		{
			return launchConfigurations.GetConfigurations (document);
		}

		public void SetActiveLaunchConfiguration (PowerShellLaunchConfiguration config, Document document)
		{
			launchConfigurations.SetActiveLaunchConfiguration (config, document);
		}
	}
}
