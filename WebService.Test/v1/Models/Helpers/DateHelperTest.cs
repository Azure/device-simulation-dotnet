// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.Helpers;
using WebService.Test.helpers;
using Xunit;

namespace WebService.Test.v1.Models.Helpers
{
    public class DateHelperTest
    {
        // Truncate the seconds in case the second changes during the test (another way would be to test
        // the actual-expected delta, and accepting a small error)
        const string FORMAT = "yyyy-MM-dd'T'HH:mm:sszzz";

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItConvertsDates()
        {
            Assert.Equal("2019-10-30T03:01:02+00:00", DateHelper.ParseDate("2019-10-30T03:01:02Z").Value.ToString("yyyy-MM-dd'T'HH:mm:sszzz"));
            Assert.Equal("2019-12-31T13:14:15+00:00", DateHelper.ParseDate("2019-12-31T13:14:15Z").Value.ToString("yyyy-MM-dd'T'HH:mm:sszzz"));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItConvertsExpressions()
        {
            var now = DateTimeOffset.UtcNow;

            Assert.Equal(now.ToString(FORMAT), DateHelper.ParseDateExpression("NOW", now).Value.ToString(FORMAT));

            Assert.Equal(now.AddDays(-1).ToString(FORMAT), DateHelper.ParseDateExpression("NOW-P1D", now).Value.ToString(FORMAT));
            Assert.Equal(now.AddDays(+1).ToString(FORMAT), DateHelper.ParseDateExpression("NOW+P1D", now).Value.ToString(FORMAT));

            Assert.Equal(now.AddHours(-30).ToString(FORMAT), DateHelper.ParseDateExpression("NOW-PT30H", now).Value.ToString(FORMAT));
            Assert.Equal(now.AddHours(+30).ToString(FORMAT), DateHelper.ParseDateExpression("NOW+PT30H", now).Value.ToString(FORMAT));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItHandlesEdgeCases()
        {
            var now = DateTimeOffset.UtcNow;

            // Empty value
            Assert.Null(DateHelper.ParseDate(""));
            Assert.Null(DateHelper.ParseDateExpression("", now));

            // Space instead of a "+"
            Assert.Equal(now.AddDays(+1).ToString(FORMAT), DateHelper.ParseDateExpression("NOW P1D", now).Value.ToString(FORMAT));
            Assert.Equal(now.AddHours(+30).ToString(FORMAT), DateHelper.ParseDateExpression("NOW PT30H", now).Value.ToString(FORMAT));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsExceptions()
        {
            Assert.Throws<InvalidDateFormatException>(() => DateHelper.ParseDate("0"));
            Assert.Throws<InvalidDateFormatException>(() => DateHelper.ParseDate("foo"));
            Assert.Throws<InvalidDateFormatException>(() => DateHelper.ParseDate("NOW"));

            var now = DateTimeOffset.UtcNow;
            Assert.Throws<InvalidDateFormatException>(() => DateHelper.ParseDateExpression("0", now));
            Assert.Throws<InvalidDateFormatException>(() => DateHelper.ParseDateExpression("NOW-", now));
            Assert.Throws<InvalidDateFormatException>(() => DateHelper.ParseDateExpression("NOW+", now));
            Assert.Throws<InvalidDateFormatException>(() => DateHelper.ParseDateExpression("NOW-0", now));
            Assert.Throws<InvalidDateFormatException>(() => DateHelper.ParseDateExpression("NOW+0", now));
            Assert.Throws<InvalidDateFormatException>(() => DateHelper.ParseDateExpression("foo", now));
            Assert.Throws<InvalidDateFormatException>(() => DateHelper.ParseDateExpression("NOW-foo", now));
            Assert.Throws<InvalidDateFormatException>(() => DateHelper.ParseDateExpression("NOW+foo", now));
            Assert.Throws<InvalidDateFormatException>(() => DateHelper.ParseDateExpression("NOW-NOW", now));
            Assert.Throws<InvalidDateFormatException>(() => DateHelper.ParseDateExpression("NOW+NOW", now));
        }
    }
}
