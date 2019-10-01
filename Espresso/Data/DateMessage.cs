using System;
using System.Diagnostics;

namespace Espresso.Data
{
    [DebuggerDisplay("Date={Date} | Message={Message}")]
    public class DateMessage
    {
        public readonly DateTime Date;
        public readonly string Message;

        public DateMessage(DateTime date, string message)
        {
            Date = date;
            Message = message;
        }


        public bool IsLockOrLogoff()
        {
            return !string.IsNullOrEmpty(Message) && (Message.EndsWith("SessionLock") || Message.EndsWith("SessionLogoff"));
        }
        public bool IsUnLockOrLogon()
        {
            return !string.IsNullOrEmpty(Message) && (Message.EndsWith("SessionUnlock") || Message.EndsWith("SessionLogon"));
        }
    }
}
