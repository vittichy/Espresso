using Dtc.Common.Extensions;
using Espresso.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceProcess;

namespace Espresso
{
    class Program
    {
        private const string WEEK_HEADER = "----------------------D-----W-----M--";

        static void Main(string[] args)
        {
            // chci tecky misto carek u desetinne tecky u double: https://stackoverflow.com/questions/9160059/set-up-dot-instead-of-comma-in-numeric-values
            var customCulture = (CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            var common = new EspressoCommon.Common();

            if (!File.Exists(common.LogFilePath))
            {
                Console.WriteLine($"File not found: {common.LogFilePath}!");
                return;
            }

            var logLines = File.ReadAllLines(common.LogFilePath);
            if (logLines == null || logLines.Count() <= 0)
            {
                Console.WriteLine("No lines, no fun.");
                return;
            }

            var setupFile =  new Setup(common.ReadSetupFile());
            if (!setupFile.IsValid)
            {
                Console.WriteLine("Invalid setup file.");
                return;
            }

            setupFile.WriteInfo();
            ShowIt(logLines, setupFile);
        }


        private static void ShowIt(string[] lines, Setup setup)
        {
            const string DATE_FORMAT = "yyyy-MM-dd HH:mm:ss";

            const double HOURS_PER_DAY = 8.5;

            // rozparsovane radky datetime + message
            var log = lines
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                            .Select(p => new
                                            {
                                                date = DateTime.ParseExact(p.Substring(0, DATE_FORMAT.Length), DATE_FORMAT, CultureInfo.InvariantCulture),
                                                message = (p + new string(' ', DATE_FORMAT.Length)).Remove(0, DATE_FORMAT.Length).Trim()
                                            })
                                .ToList();

            // skupiny za jednotlive dny
            var groupsByDay = log.GroupBy(p => p.date.Date);
            var dayFirst = groupsByDay.Min(p => p.Key);
            var dayLast = groupsByDay.Max(p => p.Key);

            var day = dayFirst;
            var weekHours = 0d;
            var weekHoursTeo = 0d;
            var monthHours = 0d;
            var monthHoursTeo = 0d;
            do
            {
                var dayHours = 0d;

                // teoreticke pracovni dny 
                if (day.DayOfWeek != DayOfWeek.Saturday && day.DayOfWeek != DayOfWeek.Sunday)
                {
                    weekHoursTeo += HOURS_PER_DAY;
                    monthHoursTeo += HOURS_PER_DAY;
                }

                var logByDay = groupsByDay.FirstOrDefault(p => p.Key == day);

                var logByDayForUser = (logByDay != null) ? logByDay.Where(p => p.message.StartsWith(setup.UserName))
                                                                            .Select(p => new DateMessage(p.date, p.message))
                                                                                .ToList()
                                                             : new List<DateMessage>();

                var unlocks = logByDayForUser.Where(p => p.Message.EndsWith("SessionUnlock") || p.Message.EndsWith("SessionLogon"))
                                                .Select(p => p.Date)
                                                    .ToList();
                var minUnlock = GetMinTime(unlocks);

                DateTime? maxLock;
                if (day == DateTime.Today)
                    maxLock = DateTime.Now;
                else
                {
                    var locks = logByDayForUser.Where(p => p.Message.EndsWith("SessionLock") || p.Message.EndsWith("SessionLogoff"))
                                                    .Select(p => p.Date)
                                                        .ToList();
                    maxLock = GetMaxTime(locks);
                }

                if (minUnlock.HasValue && maxLock.HasValue && minUnlock.Value < maxLock.Value)
                    dayHours = (maxLock.Value - minUnlock.Value).TotalHours;

                var dayRemark = setup.DayRemarks.FirstOrDefault(p => p.Date.Date == day.Date);

                var dayOff = setup.DaysOff.FirstOrDefault(p => p.Date.Date == day.Date);
                if (dayOff != null)
                    dayHours += HOURS_PER_DAY;

                var stateHoliday = setup.StateHolidays.FirstOrDefault(p => p.Date.Date == day.Date);
                if (stateHoliday != null)
                    dayHours += HOURS_PER_DAY;

                weekHours += dayHours;
                monthHours += dayHours;

                var monthLoss = monthHours - monthHoursTeo;

                if (day.DayOfWeek == DayOfWeek.Monday)
                    Console.WriteLine(WEEK_HEADER);

                // {dayHours,4:#0.0} => formatuj na delku 4, #-cislo nebo mezera, 0-povinne cislo
                Console.Write($"{day:ddd dd.MM} {AsStr(minUnlock)}-{AsStr(maxLock)} ");
                if (dayHours != 0)
                {
                    if (dayOff != null)
                        Console.ForegroundColor = ConsoleColor.Green;
                    else if (stateHoliday != null)
                        Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write($"{dayHours,4:#0.0}");
                    Console.ResetColor();
                    Console.Write($" {(weekHours - weekHoursTeo),5:+##0.0;-##0.0;0} {monthLoss,5:+##0.0;-##0.0;0}");
                }
                else
                    Console.Write(new string(' ', 16)); // day without working hours

                WriteBlueText(dayOff);          // dayoff
                WriteBlueText(stateHoliday);    // state holiday
                WriteBlueText(dayRemark);       // remark for day
                Console.WriteLine();

                var lastDay = (day == dayLast);

                // suma za week
                if (day.DayOfWeek == DayOfWeek.Sunday || lastDay)
                {
                    const double WEEK_TIME = 5 * HOURS_PER_DAY;
                    var weekLoss = weekHours - WEEK_TIME;
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write($"    w: {weekHours:0.0} ({weekLoss:+#0.0;-#0.0;0})");
                    Console.ResetColor();
                    Console.WriteLine();
                    weekHours = 0;
                    weekHoursTeo = 0;
                }

                // konec mesice
                if (day.Month != (day.AddDays(1).Month) || lastDay)
                {
                    //var monthLoss = monthHours - monthHoursTeo;
                    Console.BackgroundColor = ConsoleColor.Blue;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write($"    m: {day:MM.yyyy}: {monthHours:0.0} ({monthLoss:+#0.0;-#0.0;0})".ExtendToLength(WEEK_HEADER.Length, ' '));
                    Console.ResetColor();
                    Console.WriteLine();
                    monthHours = 0;
                    monthHoursTeo = 0;
                }

                day = day.AddDays(1);
            }
            while (day <= dayLast);

            if (Debugger.IsAttached)
                Console.ReadKey();
        }


        private static void WriteBlueText(WorkDay workDay)
        {
            if (workDay != null)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write($" {workDay?.Text?.Trim()}"); 
                Console.ResetColor();
            }
        }

        private static string AsStr(DateTime? value)
        {
            return value.HasValue ? $"{value:HH:mm}" : "     ";
        }


        private static DateTime? GetMinTime(IEnumerable<DateTime> enumerable)
        {
            if (enumerable == null || !enumerable.Any())
                return null;
            return enumerable.Min();
        }


        private static DateTime? GetMaxTime(IEnumerable<DateTime> enumerable)
        {
            if (enumerable == null || !enumerable.Any())
                return null;
            return enumerable.Max();
        }

        private static SessionChangeReason SessionChangeReasonFromStr(string s)
        {
            Enum.TryParse(s, out SessionChangeReason result);
            return result;
        }

    }
}
