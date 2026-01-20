using System.ComponentModel;

namespace SharkyParser.Core.Enums;

public enum StackTraceMode
{
    [Description("All lines after timestamp go to stack trace")]
    AllToStackTrace = 0,
    
    [Description("No stack traces - all in message")]
    NoStackTrace = 1
}