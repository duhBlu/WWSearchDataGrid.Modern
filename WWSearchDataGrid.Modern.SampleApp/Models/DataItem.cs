using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using WWSearchDataGrid.Modern.SampleApp.Models;

namespace WWSearchDataGrid.Modern.SampleApp
{
    public class DataItem : ObservableObject
    {
        public string? CustomerName { get; set; }
        
        // Complete .NET data type matrix - Boolean types
        public bool BoolValue { get; set; }
        public bool? NullableBoolValue { get; set; }

        // Integer types
        public int IntValue { get; set; }
        public int? NullableIntValue { get; set; }
        public long LongValue { get; set; }
        public long? NullableLongValue { get; set; }
        public short ShortValue { get; set; }
        public short? NullableShortValue { get; set; }
        public byte ByteValue { get; set; }
        public byte? NullableByteValue { get; set; }

        // Floating-point and decimal types
        public float FloatValue { get; set; }
        public float? NullableFloatValue { get; set; }
        public double DoubleValue { get; set; }
        public double? NullableDoubleValue { get; set; }
        public decimal DecimalValue { get; set; }
        public decimal? NullableDecimalValue { get; set; }

        // Text types
        public string? StringValue { get; set; }
        public char CharValue { get; set; }
        public char? NullableCharValue { get; set; }

        // Date and time types
        public DateTime DateTimeValue { get; set; }
        public DateTime? NullableDateTimeValue { get; set; }
        public TimeSpan TimeSpanValue { get; set; }
        public TimeSpan? NullableTimeSpanValue { get; set; }

        // Enum types
        public Priority PriorityValue { get; set; }
        public Priority? NullablePriorityValue { get; set; }
    }
}