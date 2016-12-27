//
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

using Microsoft.PowerShell.EditorServices.Protocol.LanguageServer;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.PowerShell
{
	static class TextChangeEventArgsExtensions
	{
		public static TextDocumentChangeEvent CreateTextDocumentChangeEvent (this TextChangeEventArgs e, TextEditor editor)
		{
			int startOffset = e.Offset;
			int endOffset = e.Offset + e.RemovalLength;

			var startLocation = editor.OffsetToLocation (startOffset);
			var endLocation = editor.OffsetToLocation (endOffset);

			return new TextDocumentChangeEvent {
				Range = new Range {
					Start = startLocation.CreatePosition (),
					End = endLocation.CreatePosition ()
				},
				RangeLength = endOffset - startOffset,
				Text = e.InsertedText.Text
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
