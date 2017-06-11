//
// PowerShellReferencesFinder.cs
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
using System.Threading.Tasks;
using System.Linq;
using Microsoft.PowerShell.EditorServices.Protocol.LanguageServer;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.FindInFiles;

namespace MonoDevelop.PowerShell
{
	class PowerShellReferencesFinder
	{
		TextEditor editor;
		PowerShellSession session;

		public PowerShellReferencesFinder (TextEditor editor, PowerShellSession session)
		{
			this.editor = editor;
			this.session = session;
		}

		public async Task FindReferences (FilePath fileName, DocumentLocation location)
		{
			try {
				using (var monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true)) {
					Location[] locations = await session.GetReferences (fileName, location.CreatePosition ());

					List<SearchResult> references = locations.Select (CreateSearchResult).ToList ();
					monitor.ReportResults (references);
				}
			} catch (Exception ex) {
				PowerShellLoggingService.LogError ("FindReferences error.", ex);
			}
		}

		SearchResult CreateSearchResult (Location location)
		{
			int startOffset = editor.PositionToOffset (location.Range.Start);
			int endOffset = editor.PositionToOffset (location.Range.End);
			var provider = new FileProvider (new FilePath (location.Uri), null, startOffset, endOffset);
			return new SearchResult (provider, startOffset, endOffset - startOffset);
		}

		public async Task RenameOccurrences (FilePath fileName, DocumentLocation location)
		{
			try {
				Location[] locations = await session.GetReferences (fileName, location.CreatePosition ());
				List<SearchResult> references = locations.Select (CreateSearchResult).ToList ();
				StartTextEditorRename (references);
			} catch (Exception ex) {
				PowerShellLoggingService.LogError ("FindReferences error.", ex);
			}
		}

		void StartTextEditorRename (IEnumerable<SearchResult> references)
		{
			var oldVersion = editor.Version;

			var links = CreateTextLinks (references);

			editor.StartTextLinkMode (new TextLinkModeOptions (links, (arg) => {
				if (!arg.Success) {
					List<TextChangeEventArgs> eventArgs = editor.Version.GetChangesTo (oldVersion).ToList ();
					foreach (TextChangeEventArgs eventArg in eventArgs) {
						foreach (TextChange textChange in eventArg.TextChanges) {
							editor.ReplaceText (textChange.Offset, textChange.RemovalLength, textChange.InsertedText);
						}
					}
				}
			}));
		}

		List<TextLink> CreateTextLinks (IEnumerable<SearchResult> references)
		{
			var links = new List<TextLink> ();
			var link = new TextLink ("name");

			foreach (SearchResult reference in references) {
				var segment = new TextSegment (reference.Offset, reference.Length);
				if (segment.Offset <= editor.CaretOffset && editor.CaretOffset <= segment.EndOffset) {
					link.Links.Insert (0, segment);
				} else {
					link.AddLink (segment);
				}
			}
			links.Add (link);

			return links;
		}
	}
}
