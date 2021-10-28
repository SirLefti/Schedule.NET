using System;
using System.Reflection;
using NUnit.Framework;
using ScheduleSharp;

namespace ScheduleTest
{
    public class TestNextExecution
    {
        FieldInfo nextExecution;
        MethodInfo nextExecutionTimestamp;

        public TestNextExecution()
        {
            var type = typeof(Schedule);
            nextExecution = type.GetField("_nextExecution", BindingFlags.NonPublic | BindingFlags.Instance);
            nextExecutionTimestamp = type.GetMethod("NextExecutionTimestamp", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [Test]
        public void TestSecondDelayForOneMinute()
        {
            Schedule task = Schedule.Every().Second();

            var now = DateTime.Now;
            var second = TimeSpan.FromSeconds(1);
            for (int i = 0; i < 60; i++)
            {
                nextExecution.SetValue(task, now + i * second);
                var next = (DateTime)nextExecutionTimestamp.Invoke(task, null);
                Assert.AreEqual(now + (i + 1) * second, next);
            }
        }

        [Test]
        public void TestFirstOfMonth()
        {
            Schedule task = Schedule.Every().Month().At("-01");

            var firstOfMonth = DateTime.Today;
            var totalDays = DateTime.DaysInMonth(firstOfMonth.Year, firstOfMonth.Month);
            firstOfMonth = firstOfMonth.AddDays(totalDays - firstOfMonth.Day + 1);

            for (int i = 0; i <= 12; i++)
            {
                var next = (DateTime)nextExecutionTimestamp.Invoke(task, null);
                Assert.AreEqual(firstOfMonth.AddMonths(i), next);
                nextExecution.SetValue(task, next);
            }
        }

        [Test]
        public void TestEveryFirstApril()
        {
            Schedule task = Schedule.Every().April().At("-01");

            var firstOfApril = new DateTime(DateTime.Now.Year, 4, 1, 0, 0, 0);
            if (firstOfApril < DateTime.Now)
            {
                firstOfApril = firstOfApril.AddYears(1);
            }

            for (int i = 0; i <= 4; i++)
            {
                var next = (DateTime)nextExecutionTimestamp.Invoke(task, null);
                Assert.AreEqual(firstOfApril.AddYears(i), next);
                nextExecution.SetValue(task, next);
            }
        }

        [Test]
        public void TestEveryTalkLikeAPirateDay()
        {
            Schedule task = Schedule.Every().September().At("-19");

            var talkLikeAPirateDay = new DateTime(DateTime.Now.Year, 9, 19, 0, 0, 0);
            if (talkLikeAPirateDay < DateTime.Now)
            {
                talkLikeAPirateDay = talkLikeAPirateDay.AddYears(1);
            }

            for (int i = 0; i <= 4; i++)
            {
                var next = (DateTime)nextExecutionTimestamp.Invoke(task, null);
                Assert.AreEqual(talkLikeAPirateDay.AddYears(i), next);
                nextExecution.SetValue(task, next);
            }
        }

        [Test]
        public void TestEveryFirstAprilUsingAt()
        {
            Schedule task = Schedule.Every().Year().At("04-01");

            var firstOfApril = new DateTime(DateTime.Now.Year, 4, 1, 0, 0, 0);
            if (firstOfApril < DateTime.Now)
            {
                firstOfApril = firstOfApril.AddYears(1);
            }

            Assert.AreEqual(firstOfApril, (DateTime)nextExecutionTimestamp.Invoke(task, null));
        }

        [Test]
        public void TestOnceAtSpecificDate()
        {
            Schedule task = Schedule.Once().September().At("-19");

            var talkLikeAPirateDay = new DateTime(DateTime.Now.Year, 9, 19, 0, 0, 0);
            if (talkLikeAPirateDay < DateTime.Now)
            {
                talkLikeAPirateDay = talkLikeAPirateDay.AddYears(1);
            }

            Assert.AreEqual(talkLikeAPirateDay, (DateTime)nextExecutionTimestamp.Invoke(task, null));
        }

        [Test]
        public void TestOnceAtNextMonday()
        {
            Schedule task = Schedule.Once().Monday().At("08:00");
            const int daysPerWeek = 7;

            var nextMonday8AM = DateTime.Today + TimeSpan.FromHours(8);
            if (nextMonday8AM < DateTime.Now)
            {
                var shiftDays = (DayOfWeek.Monday - nextMonday8AM.DayOfWeek + daysPerWeek) % daysPerWeek;
                nextMonday8AM = nextMonday8AM.AddDays(shiftDays == 0 ? 7 : shiftDays);
            }

            Assert.AreEqual(nextMonday8AM, (DateTime)nextExecutionTimestamp.Invoke(task, null));
        }
    }
}
