﻿//
// PowerShellCompletionData.cs
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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerShell.EditorServices.Protocol.LanguageServer;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.PowerShell
{
	class PowerShellCompletionData : CompletionData
	{
		PowerShellSession session;

		public PowerShellCompletionData (PowerShellSession session, CompletionItem item)
		{
			this.session = session;
			CompletionItem = item;
			CompletionText = item.InsertText;

			Icon = GetIcon (item);
		}

		public CompletionItem CompletionItem { get; private set; }

		/// <summary>
		/// Returns InsertText instead of Label so parameters are handled (e.g. -path).
		/// VSCode shows only the parameter name without the '-'. It is simpler to
		/// show the '-' as part of the text in the code completion list in MonoDevelop
		/// than to support it as VSCode does.
		/// </summary>
		public override string DisplayText {
			get { return CompletionItem.InsertText; }
			set { base.DisplayText = value; }
		}

		public override string Description {
			get {
				return GLib.Markup.EscapeText (GetDescription ());
			}
			set { base.Description = value; }
		}

		string GetDescription ()
		{
			if (!string.IsNullOrEmpty (CompletionItem.Documentation)) {
				return CompletionItem.Documentation;
			}

			return CompletionItem.Detail ?? string.Empty;
		}

		static IconId GetIcon (CompletionItem item)
		{
			if (!item.Kind.HasValue)
				return null;

			switch (item.Kind) {
				case CompletionItemKind.Property:
				return Stock.Property;

				case CompletionItemKind.Constructor:
				case CompletionItemKind.Method:
				case CompletionItemKind.Function:
				return Stock.Method;

				case CompletionItemKind.Text:
				case CompletionItemKind.Keyword:
				return Stock.Literal;

				case CompletionItemKind.Class:
				return Stock.Class;

				case CompletionItemKind.Field:
				return Stock.PrivateField;

				case CompletionItemKind.Interface:
				return Stock.Interface;

				case CompletionItemKind.Module:
				return Stock.NameSpace;

				case CompletionItemKind.Enum:
				return Stock.Enum;

				case CompletionItemKind.Variable:
				return "md-variable";

				case CompletionItemKind.File:
				return Stock.EmptyFileIcon;

				default:
				return null;
			}
		}

		public override async Task<TooltipInformation> CreateTooltipInformation (bool smartWrap, CancellationToken cancelToken)
		{
			try {
				if (CompletionItem.Kind == CompletionItemKind.Function) {
					CompletionItem resolvedCompletionItem = await session.ResolveCompletionItem (CompletionItem);
					if (resolvedCompletionItem != null) {
						CompletionItem = resolvedCompletionItem;
					}
				}
			} catch (Exception ex) {
				PowerShellLoggingService.LogError ("PowerShellCompletionData.CreateTooltip error", ex);
			}
			return await base.CreateTooltipInformation (smartWrap, cancelToken);
		}
	}
}
