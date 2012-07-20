using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Threading.Tasks {
    
    internal class TaskHelpers {

        private static readonly Task _defaultCompleted = FromResult<AsyncVoid>(default(AsyncVoid));

        internal static Task<TResult> FromResult<TResult>(TResult result) {

            TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>();
            tcs.SetResult(result);
            return tcs.Task;
        }

        internal static Task FromError(Exception exception) {

            return FromError<AsyncVoid>(exception);
        }

        internal static Task<TResult> FromError<TResult>(Exception exception) {

            TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>();
            tcs.SetException(exception);
            return tcs.Task;
        }

        private struct AsyncVoid { }
    }
}