using System;
using System.Diagnostics;

namespace Espresso.Data
{
    [DebuggerDisplay("Date:{Date}, Text:{text}")]
    public class WorkDay
    {
        public readonly DateTime Date;
        public readonly string Text;

        public WorkDay(DateTime date, string text)
        {
            Date = date;
            Text = text;
        }
    }
}