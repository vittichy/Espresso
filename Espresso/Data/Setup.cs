using Dtc.Common.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Espresso.Data
{
    /// <summary>
    /// read setup data from json file.
    /// 
    /// format:
    /// {
    ///     "user":"DESKTOP-2R9PJDT\\tichy",
    ///     "daysOff":
    ///     [ 
    ///        "2019-10-02 off1", 
    ///        "2019-10-03 prichod", 
    ///     ],
    ///     "stateHolidays":
    ///     [ 
    ///       "2019-09-09 XXXX",
    ///     ],
    ///     "remarks":
    ///     [
    ///       "2019-09-09 XXXX",
    ///     ]
    /// }
    /// </summary>
    public class Setup
    {
        const string DATE_FORMAT = "yyyy-MM-dd";

        /// <summary>
        /// tracked user name
        /// </summary>
        public readonly string UserName;

        /// <summary>
        /// sef of state holidays
        /// </summary>
        public List<WorkDay> StateHolidays = new List<WorkDay>();

        /// <summary>
        /// set of days off
        /// </summary>
        public List<WorkDay> DaysOff = new List<WorkDay>();

        /// <summary>
        /// remarks for days
        /// </summary>
        public List<WorkDay> DayRemarks = new List<WorkDay>();

        public bool IsValid { get { return !string.IsNullOrEmpty(UserName); } }


        public Setup(string json)
        {
            if (!string.IsNullOrEmpty(json))
            {
                var jObject = JObject.Parse(json);

                UserName = jObject["user"]?.ToString();

                var daysOff = jObject["daysOff"] as JArray;
                daysOff?.ToList().ForEach(p => DecodeWorkDay(p, DaysOff));

                var holidays = jObject["stateHolidays"] as JArray;
                holidays?.ToList().ForEach(p => DecodeWorkDay(p, StateHolidays));

                var remarks = jObject["remarks"] as JArray;
                remarks?.ToList().ForEach(p => DecodeWorkDay(p, DayRemarks));
            }
        }


        private void DecodeWorkDay(JToken jToken, List<WorkDay> workDaySet)
        {
            if ((jToken != null) && (workDaySet != null))
            {
                var value = jToken.Value<string>();
                if (value != null)
                {
                    var splitted = value.Split2Half(' ');
                    if (splitted != null && !string.IsNullOrEmpty(splitted.Item1))
                    {
                        var date = DateTime.ParseExact(splitted.Item1, DATE_FORMAT, CultureInfo.InvariantCulture);
                        workDaySet.Add(new WorkDay(date, splitted.Item2?.Trim()));
                    }
                }
            }
        }


        internal void WriteInfo()
        {
            Console.WriteLine($"User:{UserName}, stateHolidays:{StateHolidays.Count()}, daysOff:{DaysOff.Count()}, remarks:{DayRemarks.Count()}");
        }
    }
}
