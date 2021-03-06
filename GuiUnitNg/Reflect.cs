﻿using System;
using System.Reflection;

namespace NUnit.Framework.Internal
{
	/* Thanks to ILRepack, this method implementation will replace the original
	 * one used in NUnit and thus allow us to inject the code we want to support
	 * main loop usage
	 */
	public class Reflect
	{
		public static object InvokeMethod (MethodInfo method, object fixture, params object [] args)
		{
			if (method != null) {
				if (fixture == null && !method.IsStatic)
					Console.WriteLine ("Trying to call {0} without an instance", method.Name);

				try {
					GuiUnitNg.GuiUnitEventSource.Log.InnerInvokeStart (TestExecutionContext.CurrentContext.CurrentTest.FullName,
					                                                   method.Name,
					                                                   fixture?.GetType ()?.Name ?? "no fixture");
					object result = null;
					if (GuiUnitNg.TextRunner.MainLoop == null || TestExecutionContext.CurrentContext.ParallelScope != ((ParallelScope)0)) {
						result = method.Invoke (fixture, args);
					} else {
						var invokeHelper = new GuiUnit.InvokerHelper {
							Context = TestExecutionContext.CurrentContext,
							Func = () => method.Invoke (fixture, args)
						};

						GuiUnitNg.TextRunner.MainLoop.InvokeOnMainLoop (invokeHelper);
						invokeHelper.Waiter.WaitOne ();
						if (invokeHelper.ex != null)
							Rethrow (invokeHelper.ex);
						result = invokeHelper.Result;
					}

					return result;
				} catch (Exception e) {
					Rethrow (e);
				} finally {
					GuiUnitNg.GuiUnitEventSource.Log.InnerInvokeStop ();
				}
			}

			return null;
		}

		static void Rethrow (Exception e)
		{
			string Rethrown = "Rethrown";
			if (e is NUnitException && e.Message == Rethrown)
				throw e;
			if (e is System.Threading.ThreadAbortException)
				return;
			if (e is TargetInvocationException || e is AggregateException)
				throw new NUnitException (Rethrown, e.InnerException);
			else
				throw new NUnitException (Rethrown, e);
		}
	}
}
