//
// ObjectValueExtensions.cs
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

using Mono.Debugging.Backend;
using Mono.Debugging.Client;

namespace MonoDevelop.PowerShell
{
	static class ObjectValueFactory
	{
		public static ObjectValue CreateObject (PowerShellVariableObjectValueSource valueSource, ObjectPath path)
		{
			return CreateObject (valueSource, path, string.Empty);
		}

		public static ObjectValue CreateObject (PowerShellVariableObjectValueSource valueSource, ObjectPath path, string value)
		{
			return ObjectValue.CreateObject (
				valueSource,
				path,
				string.Empty,
				value,
				ObjectValueFlags.Group,
				null);
		}

		public static ObjectValue CreateVariable (PowerShellVariableObjectValueSource valueSource, ObjectPath path)
		{
			return CreateVariable (valueSource, path, string.Empty);
		}

		public static ObjectValue CreateVariable (PowerShellVariableObjectValueSource valueSource, ObjectPath path, string value)
		{
			return CreateVariable (valueSource, path, value, ObjectValueFlags.None);
		}

		static ObjectValue CreateVariable (
			PowerShellVariableObjectValueSource valueSource,
			ObjectPath path,
			string value,
			ObjectValueFlags flags)
		{
			return ObjectValue.CreatePrimitive (
				valueSource,
				path,
				string.Empty,
				new EvaluationResult (value),
				ObjectValueFlags.Variable | flags);
		}

		public static ObjectValue CreateReadOnlyVariable (PowerShellVariableObjectValueSource valueSource, ObjectPath path, string value)
		{
			return CreateVariable (valueSource, path, value, ObjectValueFlags.ReadOnly);
		}

		public static ObjectValue CreateEvaluatingGroupArray (ObjectPath path, ObjectValue[] values)
		{
			return ObjectValue.CreateArray (
				null,
				path,
				string.Empty,
				values.Length,
				ObjectValueFlags.EvaluatingGroup,
				values);
		}
	}
}
