//
// PowerShellDebuggerSession.cs
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
using Microsoft.PowerShell.EditorServices.Protocol.MessageProtocol.Channel;
using Mono.Debugging.Client;
using MonoDevelop.Core;

using Breakpoint = Mono.Debugging.Client.Breakpoint;

namespace MonoDevelop.PowerShell
{
	class PowerShellDebuggerSession : DebuggerSession
	{
		PowerShellSession session;
		PowerShellDebugAdapterClient debugClient;
		Dictionary<BreakEvent, BreakEventInfo> breakpoints = new Dictionary<BreakEvent, BreakEventInfo> ();
		bool breakpointsSetBeforeScriptLaunch;
		StoppedEventBody currentStoppedEventBody;
		int debugCommandTimeout = 5000;

		protected override void OnAttachToProcess (long processId)
		{
			throw new NotImplementedException ();
		}

		protected override void OnContinue ()
		{
			debugClient.SendRequest (ContinueRequest.Type, null).Wait (debugCommandTimeout);
		}

		protected override void OnDetach ()
		{
			throw new NotImplementedException ();
		}

		protected override void OnEnableBreakEvent (BreakEventInfo eventInfo, bool enable)
		{
			throw new NotImplementedException ();
		}

		protected override async void OnExit ()
		{
			HasExited = true;

			try {
				var client = debugClient;
				debugClient = null;

				if (client != null) {
					await client.SendRequest (DisconnectRequest.Type, null);
					await client.Stop ();
				}
			} catch (Exception ex) {
				PowerShellLoggingService.LogError ("PowerShellDebuggerSession.OnStop error.", ex);
			}
		}

		protected override void OnFinish ()
		{
			debugClient.SendRequest (StepOutRequest.Type, null).Wait (debugCommandTimeout);
		}

		protected override ProcessInfo[] OnGetProcesses ()
		{
			return new [] {
				new ProcessInfo (1, "powershell")
			};
		}

		protected override Backtrace OnGetThreadBacktrace (long processId, long threadId)
		{
			Task<StackTraceResponseBody> task = GetStackTrace ();
			if (task.Wait (1000)) {
				StackTraceResponseBody body = task.Result;
				return new Backtrace (new PowerShellThreadBacktrace (this, body));
			}

			return new Backtrace (new PowerShellThreadBacktrace (this, currentStoppedEventBody));
		}

		Task<StackTraceResponseBody> GetStackTrace ()
		{
			var message = new StackTraceRequestArguments ();
			return debugClient.SendRequest (StackTraceRequest.Type, message);
		}

		protected override ThreadInfo[] OnGetThreads (long processId)
		{
			return new [] {
				new ThreadInfo (processId, 1, "Main Thread", string.Empty)
			};
		}

		protected override BreakEventInfo OnInsertBreakEvent (BreakEvent breakEvent)
		{
			var breakpoint = breakEvent as Breakpoint;
			if (breakpoint != null) {
				var breakEventInfo = new BreakEventInfo ();
				breakpoints.Add (breakpoint, breakEventInfo);

				if (breakpointsSetBeforeScriptLaunch) {
					UpdateBreakpoints (breakpoint.FileName);
				}

				return breakEventInfo;
			}

			return null;
		}

		SourceBreakpoint CreateSourceBreakpoint (Breakpoint breakpoint)
		{
			return new SourceBreakpoint {
				Condition = breakpoint.ConditionExpression,
				Column = breakpoint.Column,
				Line = breakpoint.Line
			};
		}

		Task UpdateBreakpoints (string fileName)
		{
			var breakpointsForFile = GetBreakpointsForFile (fileName).ToList ();
			return UpdateBreakpoints (fileName, breakpointsForFile);
		}

		async Task UpdateBreakpoints (string fileName, IEnumerable<Breakpoint> breakpointsForFile)
		{
			var message = new SetBreakpointsRequestArguments {
				Source = new Source {
					Path = fileName
				},
				Breakpoints = breakpointsForFile.Select (CreateSourceBreakpoint).ToArray ()
			};
			await debugClient.SendRequest (SetBreakpointsRequest.Type, message);
		}

		IEnumerable<Breakpoint> GetBreakpointsForFile (string fileName)
		{
			foreach (var keyValuePair in breakpoints) {
				var breakpoint = keyValuePair.Key as Breakpoint;
				if (breakpoint != null) {
					if (breakpoint.FileName == fileName) {
						yield return breakpoint;
					}
				}
			}
		}

		protected override void OnNextInstruction ()
		{
			OnNextLine ();
		}

		protected override void OnNextLine ()
		{
			debugClient.SendRequest (NextRequest.Type, null).Wait (debugCommandTimeout);
		}

		protected override void OnRemoveBreakEvent (BreakEventInfo eventInfo)
		{
		}

