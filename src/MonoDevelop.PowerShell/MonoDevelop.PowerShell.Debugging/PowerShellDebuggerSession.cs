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
using Mono.Debugging.Client;
using MonoDevelop.Core;
using Microsoft.PowerShell.EditorServices.Protocol.MessageProtocol.Channel;
using Microsoft.PowerShell.EditorServices.Protocol.DebugAdapter;

namespace MonoDevelop.PowerShell
{
	class PowerShellDebuggerSession : DebuggerSession
	{
		PowerShellSession session;
		PowerShellDebugAdapterClient debugClient;

		protected override void OnAttachToProcess (long processId)
		{
			throw new NotImplementedException ();
		}

		protected override void OnContinue ()
		{
			throw new NotImplementedException ();
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
					await client.Stop ();
				}
			} catch (Exception ex) {
				PowerShellLoggingService.LogError ("PowerShellDebuggerSession.OnStop error.", ex);
			}
		}

		protected override void OnFinish ()
		{
		}

		protected override ProcessInfo[] OnGetProcesses ()
		{
			throw new NotImplementedException ();
		}

		protected override Backtrace OnGetThreadBacktrace (long processId, long threadId)
		{
			throw new NotImplementedException ();
		}

		protected override ThreadInfo[] OnGetThreads (long processId)
		{
			throw new NotImplementedException ();
		}

		protected override BreakEventInfo OnInsertBreakEvent (BreakEvent breakEvent)
		{
			throw new NotImplementedException ();
		}

		protected override void OnNextInstruction ()
		{
			throw new NotImplementedException ();
		}

		protected override void OnNextLine ()
		{
			throw new NotImplementedException ();
		}

		protected override void OnRemoveBreakEvent (BreakEventInfo eventInfo)
		{
			throw new NotImplementedException ();
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
				await debugClient.LaunchScript (startInfo.Command);
			} catch (Exception ex) {
				PowerShellLoggingService.LogError ("PowerShellDebuggerSession.OnRun error.", ex);
			}
		}

		protected override void OnSetActiveThread (long processId, long threadId)
		{
			throw new NotImplementedException ();
		}

		protected override void OnStepInstruction ()
		{
			throw new NotImplementedException ();
		}

		protected override void OnStepLine ()
		{
			throw new NotImplementedException ();
		}

		protected override void OnStop ()
		{
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
			TargetEventType eventType = GetEventType (body.Reason);
			var eventArgs = new TargetEventArgs (eventType) {
			};

			OnTargetEvent (eventArgs);
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
	}
}
