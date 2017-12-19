//
// PowerShellVariableObjectValueSource.cs
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
using Microsoft.PowerShell.EditorServices.Protocol.DebugAdapter;
using Mono.Debugging.Backend;
using Mono.Debugging.Client;
using MonoDevelop.Core;

namespace MonoDevelop.PowerShell
{
	class PowerShellVariableObjectValueSource : IObjectValueSource
	{
		PowerShellDebuggerSession debugSession;
		int variablesReference;
		string name;
		int variableContainerReferenceId = -1;
		bool isString;
		ObjectValue variableObjectValues;

		public PowerShellVariableObjectValueSource (
			PowerShellDebuggerSession debugSession,
			int variablesReference)
		{
			this.debugSession = debugSession;
			this.variablesReference = variablesReference;
		}

		public PowerShellVariableObjectValueSource (
			PowerShellDebuggerSession debugSession,
			Variable variable,
			int variableContainerReferenceId)
		{
			this.debugSession = debugSession;
			this.variablesReference = variable.VariablesReference;
			this.name = variable.Name;
			this.variableContainerReferenceId = variableContainerReferenceId;

			isString = IsStringVariable (variable.Value);
		}

		static bool IsStringVariable (string value)
		{
			if (value != null && value.Length > 1) {
				return (value[0] == '"') &&
					(value[value.Length - 1] == '"');
			}
			return false;
		}

		public ObjectValue[] GetChildren (ObjectPath path, int index, int count, EvaluationOptions options)
		{
			if (variablesReference == 0) {
				return new ObjectValue[0];
			}

			if (variableObjectValues == null) {
				variableObjectValues = GetVariableObjectValuesAsync ();
			}

			return new [] { variableObjectValues };
		}

		ObjectValue GetVariableObjectValuesAsync ()
		{
			var updater = new PowerShellVariableValueUpdater (debugSession);
			var path = new ObjectPath (variablesReference.ToString ());
			var value = ObjectValue.CreateEvaluating (updater, path, ObjectValueFlags.EvaluatingGroup);
			value.Name = GettextCatalog.GetString ("Variables");

			updater.UpdateVariables (path, variablesReference).Ignore ();

			return value;
		}

		public object GetRawValue (ObjectPath path, EvaluationOptions options)
		{
			throw new NotImplementedException ();
		}

		public ObjectValue GetValue (ObjectPath path, EvaluationOptions options)
		{
			throw new NotImplementedException ();
		}

		public void SetRawValue (ObjectPath path, object value, EvaluationOptions options)
		{
			throw new NotImplementedException ();
		}

		public EvaluationResult SetValue (ObjectPath path, string value, EvaluationOptions options)
		{
			if (isString)
				value = WrapStringInDoubleQuotes (value);

			var task = SetVariableValueAsync (value);
			task.Wait (2500);
			if (task.IsCompleted && task.Result != null) {
				return new EvaluationResult (task.Result.Value);
			}
			return null;
		}

		static string WrapStringInDoubleQuotes (string value)
		{
			if (IsStringVariable (value)) {
				return value;
			}

			return "\"" + value + "\"";
		}

		Task<SetVariableResponseBody> SetVariableValueAsync (string value)
		{
			return Task.Run (async () => {
				return await debugSession.SetVariable (variableContainerReferenceId, name, value);
			});
		}
	}
}
