//
// BreakpointExtensions.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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
using Mono.Debugging.Client;

namespace MonoDevelop.PowerShell
{
	static class BreakpointExtensions
	{
		/// <summary>
		/// Only hit condition supported by the PowerShell Editor Services debugger is
		/// hit count == some number.
		/// </summary>
		public static string GetPowerShellHitCondition (this Breakpoint breakpoint)
		{
			switch (breakpoint.HitAction) {
				case HitAction.Break:
				return GetPowerShellHitCondition (breakpoint.HitCountMode, breakpoint.HitCount);

				default:
				return null;
			}
		}

		/// <summary>
		/// Only hit count == some number is supported. An attempt is made to handle
		/// the some of the other hit count modes but they may not work as expected.
		/// when debugging.
		/// </summary>
		static string GetPowerShellHitCondition (HitCountMode hitCountMode, int hitCount)
		{
			switch (hitCountMode) {
				case HitCountMode.EqualTo:
				case HitCountMode.GreaterThanOrEqualTo:
				return hitCount.ToString ();

				case HitCountMode.LessThan:
				return (hitCount - 1).ToString ();

				case HitCountMode.GreaterThan:
				return (hitCount + 1).ToString ();

				default:
				return null;
			}
		}
	}
}
