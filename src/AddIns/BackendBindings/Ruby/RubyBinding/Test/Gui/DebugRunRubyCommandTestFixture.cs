﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Diagnostics;
using ICSharpCode.Core;
using ICSharpCode.RubyBinding;
using ICSharpCode.Scripting.Tests.Utils;
using ICSharpCode.SharpDevelop.Debugging;
using NUnit.Framework;
using RubyBinding.Tests.Utils;

namespace RubyBinding.Tests.Gui
{
	[TestFixture]
	public class DebugRubyCommandTestFixture
	{
		MockDebugger debugger;
		RunDebugRubyCommand command;
		
		[TestFixtureSetUp]
		public void SetUpFixture()
		{
			MockWorkbench workbench = MockWorkbench.CreateWorkbenchWithOneViewContent(@"C:\Projects\test.rb");

			Properties p = new Properties();
			RubyAddInOptions options = new RubyAddInOptions(p);
			options.RubyFileName = @"C:\IronRuby\ir.exe";
		
			debugger = new MockDebugger();
			command = new RunDebugRubyCommand(workbench, options, debugger);
			command.Run();
		}
				
		[Test]
		public void DebuggerStartMethodCalled()
		{
			Assert.IsTrue(debugger.StartMethodCalled);
		}
		
		[Test]
		public void ProcessInfoFileNameContainsPathToIronRubyConsole()
		{
			Assert.AreEqual(@"C:\IronRuby\ir.exe", debugger.ProcessStartInfo.FileName);
		}
		
		[Test]
		public void ProcessInfoArgsHasDebugArgument()
		{
			Assert.AreEqual("-D test.rb", debugger.ProcessStartInfo.Arguments);
		}
	}
}