		protected override async void OnRun (DebuggerStartInfo startInfo)
		{
			if (HasExited)
				throw new UserException (GettextCatalog.GetString ("PowerShell debugger has exited."));

			try {
				session = await Runtime.RunInMainThread (() => {
					return PowerShellServices.Workspace.GetSession (startInfo.Command);
				});

				var channel = new TcpSocketClientChannel (session.DebugServicePort);
				debugClient = new PowerShellDebugAdapterClient (channel, this);
				await debugClient.Start ();
				await debugClient.WaitForConnection ();

				OnStarted ();

				await UpdateBreakpoints ();
				await debugClient.LaunchScript (startInfo.Command);
			} catch (Exception ex) {
				PowerShellLoggingService.LogError ("PowerShellDebuggerSession.OnRun error.", ex);
			}
		}

		async Task UpdateBreakpoints ()
		{
			foreach (var grouping in breakpoints.Keys.OfType<Breakpoint> ()
				.GroupBy (breakEvent => GetFileName (breakEvent))) {

				await UpdateBreakpoints (grouping.Key, grouping);
			}

			breakpointsSetBeforeScriptLaunch = true;
		}

		static string GetFileName (BreakEvent breakEvent)
		{
			var breakpoint = breakEvent as Breakpoint;
			if (breakpoint != null) {
				return breakpoint.FileName;
			}
			return string.Empty;
		}

		protected override void OnSetActiveThread (long processId, long threadId)
		{
		}

		protected override void OnStepInstruction ()
		{
			OnStepLine ();
		}

		protected override void OnStepLine ()
		{
			debugClient.SendRequest (StepInRequest.Type, null).Wait (debugCommandTimeout);
		}

		protected override void OnStop ()
		{
			debugClient.SendRequest (PauseRequest.Type, null).Wait (debugCommandTimeout);
		}

		protected override void OnUpdateBreakEvent (BreakEventInfo eventInfo)
		{
			throw new NotImplementedException ();
		}

		internal void OnTargetExited (int? exitCode)
		{
			var eventArgs = new TargetEventArgs (TargetEventType.TargetExited) {
				ExitCode = exitCode
			};

			OnTargetEvent (eventArgs);
		}

		internal void OnStopped (StoppedEventBody body)
		{
			// Run a task and do not wait for it since a request for the
			// current stack frames will not be processed since the OnStopped
			// method is called from the DebugAdapterClient and will not
			// process any messages until this method returns.
			Task.Run (() => OnStoppedInternal (body));
		}

		void OnStoppedInternal (StoppedEventBody body)
		{
			TargetEventType eventType = GetEventType (body.Reason);
			var eventArgs = new TargetEventArgs (eventType) {
				Thread = new ThreadInfo (1, body.ThreadId ?? 1, string.Empty, body.Source.Path)
			};

			if (eventType == TargetEventType.TargetHitBreakpoint) {
				eventArgs.BreakEvent = GetBreakEvent (body);
			}

			currentStoppedEventBody = body;

			OnTargetEvent (eventArgs);
		}

		BreakEvent GetBreakEvent (StoppedEventBody body)
		{
			var breakpointsForFile = GetBreakpointsForFile (body.Source.Path);
			return breakpointsForFile.FirstOrDefault (breakpoint => breakpoint.Line == body.Line);
		}

		static TargetEventType GetEventType (string reason)
		{
			switch (reason) {
				case "step":
				return TargetEventType.TargetStopped;

				case "breakpoint":
				return TargetEventType.TargetHitBreakpoint;

				case "pause":
				return TargetEventType.TargetStopped;

				case "exception":
				return TargetEventType.ExceptionThrown;

				default:
				return TargetEventType.TargetStopped;
			}
		}

		internal Task<ScopesResponseBody> GetScopes (int frameId)
		{
			var message = new ScopesRequestArguments {
				FrameId = frameId
			};
			return debugClient.SendRequest (ScopesRequest.Type, message);
		}

		internal Task<VariablesResponseBody> GetVariables (int reference)
		{
			var variableMessage = new VariablesRequestArguments {
				VariablesReference = reference
			};
			return debugClient.SendRequest (VariablesRequest.Type, variableMessage);
		}

		internal Task<EvaluateResponseBody> EvaluateExpression (int frameId, string expression)
		{
			var message = new EvaluateRequestArguments {
				FrameId = frameId,
				Expression = expression,
				Context = "watch"
			};
			return debugClient.SendRequest (EvaluateRequest.Type, message);
		}

		internal Task<SetVariableResponseBody> SetVariable (int variablesReference, string name, string value)
		{
			var message = new SetVariableRequestArguments {
				Name = name,
				Value = value,
				VariablesReference = variablesReference
			};
			return debugClient.SendRequest (SetVariableRequest.Type, message);
		}
	}
}
