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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.PowerShell.EditorServices.Protocol.DebugAdapter;
using Mono.Debugging.Backend;
using Mono.Debugging.Client;

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
			ScopesResponseBody response = await debugSession.GetScopes ((int)frameAddress);

			var values = response.Scopes.Select (CreateObjectValue).ToArray ();
			var result = ObjectValueFactory.CreateEvaluatingGroupArray (path, values);
			OnObjectValueUpdated (path[0], result);
		}

		ObjectValue CreateObjectValue (Scope scope)
		{
			ObjectValue value = null;
			var path = new ObjectPath (scope.Name + scope.VariablesReference.ToString ());
			var valueSource = new PowerShellVariableObjectValueSource (debugSession, scope.VariablesReference);

			if (scope.VariablesReference == 0) {
				value = ObjectValueFactory.CreateVariable (valueSource, path);
			} else {
				value = ObjectValueFactory.CreateObject (valueSource, path);
			}

			value.Name = scope.Name;

			return value;
		}

		public async Task UpdateVariables (ObjectPath path, int variablesReference)
		{
			VariablesResponseBody response = await debugSession.GetVariables (variablesReference);

			var values = response.Variables.Select (variable => CreateObjectValue (variable, variablesReference)).ToArray ();
			var result = ObjectValueFactory.CreateEvaluatingGroupArray (path, values);
			OnObjectValueUpdated (path[0], result);
		}

		ObjectValue CreateObjectValue (Variable variable, int variableContainerReferenceId)
		{
			var path = new ObjectPath (variable.VariablesReference.ToString ());
			var valueSource = new PowerShellVariableObjectValueSource (debugSession, variable, variableContainerReferenceId);

			ObjectValue value = null;
			if (variable.VariablesReference == 0) {
				value = ObjectValueFactory.CreateVariable (valueSource, path, variable.Value);
			} else {
				value = ObjectValueFactory.CreateObject (valueSource, path, variable.Value);
			}

			value.Name = variable.Name;

			return value;
		}

		public async Task GetExpressionValue (ObjectPath path, long frameAddress, string expression)
		{
			EvaluateResponseBody response = await debugSession.EvaluateExpression ((int)frameAddress, expression);

			var value = CreateObjectValue (path, response);

			OnObjectValueUpdated (path[0], value);
		}

		ObjectValue CreateObjectValue (ObjectPath path, EvaluateResponseBody response)
		{
			var valueSource = new PowerShellVariableObjectValueSource (debugSession, response.VariablesReference);

			if (response.VariablesReference == 0) {
				return ObjectValueFactory.CreateVariable (valueSource, path, response.Result);
			} else {
				return ObjectValueFactory.CreateObject (valueSource, path);
			}
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
