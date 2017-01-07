//
// PowerShellThreadBacktrace.cs
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
using System.Threading.Tasks;
using Microsoft.PowerShell.EditorServices.Protocol.DebugAdapter;
using Mono.Debugging.Backend;
using Mono.Debugging.Client;
using MonoDevelop.Core;

using StackFrame = Mono.Debugging.Client.StackFrame;
using PowerShellStackFrame = Microsoft.PowerShell.EditorServices.Protocol.DebugAdapter.StackFrame;

namespace MonoDevelop.PowerShell
{
	class PowerShellThreadBacktrace : IBacktrace
	{
		PowerShellDebuggerSession debugSession;
		StackFrame[] frames;

		public PowerShellThreadBacktrace (PowerShellDebuggerSession debugSession, StackTraceResponseBody body)
		{
			this.debugSession = debugSession;
			frames = body.StackFrames.Select (frame => CreateDebuggerStackFrame (frame)).ToArray ();
		}

		public PowerShellThreadBacktrace (PowerShellDebuggerSession debugSession, StoppedEventBody body)
		{
			this.debugSession = debugSession;
			if (body != null) {
				frames = new [] {
					CreateDebuggerStackFrame (body)
				};
			} else {
				frames = new StackFrame[0];
			}
		}

		static StackFrame CreateDebuggerStackFrame (PowerShellStackFrame frame)
		{
			return new StackFrame (
				frame.Id,
				CreateSourceLocation (frame),
				"PowerShell"
			);
		}

		static SourceLocation CreateSourceLocation (PowerShellStackFrame frame)
		{
			return new SourceLocation (
				frame.Name,
				frame.Source.Path,
				frame.Line,
				1,
				-1,
				-1
			);
		}

		static StackFrame CreateDebuggerStackFrame (StoppedEventBody body)
		{
			return new StackFrame (
				body.ThreadId ?? 1,
				new SourceLocation (body.Source.Name, body.Source.Path, body.Line, 1, -1, -1),
				"PowerShell"
			);
		}

		public int FrameCount {
			get { return frames.Length; }
		}

		public AssemblyLine[] Disassemble (int frameIndex, int firstLine, int count)
		{
			throw new NotImplementedException ();
		}

		public ObjectValue[] GetAllLocals (int frameIndex, EvaluationOptions options)
		{
			StackFrame frame = frames[frameIndex];
			return new [] {
				GetTopLevelScopesObjectValueAsync (frame, options)
			};
		}

		ObjectValue GetTopLevelScopesObjectValueAsync (StackFrame frame, EvaluationOptions options)
		{
			var updater = new PowerShellVariableValueUpdater (debugSession);
			var path = new ObjectPath ("Scope" + frame.Address.ToString ());
			var value = ObjectValue.CreateEvaluating (updater, path, ObjectValueFlags.EvaluatingGroup);
			value.Name = GettextCatalog.GetString ("Scopes");

			updater.UpdateVariableScopes (path, frame.Address);

			return value;
		}

		public ExceptionInfo GetException (int frameIndex, EvaluationOptions options)
		{
			throw new NotImplementedException ();
		}

		public CompletionData GetExpressionCompletionData (int frameIndex, string exp)
		{
			return new CompletionData ();
		}

		public ObjectValue[] GetExpressionValues (int frameIndex, string[] expressions, EvaluationOptions options)
		{
			StackFrame frame = frames[frameIndex];
			return expressions.Select (expression => GetExpressionValue (frame.Address, expression)).ToArray ();
		}

		public ObjectValue[] GetLocalVariables (int frameIndex, EvaluationOptions options)
		{
			throw new NotImplementedException ();
		}

		public ObjectValue[] GetParameters (int frameIndex, EvaluationOptions options)
		{
			return new ObjectValue[0];
		}

		public StackFrame[] GetStackFrames (int firstIndex, int lastIndex)
		{
			return frames.Skip (firstIndex).Take (lastIndex - firstIndex + 1).ToArray ();
		}

		public ObjectValue GetThisReference (int frameIndex, EvaluationOptions options)
		{
			return null;
		}

		public ValidationResult ValidateExpression (int frameIndex, string expression, EvaluationOptions options)
		{
			return new ValidationResult (true, null);
		}

		ObjectValue GetExpressionValue (long frameAddress, string expression)
		{
			var updater = new PowerShellVariableValueUpdater (debugSession);
			var path = new ObjectPath (expression);
			var value = ObjectValue.CreateEvaluating (updater, path, ObjectValueFlags.Object);

			updater.GetExpressionValue (path, frameAddress, expression);

			return value;
		}
	}
}
