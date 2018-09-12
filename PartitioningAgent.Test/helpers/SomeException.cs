// Copyright (c) Microsoft. All rights reserved.

using System;

namespace PartitioningAgent.Test.helpers
{
    /*
    This class is used to inject exceptions in the code under test and to verify that
    the system fails with the injected exception, i.e. not ANY exception.
    This is to avoid false negatives, i.e. to avoid that a test passes due to
    an unexpected exception.

    Example usage:

        this.dependencyMock.Setup(x => x.SomeMethod()).Throws<SomeException>();

        Assert.ThrowsAsync<ExpectedException>(() => this.target.MethodUnderTest())
    */
    public class SomeException : Exception
    {
    }
}
