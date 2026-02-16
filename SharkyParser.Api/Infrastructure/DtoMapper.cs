using System.ComponentModel;
using SharkyParser.Api.DTOs;
using SharkyParser.Core;
using SharkyParser.Core.Enums;

namespace SharkyParser.Api.Infrastructure;

/// <summary>
/// Maps Core domain models to API DTOs.
/// Single responsibility: conversion between layers.
/// </summary>
public static class DtoMapper
{
    public static LogEntryDto ToDto(LogEntry entry)
    {
        return new LogEntryDto
        {
            Timestamp = entry.Timestamp.ToString("O"),
            Level = entry.Level,
            Message = entry.Message,
            Source = entry.Source,
            StackTrace = entry.StackTrace,
            LineNumber = entry.LineNumber,
            FilePath = entry.FilePath,
            RawData = entry.RawData
        };
    }

    public static LogStatisticsDto ToDto(LogStatistics statistics)
    {
        return new LogStatisticsDto(
            statistics.TotalCount,
            statistics.ErrorCount,
            statistics.WarningCount,
            statistics.InfoCount,
            statistics.DebugCount,
            statistics.IsHealthy,
            statistics.ExtendedData
        );
    }

    public static LogTypeDto ToDto(LogType type)
    {
        return new LogTypeDto(
            (int)type,
            type.ToString(),
            GetDescription(type)
        );
    }

    private static string GetDescription(LogType type)
    {
        var field = typeof(LogType).GetField(type.ToString());
        if (field == null) return type.ToString();

        var attr = field.GetCustomAttributes(typeof(DescriptionAttribute), false)
            .OfType<DescriptionAttribute>()
            .FirstOrDefault();

        return attr?.Description ?? type.ToString();
    }
}
