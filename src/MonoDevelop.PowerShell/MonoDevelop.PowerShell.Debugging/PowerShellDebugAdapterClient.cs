//
// PowerShellDebugAdapterClient.cs
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
using System.Threading.Tasks;
using Microsoft.PowerShell.EditorServices.Protocol.Client;
using Microsoft.PowerShell.EditorServices.Protocol.DebugAdapter;
using Microsoft.PowerShell.EditorServices.Protocol.MessageProtocol;
using Microsoft.PowerShell.EditorServices.Protocol.MessageProtocol.Channel;
using Mono.Debugging.Client;

namespace MonoDevelop.PowerShell
{
	class PowerShellDebugAdapterClient : DebugAdapterClient
	{
		PowerShellDebuggerSession session;
		TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool> ();

		public PowerShellDebugAdapterClient (ChannelBase clientChannel, PowerShellDebuggerSession session)
			: base (clientChannel)
		{
			this.session = session;
		}

		protected override async Task OnConnect ()
		{
			await base.OnConnect ();
			taskCompletionSource.SetResult (true);
		}

		public Task WaitForConnection ()
		{
			return taskCompletionSource.Task;
		}

		protected override Task OnStart ()
		{
			SetEventHandler (OutputEvent.Type, OnOutput);
			SetEventHandler (ExitedEvent.Type, OnExited);
			SetEventHandler (TerminatedEvent.Type, OnTerminated);
			SetEventHandler (StoppedEvent.Type, OnStopped);

			return Task.FromResult (true);
		}

		Task OnOutput (OutputEventBody body, EventContext context)
		{
			bool isStdErr = body.Category == "stderr";
			session.OutputWriter (isStdErr, body.Output);
			return Task.FromResult (true);
		}

		Task OnExited (ExitedEventBody body, EventContext context)
		{
			session.OnTargetExited (body.ExitCode);
			return Task.FromResult (true);
		}

		Task OnTerminated (object arg, EventContext context)
		{
			session.OnTargetExited (null);
			return Task.FromResult (true);
		}

		Task OnStopped (StoppedEventBody body, EventContext context)
		{
			session.OnStopped (body);
			return Task.FromResult (true);
		}

		public async Task LaunchScript (DebuggerStartInfo startInfo)
		{
			var request =  new LaunchRequestArguments {
				Script = startInfo.Command,
				Args = GetArguments (startInfo),
				Cwd = startInfo.WorkingDirectory,
				Env = startInfo.EnvironmentVariables
			};
			await SendRequest (LaunchRequest.Type, request);
			await SendRequest (ConfigurationDoneRequest.Type, null);
		}

		static string[] GetArguments (DebuggerStartInfo startInfo)
		{
			var powerShellDebugStartInfo = startInfo as PowerShellDebuggerStartInfo;
			if (powerShellDebugStartInfo?.ArgumentsArray != null) {
				return powerShellDebugStartInfo.ArgumentsArray;
			}

			return ToArray (startInfo.Arguments);
		}

		static string[] ToArray (string arguments)
		{
			if (string.IsNullOrEmpty (arguments))
				return null;

			return arguments.Split (new [] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		}
	}
}
