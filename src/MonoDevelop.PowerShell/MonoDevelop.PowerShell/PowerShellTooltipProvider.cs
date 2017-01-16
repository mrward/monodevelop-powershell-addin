//
// PowerShellTooltipProvider.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerShell.EditorServices.Protocol.LanguageServer;
using MonoDevelop.Components;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor;
using Xwt;

namespace MonoDevelop.PowerShell
{
	class PowerShellTooltipProvider : TooltipProvider
	{
		public override async Task<TooltipItem> GetItem (
			TextEditor editor,
			DocumentContext ctx,
			int offset,
			CancellationToken token = default (CancellationToken))
		{
			try {
				PowerShellSession session = PowerShellServices.Workspace.GetSession (editor.FileName);

				DocumentLocation location = editor.OffsetToLocation (offset);
				Hover result = await session.Hover (location);
				return CreateTooltipItem (editor, result);

			} catch (Exception ex) {
				PowerShellLoggingService.LogError ("TooltipProvider error.", ex);
			}

			return null;
		}

		TooltipItem CreateTooltipItem (TextEditor editor, Hover result)
		{
			if (result?.Contents?.Length > 0) {
				return new TooltipItem (
					CreateTooltipInformation (result.Contents),
					GetTextSegment (editor, result.Range)
				);
			}

			return null;
		}

		TooltipInformation CreateTooltipInformation (MarkedString[] contents)
		{
			var tooltipInfo = new TooltipInformation ();
			tooltipInfo.SummaryMarkup = EscapeMarkup (GetSummaryMarkup (contents));
			tooltipInfo.SignatureMarkup = EscapeMarkup (GetSignatureMarkup (contents));
			return tooltipInfo;
		}

		static string EscapeMarkup (string text)
		{
			return GLib.Markup.EscapeText (text ?? string.Empty);
		}

		static string GetSummaryMarkup (MarkedString[] contents)
		{
			if (contents.Length > 1)
				return contents[1].Value;

			return string.Empty;
		}

		static string GetSignatureMarkup (MarkedString[] contents)
		{
			if (contents.Length > 0)
				return contents[0].Value;

			return string.Empty;
		}

		ISegment GetTextSegment (TextEditor editor, Range? range)
		{
			if (range.HasValue) {
				int startOffset = editor.PositionToOffset (range.Value.Start);
				int endOffset = editor.PositionToOffset (range.Value.End);
				return new TextSegment (startOffset, endOffset - startOffset);
			}

			return TextSegment.Invalid;
		}

		public override Control CreateTooltipWindow (
			TextEditor editor,
			DocumentContext ctx,
			TooltipItem item,
			int offset,
			ModifierKeys modifierState)
		{
			var doc = ctx;
			if (doc == null)
				return null;

			var result = new TooltipInformationWindow ();
			result.ShowArrow = true;
			result.AddOverload ((TooltipInformation)item.Item);
			result.RepositionWindow ();
			return result;
		}
	}
}
