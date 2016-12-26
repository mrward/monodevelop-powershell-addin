﻿//
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

namespace MonoDevelop.PowerShell
{
	class PowerShellWorkspace
	{
		List<PowerShellSession> sessions = new List<PowerShellSession> ();

		public void Initialize ()
		{
			IdeApp.Workbench.DocumentOpened += WorkbenchDocumentOpened;
			IdeApp.Workbench.DocumentClosed += WorkbenchDocumentClosed;
		}

		public void Dispose ()
		{
			IdeApp.Workbench.DocumentOpened -= WorkbenchDocumentOpened;
			IdeApp.Workbench.DocumentClosed -= WorkbenchDocumentClosed;
		}

		static bool IsSupported (Document document)
		{
			return IsSupported (document.FileName);
		}

		static bool IsSupported (FilePath fileName)
		{
			return fileName.HasExtension (".ps1") ||
				fileName.HasExtension (".psd1") ||
				fileName.HasExtension (".psm1");
		}

		void WorkbenchDocumentOpened (object sender, DocumentEventArgs e)
		{
			if (IsSupported (e.Document))
				WorkbenchDocumentOpened (e.Document);
		}

		void WorkbenchDocumentClosed (object sender, DocumentEventArgs e)
		{
			if (IsSupported (e.Document))
				WorkbenchDocumentClosed (e.Document);
		}

		void WorkbenchDocumentOpened (Document document)
		{
			//PowerShellSession session = GetSession (document);
			//session.OpenDocument (document);
		}

		void WorkbenchDocumentClosed (Document document)
		{
			try {
				PowerShellSession session = GetSession (document);
				session.Stop ();
			} catch (Exception ex) {
				LoggingService.LogError ("Error stopping PowerShell session.", ex);
			}
		}

		PowerShellSession GetSession (Document document)
		{
			return GetSession (document.FileName);
		}

		public PowerShellSession GetSession (FilePath fileName)
		{
			PowerShellSession session = sessions.FirstOrDefault (currentSession => currentSession.FileName == fileName);
			if (session != null) {
				return session;
			}

			session = CreateSession (fileName);
			sessions.Add (session);

			return session;
		}

		PowerShellSession CreateSession (FilePath fileName)
		{
			var session = new PowerShellSession (fileName);
			session.Start ();
			return session;
		}
	}
}
