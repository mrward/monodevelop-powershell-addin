//
// PowerShellLanguageServiceClient.cs
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

using System.Threading.Tasks;
using Microsoft.PowerShell.EditorServices.Protocol.Client;
using Microsoft.PowerShell.EditorServices.Protocol.LanguageServer;
using Microsoft.PowerShell.EditorServices.Protocol.MessageProtocol.Channel;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.PowerShell
{
	class PowerShellLanguageServiceClient : LanguageServiceClient
	{
		PowerShellSession session;
		DidOpenTextDocumentNotification openTextDocumentNotification;
		bool connected;

		public PowerShellLanguageServiceClient (ChannelBase clientChannel, PowerShellSession session)
			: base (clientChannel)
		{
			this.session = session;
		}

		public Task OpenDocument (Document document)
		{
			DidOpenTextDocumentNotification message = CreateOpenTextDocumentNotification (document);
			return OpenDocument (message);
		}

		public Task OpenDocument (DidOpenTextDocumentNotification message)
		{
			if (!connected) {
				lock (session) {
					if (!connected) {
						openTextDocumentNotification = message;
						return Task.FromResult (true);
					}
				}
			}

			return SendEvent (DidOpenTextDocumentNotification.Type, message);
		}

		DidOpenTextDocumentNotification CreateOpenTextDocumentNotification (Document document)
		{
			return new DidOpenTextDocumentNotification () {
				Uri = document.FileName,
				Text = document.Editor.Text
			};
		}

		protected override Task Initialize ()
		{
			SetEventHandler (PublishDiagnosticsNotification.Type, session.OnPublishDiagnosticsEvent);

			return Task.FromResult (true);
		}

		protected override Task OnConnect ()
		{
			DidOpenTextDocumentNotification message = null;

			lock (session) {
				message = openTextDocumentNotification;
				openTextDocumentNotification = null;
			}

			connected = true;

			if (message != null) {
				return SendEvent (DidOpenTextDocumentNotification.Type, message);
			}

			return Task.FromResult (true);
		}
	}
}
