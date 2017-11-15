// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
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
        const string FORMAT = "yyyy-MM-dd'T'HH:mm:--zzz";

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItConvertsDates()
        {
            Assert.Equal("2019-10-30T03:01:02+00:00", DateHelper.ParseDate("2019-10-30T03:01:02Z").Value.ToString("yyyy-MM-dd'T'HH:mm:sszzz"));
            Assert.Equal("2019-12-31T13:14:15+00:00", DateHelper.ParseDate("2019-12-31T13:14:15Z").Value.ToString("yyyy-MM-dd'T'HH:mm:sszzz"));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItConvertsFormulas()
        {
            var now = SyncClock();

            Assert.Equal(now.ToString(FORMAT), DateHelper.ParseDate("NOW").Value.ToString(FORMAT));

            Assert.Equal(now.AddDays(-1).ToString(FORMAT), DateHelper.ParseDate("NOW-P1D").Value.ToString(FORMAT));
            Assert.Equal(now.AddDays(+1).ToString(FORMAT), DateHelper.ParseDate("NOW+P1D").Value.ToString(FORMAT));

            Assert.Equal(now.AddHours(-30).ToString(FORMAT), DateHelper.ParseDate("NOW-PT30H").Value.ToString(FORMAT));
            Assert.Equal(now.AddHours(+30).ToString(FORMAT), DateHelper.ParseDate("NOW+PT30H").Value.ToString(FORMAT));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItHandlesEdgeCases()
        {
            var now = SyncClock();

            // Empty value
            Assert.Null(DateHelper.ParseDate(""));

            // Space instead of a "+"
            Assert.Equal(now.AddDays(+1).ToString(FORMAT), DateHelper.ParseDate("NOW P1D").Value.ToString(FORMAT));
            Assert.Equal(now.AddHours(+30).ToString(FORMAT), DateHelper.ParseDate("NOW PT30H").Value.ToString(FORMAT));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsExceptions()
        {
            Assert.Throws<InvalidDateFormatException>(() => DateHelper.ParseDate("0"));
            Assert.Throws<InvalidDateFormatException>(() => DateHelper.ParseDate("NOW-"));
            Assert.Throws<InvalidDateFormatException>(() => DateHelper.ParseDate("NOW+"));
            Assert.Throws<InvalidDateFormatException>(() => DateHelper.ParseDate("NOW-0"));
            Assert.Throws<InvalidDateFormatException>(() => DateHelper.ParseDate("NOW+0"));
            Assert.Throws<InvalidDateFormatException>(() => DateHelper.ParseDate("foo"));
            Assert.Throws<InvalidDateFormatException>(() => DateHelper.ParseDate("NOW-foo"));
            Assert.Throws<InvalidDateFormatException>(() => DateHelper.ParseDate("NOW+foo"));
            Assert.Throws<InvalidDateFormatException>(() => DateHelper.ParseDate("NOW-NOW"));
            Assert.Throws<InvalidDateFormatException>(() => DateHelper.ParseDate("NOW+NOW"));
        }

        private static DateTimeOffset SyncClock()
        {
            var now = DateTimeOffset.UtcNow;

            // Sync the clock to avoid test failures, assuming the test takes less than 5 seconds to complete
            while (now.Second >= 55)
            {
                Thread.Sleep(1000);
                now = DateTimeOffset.UtcNow;
            }
            return now;
        }
    }
}
