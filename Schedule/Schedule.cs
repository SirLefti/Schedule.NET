using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace Scheduling
{
    public class Schedule
    {
        private readonly int _interval;
        private readonly bool _repeat = true;
        
        private TimeUnit _unit;
        private DayOfWeek _targetDayOfWeek;
        private Action _action;
        private DateTime _nextExecution = DateTime.MinValue;

        private bool _unitSet = false;

        private bool _alive = true;
        private bool _usingTargetTime = false;
        private bool _usingTargetDate = false;
        private int _targetMonth = 1;
        private int _targetDay = 1;
        private int _targetHour = 0;
        private int _targetMinute = 0;
        private int _targetSecond = 0;


        private static readonly CoreScheduler _coreScheduler = new CoreScheduler();
        private class CoreScheduler
        {
            private HashSet<Schedule> _scheduledTasks = new HashSet<Schedule>();
            private bool _alive = true;

            private readonly Thread _thread;
            private readonly AutoResetEvent _waitHandle;

            public CoreScheduler()
            {
                _thread = new Thread(Run);
                _waitHandle = new AutoResetEvent(false);
            }

            public void Kill()
            {
                _alive = false;
                _scheduledTasks.Clear();
            }

            public void AddTask(Schedule task)
            {
                _scheduledTasks.Add(task);
                if (!_thread.IsAlive)
                {
                    _thread.Start();
                }
                else
                {
                    _waitHandle.Set();
                }
            }

            private void Run()
            {
                while (_alive)
                {
                    // find next execution timestamp and collect tasks, that are still alive
                    var nextExecution = DateTime.Now + TimeSpan.FromDays(30);
                    var waitIndefinitely = true;
                    var toRun = new HashSet<Schedule>();
                    foreach (var task in _scheduledTasks.Where(task => task._alive))
                    {
                        // Console.WriteLine(DateTime.Now.ToString("u") + " task execution timestamp is " + task._nextExecution);
                        if (task._nextExecution < nextExecution)
                        {
                            nextExecution = task._nextExecution;
                            waitIndefinitely = false;
                        }
                        toRun.Add(task);
                    }
                    // remove tasks, that are not alive
                    _scheduledTasks.Clear();
                    _scheduledTasks = toRun;

                    var now = DateTime.Now;
                    if (waitIndefinitely)
                    {
                        // Console.WriteLine(DateTime.Now.ToString("u") + " nothing scheduled, wait for wake signal");
                        // wait until wake signal
                        _waitHandle.WaitOne();
                    }
                    else if (nextExecution > now)
                    {
                        // Console.WriteLine(DateTime.Now.ToString("u") + " nothing to do, wait " + (nextExecution - now) + " unless wake signal received");
                        // WaitOne supports only 2^31-1 milliseconds of waiting, which is Int32 max value, even when using TimeSpan object.
                        // Thus align the total milliseconds to this max value and wait for this value instead.
                        // This allows to sleep repeatedly until real execution occurs, because this limit is equal to around 24 days.
                        var milliseconds = (int)(nextExecution - now).TotalMilliseconds;
                        _waitHandle.WaitOne(milliseconds);
                        // Console.WriteLine(DateTime.Now.ToString("u") + " time is up or wake signal received");
                    }

                    foreach (var task in _scheduledTasks.Where(task => task._alive && task._nextExecution < DateTime.Now))
                    {
                        new Thread(new ThreadStart(task._action)).Start();
                        if (task._repeat)
                        {
                            task._nextExecution = task.NextExecutionTimestamp();
                            // Console.WriteLine(DateTime.Now.ToString("u") + " next execution scheduled to " + task._nextExecution);
                        }
                        else
                        {
                            // Flags the task as not alive any more and will be removed from scheduled tasks by the
                            // core schedule in the next while iteration. This is to avoid remove items from a
                            // collection while iterating.
                            task.Cancel();
                        }
                    }
                }
            }
        }

        private Schedule(int interval)
        {
            _interval = interval;
        }

        private Schedule(int interval, bool repeat)
        {
            _interval = interval;
            _repeat = repeat;
        }

        public static Schedule Every()
        {
            return new Schedule(1);
        }

        public static Schedule Every(int interval)
        {
            if (interval == 1)
            {
                throw new IntervalException("use Every() instead");
            }
            if (interval < 1)
            {
                throw new IntervalException("use positive interval values only");
            }
            return new Schedule(interval);
        }

        public static Schedule Once()
        {
            return new Schedule(1, false);
        }

        public Schedule Second()
        {
            if (_interval != 1)
            {
                throw new IntervalException("use seconds() instead");
            }
            return Seconds();
        }

        public Schedule Seconds()
        {
            if (_unitSet)
            {
                throw new ScheduleException("schedule unit already set");
            }
            _unit = TimeUnit.SECONDS;
            _unitSet = true;
            return this;
        }

        public Schedule Minute()
        {
            if (_interval != 1)
            {
                throw new IntervalException("use Minutes() instead");
            }
            return Minutes();
        }

        public Schedule Minutes()
        {
            if (_unitSet)
            {
                throw new ScheduleException("schedule unit already set");
            }
            _unit = TimeUnit.MINUTES;
            _unitSet = true;
            return this;
        }
        
        public Schedule Hour()
        {
            if (_interval != 1)
            {
                throw new IntervalException("use Hours() instead");
            }
            return Hours();
        }

        public Schedule Hours()
        {
            if (_unitSet)
            {
                throw new ScheduleException("schedule unit already set");
            }
            _unit = TimeUnit.HOURS;
            _unitSet = true;
            return this;
        }
        
        public Schedule Day()
        {
            if (_interval != 1)
            {
                throw new IntervalException("use Days() instead");
            }
            return Days();
        }

        public Schedule Days()
        {
            if (_unitSet)
            {
                throw new ScheduleException("schedule unit already set");
            }
            _unit = TimeUnit.DAYS;
            _unitSet = true;
            return this;
        }
        
        public Schedule Week()
        {
            if (_interval != 1)
            {
                throw new IntervalException("use Weeks() instead");
            }
            return Weeks();
        }

        public Schedule Weeks()
        {
            if (_unitSet)
            {
                throw new ScheduleException("schedule unit already set");
            }
            _unit = TimeUnit.WEEKS;
            _targetDayOfWeek = DateTime.Now.DayOfWeek;
            _unitSet = true;
            return this;
        }

        public Schedule Monday()
        {
            if (_unitSet)
            {
                throw new ScheduleException("schedule unit already set");
            }
            _unit = TimeUnit.WEEKS;
            _targetDayOfWeek = DayOfWeek.Monday;
            _unitSet = true;
            return this;
        }

        public Schedule Tuesday()
        {
            if (_unitSet)
            {
                throw new ScheduleException("schedule unit already set");
            }
            _unit = TimeUnit.WEEKS;
            _targetDayOfWeek = DayOfWeek.Tuesday;
            _unitSet = true;
            return this;
        }

        public Schedule Wednesday()
        {
            if (_unitSet)
            {
                throw new ScheduleException("schedule unit already set");
            }
            _unit = TimeUnit.WEEKS;
            _targetDayOfWeek = DayOfWeek.Wednesday;
            _unitSet = true;
            return this;
        }

        public Schedule Thursday()
        {
            if (_unitSet)
            {
                throw new ScheduleException("schedule unit already set");
            }
            _unit = TimeUnit.WEEKS;
            _targetDayOfWeek = DayOfWeek.Thursday;
            _unitSet = true;
            return this;
        }

        public Schedule Friday()
        {
            if (_unitSet)
            {
                throw new ScheduleException("schedule unit already set");
            }
            _unit = TimeUnit.WEEKS;
            _targetDayOfWeek = DayOfWeek.Friday;
            _unitSet = true;
            return this;
        }

        public Schedule Saturday()
        {
            if (_unitSet)
            {
                throw new ScheduleException("schedule unit already set");
            }
            _unit = TimeUnit.WEEKS;
            _targetDayOfWeek = DayOfWeek.Saturday;
            _unitSet = true;
            return this;
        }

        public Schedule Sunday()
        {
            if (_unitSet)
            {
                throw new ScheduleException("schedule unit already set");
            }
            _unit = TimeUnit.WEEKS;
            _targetDayOfWeek = DayOfWeek.Sunday;
            _unitSet = true;
            return this;
        }
        
        public Schedule Month()
        {
            if (_interval != 1)
            {
                throw new IntervalException("use Months() instead");
            }
            return Months();
        }

        public Schedule Months()
        {
            if (_unitSet)
            {
                throw new ScheduleException("schedule unit already set");
            }
            _unit = TimeUnit.MONTHS;
            _unitSet = true;
            return this;
        }

        public Schedule January()
        {
            if (_unitSet)
            {
                throw new ScheduleException("schedule unit already set");
            }
            _unit = TimeUnit.YEARS;
            _targetMonth = 1;
            _unitSet = true;
            return this;
        }

        public Schedule February()
        {
            if (_unitSet)
            {
                throw new ScheduleException("schedule unit already set");
            }
            _unit = TimeUnit.YEARS;
            _targetMonth = 2;
            _unitSet = true;
            return this;
        }

        public Schedule March()
        {
            if (_unitSet)
            {
                throw new ScheduleException("schedule unit already set");
            }
            _unit = TimeUnit.YEARS;
            _targetMonth = 3;
            _unitSet = true;
            return this;
        }

        public Schedule April()
        {
            if (_unitSet)
            {
                throw new ScheduleException("schedule unit already set");
            }
            _unit = TimeUnit.YEARS;
            _targetMonth = 4;
            _unitSet = true;
            return this;
        }

        public Schedule May()
        {
            if (_unitSet)
            {
                throw new ScheduleException("schedule unit already set");
            }
            _unit = TimeUnit.YEARS;
            _targetMonth = 5;
            _unitSet = true;
            return this;
        }

        public Schedule June()
        {
            if (_unitSet)
            {
                throw new ScheduleException("schedule unit already set");
            }
            _unit = TimeUnit.YEARS;
            _targetMonth = 6;
            _unitSet = true;
            return this;
        }

        public Schedule July()
        {
            if (_unitSet)
            {
                throw new ScheduleException("schedule unit already set");
            }
            _unit = TimeUnit.YEARS;
            _targetMonth = 7;
            _unitSet = true;
            return this;
        }

        public Schedule August()
        {
            if (_unitSet)
            {
                throw new ScheduleException("schedule unit already set");
            }
            _unit = TimeUnit.YEARS;
            _targetMonth = 8;
            _unitSet = true;
            return this;
        }

        public Schedule September()
        {
            if (_unitSet)
            {
                throw new ScheduleException("schedule unit already set");
            }
            _unit = TimeUnit.YEARS;
            _targetMonth = 9;
            _unitSet = true;
            return this;
        }

        public Schedule October()
        {
            if (_unitSet)
            {
                throw new ScheduleException("schedule unit already set");
            }
            _unit = TimeUnit.YEARS;
            _targetMonth = 10;
            _unitSet = true;
            return this;
        }

        public Schedule November()
        {
            if (_unitSet)
            {
                throw new ScheduleException("schedule unit already set");
            }
            _unit = TimeUnit.YEARS;
            _targetMonth = 11;
            _unitSet = true;
            return this;
        }

        public Schedule December()
        {
            if (_unitSet)
            {
                throw new ScheduleException("schedule unit already set");
            }
            _unit = TimeUnit.YEARS;
            _targetMonth = 12;
            _unitSet = true;
            return this;
        }
        
        public Schedule Year()
        {
            if (_interval != 1)
            {
                throw new IntervalException("use Years() instead");
            }
            return Years();
        }

        public Schedule Years()
        {
            if (_unitSet)
            {
                throw new ScheduleException("schedule unit already set");
            }
            _unit = TimeUnit.YEARS;
            _unitSet = true;
            return this;
        }

        public Schedule At(string timestamp)
        {
            _usingTargetTime = true;
            if (!_repeat && !_unitSet)
            {
                _unit = TimeUnit.YEARS;
                _unitSet = true;
            }

            if (!_unitSet)
            {
                throw new ScheduleException("schedule unit not set");
            }

            if (_unit == TimeUnit.MINUTES)
            {
                if (!Regex.IsMatch(timestamp, "^:[0-5]\\d$"))
                {
                    throw new TimeFormatException("invalid time format");
                }
            } else if (_unit == TimeUnit.HOURS)
            {
                if (!Regex.IsMatch(timestamp, "^([0-5]\\d)?:[0-5]\\d$"))
                {
                    throw new TimeFormatException("invalid time format");
                }
            } else if (_unit == TimeUnit.DAYS || _unit == TimeUnit.WEEKS)
            {
                if (!Regex.IsMatch(timestamp, "^([0-2]\\d:)?[0-5]\\d:[0-5]\\d$"))
                {
                    throw new TimeFormatException("invalid time format");
                }
            } else if (_unit == TimeUnit.MONTHS)
            {
                if (!Regex.IsMatch(timestamp, "^(-[0-3]\\d)((\\s[0-2]\\d):([0-5]\\d)(:[0-5]\\d)?)?$"))
                {
                    throw new TimeFormatException("invalid time format");
                }
                _usingTargetDate = true;
            } else if (_unit == TimeUnit.YEARS)
            {
                if (!Regex.IsMatch(timestamp, "^([0-1]\\d)?(-[0-3]\\d)((\\s[0-2]\\d):([0-5]\\d)(:[0-5]\\d)?)?$"))
                {
                    throw new TimeFormatException("invalid time format");
                }
                _usingTargetDate = true;
            }
            // Split on colon, whitespace and hyphen
            var values = Regex.Split(timestamp, ":|\\s|-");
            if (values.Length == 5)
            {
                if (!string.IsNullOrEmpty(values[0]))
                {
                    _targetMonth = int.Parse(values[0]);
                }
                _targetDay = int.Parse(values[1]);
                _targetHour = int.Parse(values[2]);
                _targetMinute = int.Parse(values[3]);
                _targetSecond = int.Parse(values[4]);
            } else if (values.Length == 4)
            {
                if (!string.IsNullOrEmpty(values[0]))
                {
                    _targetMonth = int.Parse(values[0]);
                }
                _targetDay = int.Parse(values[1]);
                _targetHour = int.Parse(values[2]);
                _targetMinute = int.Parse(values[3]);
            } else if (values.Length == 3)
            {
                _targetHour = int.Parse(values[0]);
                _targetMinute = int.Parse(values[1]);
                _targetSecond = int.Parse(values[2]);
            } else if (values.Length == 2 && _unit == TimeUnit.MINUTES)
            {
                _targetSecond = int.Parse(values[1]);
            } else if (values.Length == 2 && _unit == TimeUnit.HOURS)
            {
                if (string.IsNullOrEmpty(values[0]))
                {
                    _targetMinute = int.Parse(values[1]);
                }
                else
                {
                    _targetMinute = int.Parse(values[0]);
                    _targetSecond = int.Parse(values[1]);
                }
            } else if (values.Length == 2 && (_unit == TimeUnit.DAYS || _unit == TimeUnit.WEEKS))
            {
                _targetHour = int.Parse(values[0]);
                _targetMinute = int.Parse(values[1]);
            } else if (values.Length == 2 && (_unit == TimeUnit.MONTHS || _unit == TimeUnit.YEARS))
            {
                if (!string.IsNullOrEmpty(values[0]))
                {
                    _targetMonth = int.Parse(values[0]);
                }

                _targetDay = int.Parse(values[1]);
            }
            return this;
        }

        public Schedule Run(Action action)
        {
            _action = action;
            _nextExecution = NextExecutionTimestamp();
            // Console.WriteLine(DateTime.Now.ToString("u") + " scheduled first to " + _nextExecution);
            _coreScheduler.AddTask(this);
            return this;
        }

        public void Cancel()
        {
            _alive = false;
        }

        public static void Shutdown()
        {
            _coreScheduler.Kill();
        }

        private DateTime NextExecutionTimestamp()
        {
            const int daysPerWeek = 7;
            if (_nextExecution == DateTime.MinValue && !_usingTargetTime && !_usingTargetDate)
            {
                // first execution, untimed
                return DateTime.Now;
            } else if (_nextExecution == DateTime.MinValue)
            {
                // date part of the object, time is zero, which means midnight
                var now = DateTime.Now;
                var next = now;
                // first execution, timed
                if (_unit == TimeUnit.SECONDS)
                {
                    next = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second)
                        .AddSeconds(1);
                } else if (_unit == TimeUnit.MINUTES)
                {
                    next = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, _targetSecond);
                } else if (_unit == TimeUnit.HOURS)
                {
                    next = new DateTime(now.Year, now.Month, now.Day, now.Hour, _targetMinute, _targetSecond);
                } else if (_unit == TimeUnit.DAYS)
                {
                    next = new DateTime(now.Year, now.Month, now.Day, _targetHour, _targetMinute, _targetSecond);
                } else if (_unit == TimeUnit.WEEKS)
                {
                    var daysToAdd = (_targetDayOfWeek - next.DayOfWeek + daysPerWeek) % daysPerWeek;
                    next = new DateTime(now.Year, now.Month, now.Day, _targetHour, _targetMinute, _targetSecond)
                        .Add(daysToAdd, TimeUnit.DAYS);
                } else if (_unit == TimeUnit.MONTHS || _unit == TimeUnit.YEARS)
                {
                    next = new DateTime(now.Year, _targetMonth, _targetDay, _targetHour, _targetMinute, _targetSecond);
                }
                while (next < now)
                {
                    next = next.Add(1, _unit);
                }
                return next;
            }
            else
            {
                // next execution, just add interval
                return _nextExecution.Add(_interval, _unit);
            }
        }
    }
}