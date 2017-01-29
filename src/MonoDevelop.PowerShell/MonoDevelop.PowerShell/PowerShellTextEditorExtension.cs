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
using MonoDevelop.Debugger;
using MonoDevelop.Ide;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Refactoring;

namespace MonoDevelop.PowerShell
{
	class PowerShellTextEditorExtension : CompletionTextEditorExtension, IDebuggerExpressionResolver
	{
		PowerShellSession session;
		FilePath fileName;
		List<IErrorMarker> errorMarkers = new List<IErrorMarker> ();

		protected override void Initialize ()
		{
			PowerShellServices.Activate ();
			fileName = DocumentContext.Name;
			session = PowerShellServices.Workspace.GetSession ();
			session.OnDiagnostics += OnDiagnostics;

			Editor.TextChanging += TextChanging;
			Editor.FileNameChanged += FileNameChanged;

			base.Initialize ();
		}

		public override bool IsValidInContext (DocumentContext context)
		{
			return PowerShellWorkspace.IsSupported (context.Name);
		}

		public override void Dispose ()
		{
			if (session != null) {
				session.OnDiagnostics -= OnDiagnostics;
				session = null;
			}
			if (Editor != null) {
				Editor.TextChanged -= TextChanging;
				Editor.FileNameChanged -= FileNameChanged;
			}
			base.Dispose ();
		}

		public override string CompletionLanguage {
			get { return "powershell"; }
		}

		void OnDiagnostics (object sender, DiagnosticsEventArgs e)
		{
			if (e.FileName == null || !(fileName == e.FileName))
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
				session.TextChanged (fileName, e, Editor);
			} catch (Exception ex) {
				PowerShellLoggingService.LogError ("TextChanged error.", ex);
			}
		}

		void FileNameChanged (object sender, EventArgs e)
		{
			try {
				if (Editor.FileName == fileName)
					return;

				session.FileNameChanged (fileName, Editor.FileName, Editor.Text);
				fileName = Editor.FileName;
			} catch (Exception ex) {
				PowerShellLoggingService.LogError ("FileNameChanged error.", ex);
			}
		}

