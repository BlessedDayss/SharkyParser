using System.ComponentModel;

namespace SharkyParser.Core.Enums;

public enum LogType
{
    [Description("Installation Logs")]
    Installation = 0,
    
    [Description("Update Logs")]
    Update = 1,
    
    [Description("RabbitMQ Logs")]
    RabbitMq = 2,
    
    [Description("IIS Logs")]
    IIS = 3,
    
    [Description("TeamCity Logs")]
    TeamCity = 4
}