using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using WinRT;

namespace SonicWave.App;

public static class Program
{
	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	[STAThread]
	private static void Main(string[] args)
	{
		ComWrappersSupport.InitializeComWrappers();
		Application.Start((ApplicationInitializationCallbackParams p) =>
		{
			SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
			new App();
		});
	}
}
