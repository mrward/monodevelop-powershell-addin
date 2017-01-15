//
// PowerShellSession.cs
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
using System.Threading.Tasks;
using Microsoft.PowerShell.EditorServices.Protocol.MessageProtocol.Channel;
using Microsoft.PowerShell.EditorServices.Protocol.LanguageServer;
using Microsoft.PowerShell.EditorServices.Protocol.MessageProtocol;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.PowerShell
{
	class PowerShellSession
	{
		PowerShellProcess process;
		PowerShellLanguageServiceClient languageServiceClient;
		Document documentOpened;

		public PowerShellSession (FilePath fileName)
		{
			this.FileName = fileName;
		}

		public FilePath FileName { get; private set; }

		public int DebugServicePort {
			get {
				if (process?.SessionDetails != null) {
					return process.SessionDetails.DebugServicePort;
				}
				return -1;
			}
		}

		public void Start ()
		{
			process = new PowerShellProcess ();
			process.Started += PowerShellProcessStarted;
			process.Start ();
		}

		public void Stop ()
		{
			if (languageServiceClient != null) {
				languageServiceClient.Stop ();
				languageServiceClient = null;
			}

			if (process != null) {
				process.Stop ();
				process.Started -= PowerShellProcessStarted;
				process = null;
			}
		}

		void PowerShellProcessStarted (object sender, EventArgs e)
		{
			try {
				StartLanguageServiceClient (process.SessionDetails).Wait ();
				OpenDocumentIfNotOpened ().Wait ();
			} catch (Exception ex) {
				PowerShellLoggingService.LogError ("Failed to start language service client.", ex);
				PowerShellServices.ErrorReporter.ReportError ("Could not start PowerShell language service.");
			}
		}

		Task StartLanguageServiceClient (SessionDetailsMessage sessionDetails)
		{
			var channel = new TcpSocketClientChannel (sessionDetails.LanguageServicePort);
			languageServiceClient = new PowerShellLanguageServiceClient (channel, this);
			return languageServiceClient.Start ();
		}

		public void OpenDocument (Document document)
		{
			Runtime.AssertMainThread ();

			if (languageServiceClient == null) {
				documentOpened = document;
			} else {
				documentOpened = null;
				SendOpenDocumentMessage (document);
			}
		}

		void SendOpenDocumentMessage (Document document)
		{
			languageServiceClient.OpenDocument (document);
		}

		Task OpenDocumentIfNotOpened ()
		{
			return Runtime.RunInMainThread (() => {
				Document existingDocument = documentOpened;
				documentOpened = null;
				SendOpenDocumentMessage (existingDocument);
			});
		}

		public event EventHandler<DiagnosticsEventArgs> OnDiagnostics;

		internal Task OnPublishDiagnosticsEvent (PublishDiagnosticsNotification notification, EventContext context)
		{
			OnDiagnostics?.Invoke (this, new DiagnosticsEventArgs (notification));
			return Task.FromResult (true);
		}

		public void TextChanged (TextChangeEventArgs e, TextEditor editor)
		{
			Runtime.AssertMainThread ();

			if (languageServiceClient == null)
				return;

			var message = new DidChangeTextDocumentParams {
				Uri = FileName,
				ContentChanges = new [] { e.CreateTextDocumentChangeEvent (editor) }
			};
			languageServiceClient.SendEvent (DidChangeTextDocumentNotification.Type, message);
		}

		public Task<CompletionItem[]> GetCompletionItems (CodeCompletionContext completionContext)
		{
			if (languageServiceClient == null)
				return Task.FromResult (new CompletionItem[0]);

			var position = CreateTextDocumentPosition (completionContext);
			return languageServiceClient.SendRequest (CompletionRequest.Type, position);
		}

		TextDocumentPosition CreateTextDocumentPosition (CodeCompletionContext completionContext)
		{
			return new TextDocumentPosition {
				Position = new Position {
					Character = completionContext.TriggerLineOffset,
					Line = completionContext.TriggerLine - 1
				},
				Uri = FileName
			};
		}

		public Task<CompletionItem> ResolveCompletionItem (CompletionItem completionItem)
		{
			if (languageServiceClient == null)
				return Task.FromResult (completionItem);

			return languageServiceClient.SendRequest (CompletionResolveRequest.Type, completionItem);
		}

		public Task<SignatureHelp> GetSignatureHelp (CodeCompletionContext completionContext)
		{
			if (languageServiceClient == null)
				return Task.FromResult (new SignatureHelp ());

			var position = CreateTextDocumentPosition (completionContext);
			return languageServiceClient.SendRequest (SignatureHelpRequest.Type, position);
		}

		public Task<Location[]> GetReferences (Position position)
		{
			if (languageServiceClient == null)
				return Task.FromResult (new Location[0]);

			var message = new ReferencesParams {
				Context = new ReferencesContext {
					IncludeDeclaration = true
				},
				Uri = FileName,
				Position = position
			};

			return languageServiceClient.SendRequest (ReferencesRequest.Type, message);
		}

		public void ShowOnlineHelp (string text)
		{
			if (languageServiceClient == null)
				return;

			languageServiceClient.SendRequest (ShowOnlineHelpRequest.Type, text);
		}
	}
}
