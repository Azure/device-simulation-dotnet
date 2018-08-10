// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Services.Test.helpers
{
    ///<summary>
    /// This class is used to inject exceptions in the code under test
    /// and to verify that the system fails with the injected exception,
    /// i.e. not any exception, to be sure unit tests wouldn't pass in case
    /// a different exception is occurring, i.e. to avoid false positives.
    ///</summary>
    public class SomeException : Exception
    {
    }
}
