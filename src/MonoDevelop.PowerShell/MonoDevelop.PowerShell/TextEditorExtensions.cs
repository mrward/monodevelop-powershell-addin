//
// TextEditorExtensions.cs
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
using System.Text.RegularExpressions;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.PowerShell
{
	static class TextEditorExtensions
	{
		public static TextSegment GetWordRangeAtPosition (this TextEditor editor, int column, IDocumentLine line)
		{
			string text = editor.GetLineText (line);

			string pattern = @"(-?\d*\.\d\w*)|([^\`\~\!\@\#\%\^\&\*\(\)\=\+\[\{\]\}\\\|\;\'\""\,\.\<\>\/\?\s]+)";

			MatchCollection matches = Regex.Matches (text, pattern, RegexOptions.Singleline);

			foreach (Match match in matches) {
				string word = match.Value;
				int endWord = match.Index + match.Length;
				int startWord = match.Index;

				int startColumn = startWord + 1;
				int endColumn = endWord + 1;

				if (startColumn <= column && column <= endColumn) {
					Console.WriteLine ("GetWordRangeAtPosition: '{0}' start={1},length={2}", word, startColumn, word.Length);
					return new TextSegment (startColumn, word.Length);
				}
			}

			return new TextSegment ();
		}
	}
}
