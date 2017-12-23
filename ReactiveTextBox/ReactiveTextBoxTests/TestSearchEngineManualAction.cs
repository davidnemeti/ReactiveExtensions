using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ReactiveTextBox;

namespace ReactiveTextBoxTests
{
    public class TestSearchEngineManualAction : ITestSearchEngineAction
    {
        private TaskCompletionSource<object> _actionNotExecuting = new TaskCompletionSource<object>();
        private TaskCompletionSource<object> _actionExecuting;

        public async Task Execute(CancellationToken ct)
        {
            CheckThatNotBeingExecuted();
            await StartExecution(ct);
        }

        public async Task Complete()
        {
            await WaitForBeingExecuted();
            StopExecution(actionExecuting => actionExecuting.TrySetResult(null));
        }

        public async Task Cancel()
        {
            await WaitForBeingExecuted();
            StopExecution(actionExecuting => actionExecuting.TrySetCanceled());
        }

        public async Task Throw(Exception exception)
        {
            await WaitForBeingExecuted();
            StopExecution(actionExecuting => actionExecuting.TrySetException(exception));
        }

        private async Task WaitForBeingExecuted([CallerMemberName]string sourceMemberName = "")
        {
            await Helpers.NullSafeAwait(_actionNotExecuting?.Task);
        }

        private void CheckThatNotBeingExecuted()
        {
            if (_actionNotExecuting == null)
            {
                throw new InvalidOperationException($"'{nameof(Execute)}' should not be called twice.");
            }
        }

        private async Task StartExecution(CancellationToken ct)
        {
            _actionExecuting = new TaskCompletionSource<object>();
            var actionExecutingTask = _actionExecuting.Task;
            using (ct.Register(() => _actionExecuting?.TrySetCanceled()))
            {
                _actionNotExecuting.SetResult(null);
                _actionNotExecuting = null;

                await actionExecutingTask;
            }
        }

        private void StopExecution(Action<TaskCompletionSource<object>> stopAction)
        {
            stopAction(_actionExecuting);
            _actionExecuting = null;
        }
    }
}
