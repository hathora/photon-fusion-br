// Created by dylan@hathora.dev

using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Hathora.Core.Scripts.Runtime.Common.Utils
{
    public abstract class HathoraTaskUtils
    {
        /// <summary>
        /// yield return this in a coroutine to wait for a Task to complete.
        /// </summary>
        public class WaitForTaskCompletion : CustomYieldInstruction
        {
            protected readonly Task task;

            public WaitForTaskCompletion(Task _task)
            {
                this.task = _task;
            }

            public override bool keepWaiting => !task.IsCompleted;
        }

        /// <summary>
        /// yield return this in a coroutine to wait for a Task to complete.
        /// This particular overload will return the result of the Task.
        ///
        /// (!) This is a little hacky; see example below (within the class).
        /// You may, instead, want to consider just passing an obj (ByRef).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class WaitForTaskCompletion<T> : WaitForTaskCompletion
        {
            // [EXAMPLE]
            // ################################################################################################
            /* 
             * Task<string> barTask = BarAsync();
             * WaitForTaskCompletion<string> customYield = new WaitForTaskCompletion<string>(barTask);
             * yield return customYield;
             * 
             * // Cast the custom yield instruction back to WaitForTaskCompletion<T> and retrieve the result
             * string result = customYield.Result;
             * Debug.Log(result);
             */
            // ################################################################################################
        
            private T result;

            public T Result
            {
                get
                {
                    if (task.IsCompletedSuccessfully)
                    {
                        return ((Task<T>)task).Result;
                    }

                    throw new InvalidOperationException("[HathoraTaskUtils] " +
                        "The task did not complete successfully.");
                }
            }

            public WaitForTaskCompletion(Task<T> _task) : base(_task) { }
        }
    }
}
