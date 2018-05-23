// Copyright (c) Microsoft. All rights reserved.

namespace Services.Test.helpers
{
    public static class Constants
    {
        // Use this to kill unit tests not supposed to run async code
        public const int TEST_TIMEOUT = 5000;

        // Use these flags to allow running a subset of tests from the test explorer and the command line.
        public const string TYPE = "Type";
        public const string UNIT_TEST = "UnitTest";
        public const string INTEGRATION_TEST = "IntegrationTest";

        // Use these flags to allow running a subset of tests from the test explorer and the command line.
        public const string SPEED = "Speed";
        public const string SLOW_TEST = "SlowTest";
    }
}
