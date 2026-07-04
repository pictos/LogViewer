using System.Runtime.CompilerServices;

namespace LogViewer.DeviceTests;

// NUnit determines how to await an `async Task` test at runtime via reflection over the
// awaitable "shape" (see NUnit's CSharpPatternBasedAwaitAdapter): it calls Task.GetAwaiter(),
// then looks up the INotifyCompletion interface on the awaiter plus its IsCompleted / GetResult
// members. The C# compiler special-cases await and never routes through those interfaces, so on
// Apple platforms (Mac Catalyst / iOS) the Release linker strips the INotifyCompletion /
// ICriticalNotifyCompletion interface implementations off TaskAwaiter. NUnit's
// GetInterface("INotifyCompletion") then returns null and every async test fails with
// "Cannot determine result type for: System.Threading.Tasks.Task".
//
// This type is never executed. Because the test assembly is linker-rooted (RootMode="all"),
// the linker still analyses this IL and, seeing the awaiter cast to its completion interfaces,
// keeps those interface implementations and members so NUnit's reflection succeeds.
static class AsyncTrimmerRoots
{
	static void KeepTaskAwaiterShape()
	{
		TaskAwaiter awaiter = Task.CompletedTask.GetAwaiter();
		_ = awaiter.IsCompleted;
		awaiter.GetResult();
		INotifyCompletion notify = awaiter;
		notify.OnCompleted(static () => { });
		ICriticalNotifyCompletion critical = awaiter;
		critical.UnsafeOnCompleted(static () => { });
	}

	static void KeepTaskAwaiterShapeGeneric()
	{
		TaskAwaiter<int> awaiter = Task.FromResult(0).GetAwaiter();
		_ = awaiter.IsCompleted;
		_ = awaiter.GetResult();
		INotifyCompletion notify = awaiter;
		notify.OnCompleted(static () => { });
		ICriticalNotifyCompletion critical = awaiter;
		critical.UnsafeOnCompleted(static () => { });
	}
}
