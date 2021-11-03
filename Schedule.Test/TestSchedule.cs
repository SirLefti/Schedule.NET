using System;
using NUnit.Framework;
using Scheduling;

namespace Scheduling
{
    public class TestSchedule
    {
        [Test]
        public void Test10SecondsSchedule()
        {
            Schedule.Every(10).Seconds().Run(() => Console.WriteLine("Hello every 10 seconds"));
        }

        [Test]
        public void TestExpectSyntaxAssertionFail()
        {
            Assert.Throws<IntervalException>(() =>
                Schedule.Every(10).Second().Run(() => Console.WriteLine("This should never work"))
            );

        }

        [Test]
        public void TestExpectTimeFormatAssertionFail()
        {
            Assert.Throws<TimeFormatException>(() =>
                Schedule.Every().Day().At("25:90").Run(() => Console.WriteLine("This should never work"))
            );
        }

        [Test]
        public void TestClockTimeSchedule()
        {
            Schedule.Every().Day().At("21:35").Run(() => Console.WriteLine("Hello every day"));
        }

        [Test]
        public void TestWeirdSyntax()
        {
            Assert.Throws<ScheduleException>(() =>
                Schedule.Every().Second().Monday().Run(() => Console.WriteLine("This should never work"))
            );
        }

        [Test]
        public void TestMoreWeirdSyntax()
        {
            Assert.Throws<ScheduleException>(() =>
                Schedule.Every().Week().Monday().Run(() => Console.WriteLine("This should never work"))
            );
        }

        [Test]
        public void TestOnceTaskSyntax()
        {
            Schedule.Once().At("11-01 08:00").Run(() => Console.WriteLine("Auto-choose year as unit to use month and day as timestamp"));
            Schedule.Once().At("-05 09:00").Run(() => Console.WriteLine("Execute at next 5th of a month"));
        }
    }
}
