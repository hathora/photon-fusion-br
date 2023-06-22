// Created by dylan@hathora.dev

using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Hathora.Core.Scripts.Runtime.Common.Extensions
{
    /// <summary>
    /// Allow an awaitable Task
    /// </summary>
    public static class UnityWebRequestAsyncOperationExtensions
    {
        public static Task<UnityWebRequest> AsTask(this UnityWebRequestAsyncOperation asyncOperation)
        {
            var completionSource = new TaskCompletionSource<UnityWebRequest>();
            asyncOperation.completed += operation => completionSource.SetResult(asyncOperation.webRequest);
            return completionSource.Task;
        }
    }
}
