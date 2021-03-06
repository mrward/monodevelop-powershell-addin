﻿//
// TextChangeEventArgsExtensions.cs
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

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerShell.EditorServices.Protocol.LanguageServer;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.PowerShell
{
	static class TextChangeEventArgsExtensions
	{
		public static IEnumerable<TextDocumentChangeEvent> CreateTextDocumentChangeEvents (this TextChangeEventArgs e, TextEditor editor)
		{
			return e.TextChanges.Select (textChange => CreateTextDocumentChangeEvent (textChange, editor));
		}

		public static TextDocumentChangeEvent CreateTextDocumentChangeEvent (this TextChange textChange, TextEditor editor)
		{
			int startOffset = textChange.Offset;
			int endOffset = textChange.Offset + textChange.RemovalLength;

			var startLocation = editor.OffsetToLocation (startOffset);
			var endLocation = editor.OffsetToLocation (endOffset);

			return new TextDocumentChangeEvent {
				Range = new Range {
					Start = startLocation.CreatePosition (),
					End = endLocation.CreatePosition ()
				},
				RangeLength = endOffset - startOffset,
				Text = textChange.InsertedText.Text
			};
		}

		public static Position CreatePosition (this DocumentLocation location)
		{
			return new Position {
				Character = location.Column - 1,
				Line = location.Line - 1
			};
		}
	}
}