		public async override Task<ICompletionDataList> HandleCodeCompletionAsync (
			CodeCompletionContext completionContext,
			char completionChar,
			CancellationToken token = default (CancellationToken))
		{
			if (Editor.EditMode == EditMode.TextLink)
				return null;

			if (completionChar == ' ')
				return null;

			try {
				TextSegment wordSegment = Editor.GetWordRangeAtPosition (completionContext.TriggerLineOffset, Editor.GetLine (completionContext.TriggerLine));

				CompletionItem[] items = await session.GetCompletionItems (fileName, completionContext);
				if (items == null || !items.Any ())
					return null;

				var completionList = new CompletionDataList ();
				completionList.AddRange (items.Select (item => new PowerShellCompletionData (session, item)));

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

		[CommandUpdateHandler (RefactoryCommands.FindReferences)]
		void UpdateFindReferences (CommandInfo info)
		{
			info.Enabled = !IsWordAtCurrentCaretPosition ();
		}

		[CommandHandler (RefactoryCommands.FindReferences)]
		void FindReferences ()
		{
			var finder = new PowerShellReferencesFinder (Editor, session);
			finder.FindReferences (fileName, Editor.CaretLocation);
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
			renamer.RenameOccurrences (fileName, Editor.CaretLocation);
		}

		public override Task<ParameterHintingResult> HandleParameterCompletionAsync (
			CodeCompletionContext completionContext,
			char completionChar,
			CancellationToken token = default (CancellationToken))
		{
			if (completionChar == ' ') {
				try {
					return GetParameterCompletionAsync (completionContext, token);
				} catch (Exception ex) {
					PowerShellLoggingService.LogError ("HandleParameterCompletionAsync error.", ex);
				}
			}
			return base.HandleParameterCompletionAsync (completionContext, completionChar, token);
		}

		async Task<ParameterHintingResult> GetParameterCompletionAsync (
			CodeCompletionContext completionContext,
			CancellationToken token)
		{
			SignatureHelp signatureHelp = await session.GetSignatureHelp (fileName, completionContext);
			if (signatureHelp == null || !signatureHelp.Signatures.Any ()) {
				return ParameterHintingResult.Empty;
			}

			var parameterDataItems = signatureHelp
				.Signatures
				.Select (signature => new PowerShellParameterHintingData (signature) as ParameterHintingData)
				.ToList ();

			return new ParameterHintingResult (parameterDataItems, completionContext.TriggerOffset);
		}

		[CommandHandler (HelpCommands.Help)]
		void OnHelp ()
		{
			string word = GetWordAtCaret ();
			session.ShowOnlineHelp (word);
		}

		string GetWordAtCaret ()
		{
			TextSegment wordSegment = Editor.GetWordRangeAtPosition (Editor.CaretColumn, Editor.GetLine (Editor.CaretLine));
			if (!wordSegment.IsEmpty) {
				string line = Editor.GetLineText (Editor.CaretLine);
				return line.Substring (wordSegment.Offset, wordSegment.Length);
			}
			return null;
		}

		[CommandUpdateHandler (DebugCommands.Debug)]
		void OnDebug (CommandInfo info)
		{
			if (DebuggingService.IsPaused) {
				info.Enabled = true;
				info.Text = GettextCatalog.GetString ("_Continue Debugging");
				return;
			}

			info.Enabled = !DebuggingService.IsDebugging &&
				PowerShellServices.Workspace.IsReady;
		}

		[CommandHandler (DebugCommands.Debug)]
		void OnDebug ()
		{
			if (DebuggingService.IsPaused) {
				DebuggingService.Resume ();
				return;
			}

			var debugOperation = IdeApp.ProjectOperations.DebugFile (Editor.FileName);
			IdeApp.ProjectOperations.CurrentRunOperation = debugOperation;
		}

		[CommandHandler (ProjectCommands.Stop)]
		void OnStop ()
		{
			DebuggingService.Stop ();
		}

		[CommandUpdateHandler (ProjectCommands.Run)]
		void OnUpdateRun (CommandInfo info)
		{
			if (!IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted ||
				!PowerShellServices.Workspace.IsReady) {
				info.Enabled = false;
			}

			info.Enabled = IdeApp.ProjectOperations.CanExecuteFile (fileName, Runtime.ProcessService.DefaultExecutionHandler);
		}

		[CommandHandler (ProjectCommands.Run)]
		void OnRun ()
		{
			var runOperation = IdeApp.ProjectOperations.ExecuteFile (fileName, Runtime.ProcessService.DefaultExecutionHandler);
			IdeApp.ProjectOperations.CurrentRunOperation = runOperation;
		}

		public Task<DebugDataTipInfo> ResolveExpressionAsync (
			IReadonlyTextDocument editor,
			DocumentContext doc,
			int offset,
			CancellationToken cancellationToken)
		{
			DocumentLocation location = editor.OffsetToLocation (offset);
			IDocumentLine line = editor.GetLine (location.Line);
			string text = editor.GetLineText (line);

			TextSegment wordSegment = TextEditorExtensions.GetWordRangeAtPosition (text, location.Column);
			if (wordSegment.IsEmpty) {
				return Task.FromResult (new DebugDataTipInfo ());
			}

			string expression = text.Substring (wordSegment.Offset, wordSegment.Length);
			if (!expression.StartsWith ("$", StringComparison.OrdinalIgnoreCase)) {
				return Task.FromResult (new DebugDataTipInfo ());
			}

			var span = new Microsoft.CodeAnalysis.Text.TextSpan (wordSegment.Offset, wordSegment.Length);
			var tipInfo = new DebugDataTipInfo (span, expression);

			return Task.FromResult (tipInfo);
		}

		[CommandUpdateHandler (DebugCommands.DebugApplication)]
		void OnUpdateDebugApplication (CommandInfo info)
		{
			info.Enabled = !DebuggingService.IsDebugging &&
				PowerShellServices.Workspace.IsReady;
		}

		[CommandHandler (DebugCommands.DebugApplication)]
		void OnDebugApplication ()
		{
			var handler = new DebugApplicationHandler ();
			handler.DebugInfo = DebugApplicationInfo;
			if (handler.Run ())
				DebugApplicationInfo = handler.DebugInfo;
		}

		DebugApplicationInfo debugApplicationInfo;

		DebugApplicationInfo DebugApplicationInfo {
			get {
				if (debugApplicationInfo == null) {
					debugApplicationInfo = new DebugApplicationInfo {
						SelectedFile = fileName,
						WorkingDirectory = fileName.ParentDirectory
					};
				}

				return debugApplicationInfo;
			}
			set { debugApplicationInfo = value; }
		}
	}
}
