using System.ComponentModel;
using SharkyParser.Api.Data.Models;
using SharkyParser.Api.DTOs;
using SharkyParser.Core;
using SharkyParser.Core.Enums;
using SharkyParser.Core.Models;

namespace SharkyParser.Api.Infrastructure;

/// <summary>
/// Maps Core domain models to API DTOs.
/// Single responsibility: conversion between layers.
/// </summary>
public static class DtoMapper
{
    public static FileRecordDto ToDto(FileRecord record) =>
        new(record.Id, record.FileName, record.FileSize, record.LogType, record.UploadedAt);

    public static LogEntryDto ToDto(LogEntry entry)
    {
        return new LogEntryDto
        {
            Timestamp = entry.Timestamp.ToString("O"),
            Level = entry.Level,
            Message = entry.Message,
            LineNumber = entry.LineNumber,
            FilePath = entry.FilePath,
            RawData = entry.RawData,
            Fields = new Dictionary<string, string>(entry.Fields)
        };
    }

    public static LogColumnDto ToDto(LogColumn column)
    {
        return new LogColumnDto(
            column.Name,
            column.Header,
            column.Description,
            column.IsPredefined
        );
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
            statistics.ExtendedData,
            statistics.IisStatistics != null ? ToDto(statistics.IisStatistics) : null
        );
    }

    private static IisLogStatisticsDto ToDto(IisLogStatistics stats)
    {
        return new IisLogStatisticsDto(
            stats.RequestsPerMinute,
            stats.TopIps,
            stats.SlowestRequests.Select(x => new SlowRequestStatsDto(
                x.Url, 
                x.Method, 
                x.DurationMs, 
                x.Timestamp, 
                x.StatusCode
            )).ToList(),
            stats.ResponseTimeDistribution
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
