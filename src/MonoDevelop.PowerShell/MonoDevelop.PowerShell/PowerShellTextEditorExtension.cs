//
// PowerShellTextEditorExtension.cs
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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerShell.EditorServices.Protocol.LanguageServer;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.PowerShell
{
	class PowerShellTextEditorExtension : CompletionTextEditorExtension
	{
		PowerShellSession session;
		List<IErrorMarker> errorMarkers = new List<IErrorMarker> ();

		protected override void Initialize ()
		{
			PowerShellServices.Activate ();

			session = PowerShellServices.Workspace.GetSession (Editor.FileName);
			session.OnDiagnostics += OnDiagnostics;

			Editor.TextChanging += TextChanging;

			base.Initialize ();
		}

		public override void Dispose ()
		{
			if (session != null) {
				session.OnDiagnostics -= OnDiagnostics;
				session = null;
			}
			if (Editor != null) {
				Editor.TextChanged -= TextChanging;
			}
			base.Dispose ();
		}

		public override string CompletionLanguage {
			get { return "powershell"; }
		}

		void OnDiagnostics (object sender, DiagnosticsEventArgs e)
		{
			if (e.FileName == null || !(Editor.FileName == e.FileName))
				return;

			Runtime.RunInMainThread (() => {
				ShowDiagnostics (e.Diagnostics);
			});
		}

		void ClearDiagnostics ()
		{
			errorMarkers.ForEach (error => Editor.RemoveMarker (error));
			errorMarkers.Clear ();
		}

		void ShowDiagnostics (Diagnostic[] diagnostics)
		{
			ClearDiagnostics ();

			foreach (Error error in diagnostics.Select (diagnostic => diagnostic.CreateError ())) {
				IErrorMarker marker = TextMarkerFactory.CreateErrorMarker (Editor, error);
				Editor.AddMarker (marker);
				errorMarkers.Add (marker);
			}
		}

		void TextChanging (object sender, TextChangeEventArgs e)
		{
			try {
				session.TextChanged (e, Editor);
			} catch (Exception ex) {
				PowerShellLoggingService.LogError ("TextChanged error.", ex);
			}
		}

		public async override Task<ICompletionDataList> HandleCodeCompletionAsync (
			CodeCompletionContext completionContext,
			char completionChar,
			CancellationToken token = default (CancellationToken))
		{
			if (Editor.EditMode == EditMode.TextLink)
				return null;

			try {
				TextSegment wordSegment = Editor.GetWordRangeAtPosition (completionContext.TriggerLineOffset, Editor.GetLine (completionContext.TriggerLine));

				CompletionItem[] items = await session.GetCompletionItems (completionContext);
				if (items == null || !items.Any ())
					return null;

				var completionList = new CompletionDataList ();
				completionList.TriggerWordLength = 1;
				completionList.AddRange (items.Select (item => new PowerShellCompletionData (item)));

				if (!wordSegment.IsEmpty) {
					completionList.TriggerWordLength = wordSegment.Length;
					completionContext.TriggerLineOffset = wordSegment.Offset;
				}
				return completionList;
			} catch (Exception ex) {
				PowerShellLoggingService.LogError ("HandleCodeCompletionAsync error.", ex);
			}
			return null;
		}

		bool IsWordAtCurrentCaretPosition ()
		{
			TextSegment wordSegment = Editor.GetWordRangeAtPosition (Editor.CaretColumn, Editor.GetLine (Editor.CaretLine));
			return wordSegment.IsEmpty;
		}

		[CommandUpdateHandler (PowerShellCommands.FindReferences)]
		void UpdateFindReferences (CommandInfo info)
		{
			info.Enabled = !IsWordAtCurrentCaretPosition ();
		}

		[CommandHandler (PowerShellCommands.FindReferences)]
		void FindReferences ()
		{
			var finder = new PowerShellReferencesFinder (Editor, session);
			finder.FindReferences (Editor.CaretLocation);
		}

		[CommandUpdateHandler (EditCommands.Rename)]
		void UpdateRename (CommandInfo info)
		{
			info.Enabled = !IsWordAtCurrentCaretPosition ();
		}

		[CommandHandler (EditCommands.Rename)]
		void Rename ()
		{
			var renamer = new PowerShellReferencesFinder (Editor, session);
			renamer.RenameOccurrences (Editor.CaretLocation);
		}
	}
}
