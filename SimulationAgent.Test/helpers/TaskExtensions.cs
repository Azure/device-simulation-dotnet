// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;

namespace SimulationAgent.Test.helpers
{
    public static class TaskExtensions
    {
        public static void WaitInUnitTest(this Task task)
        {
            task.Wait(Constants.TEST_TIMEOUT);
        }
    }
}
