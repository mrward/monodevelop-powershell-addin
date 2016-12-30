//
// PowerShellVariableValueUpdater.cs
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
using System.Threading.Tasks;
using Mono.Debugging.Backend;
using Mono.Debugging.Client;
using Microsoft.PowerShell.EditorServices.Protocol.DebugAdapter;
using System.Linq;

namespace MonoDevelop.PowerShell
{
	class PowerShellVariableValueUpdater : IObjectValueUpdater
	{
		Dictionary<string, UpdateCallback> asyncCallbacks = new Dictionary<string, UpdateCallback> ();
		Dictionary<string, ObjectValue> asyncResults = new Dictionary<string, ObjectValue> ();
		PowerShellDebuggerSession debugSession;

		public PowerShellVariableValueUpdater (PowerShellDebuggerSession debugSession)
		{
			this.debugSession = debugSession;
		}

		public async Task UpdateVariableScopes (ObjectPath path, long frameAddress)
		{
			//var task = debugSession.GetScopes ((int)frameAddress);
			//if (task.Wait (2000)) {
			ScopesResponseBody response = await debugSession.GetScopes ((int)frameAddress);

			var values = response.Scopes.Select (scope => new ObjectValue { Name = scope.Name }).ToArray ();
			var result = ObjectValue.CreateArray (null, path, string.Empty, values.Length, ObjectValueFlags.EvaluatingGroup, values);
			OnObjectValueUpdated (path[0], result);
		}

		public void RegisterUpdateCallbacks (UpdateCallback[] callbacks)
		{
			foreach (UpdateCallback c in callbacks) {
				lock (asyncCallbacks) {
					ObjectValue val;
					string id = c.Path[0];
					if (asyncResults.TryGetValue (id, out val)) {
						c.UpdateValue (val);
						asyncResults.Remove (id);
					} else {
						asyncCallbacks[id] = c;
					}
				}
			}
		}

		void OnObjectValueUpdated (string id, ObjectValue val)
		{
			UpdateCallback cb = null;
			lock (asyncCallbacks) {
				if (asyncCallbacks.TryGetValue (id, out cb)) {
					try {
						cb.UpdateValue (val);
					} catch {
					}
					asyncCallbacks.Remove (id);
				} else {
					asyncResults [id] = val;
				}
			}
		}
	}
}
