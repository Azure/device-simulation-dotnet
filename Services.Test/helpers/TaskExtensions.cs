// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;
using Xunit.Sdk;

namespace Services.Test.helpers
{
    /**
     * Use this class when testing asynchronous code, to avoid tests
     * running forever, e.g. in case threads don't end as expected.
     *
     * Example:
     *
     * this.target.SomeMethodAsync().CompleteOrTimeout();
     *
     * var result = this.target.SomeMethodAsync().CompleteOrTimeout().Result;
     */
    public static class TaskExtensions
    {
        // Wait for the task to complete or timeout
        public static Task CompleteOrTimeout(this Task t)
        {
            var complete = t.Wait(Constants.TEST_TIMEOUT);
            if (!complete)
            {
                throw new TestTimeoutException(Constants.TEST_TIMEOUT);
            }

            return t;
        }

        // Wait for the task to complete or timeout
        public static Task<T> CompleteOrTimeout<T>(this Task<T> t)
        {
            var complete = t.Wait(Constants.TEST_TIMEOUT);
            if (!complete)
            {
                throw new TestTimeoutException(Constants.TEST_TIMEOUT);
            }

            return t;
        }
    }
}
