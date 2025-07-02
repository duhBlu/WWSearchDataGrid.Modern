using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using WWSearchDataGrid.Modern.SampleApp.Models;

namespace WWSearchDataGrid.Modern.SampleApp
{
    public class DataItem : ObservableObject
    {
        // Essential business context (minimal)
        public string CustomerName { get; set; }
        public string ProductCategory { get; set; }
        public string Region { get; set; }

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
        public string StringValue { get; set; }
        public char CharValue { get; set; }
        public char? NullableCharValue { get; set; }

        // Date and time types
        public DateTime DateTimeValue { get; set; }
        public DateTime? NullableDateTimeValue { get; set; }
        public TimeSpan TimeSpanValue { get; set; }
        public TimeSpan? NullableTimeSpanValue { get; set; }

        // GUID types
        public Guid GuidValue { get; set; }
        public Guid? NullableGuidValue { get; set; }

        // Enum types
        public OrderStatus StatusValue { get; set; }
        public OrderStatus? NullableStatusValue { get; set; }
        public Priority PriorityValue { get; set; }
        public Priority? NullablePriorityValue { get; set; }

        // Business datetimes with time precision
        public DateTime OrderDateTime { get; set; }
        public DateTime? ShippedDateTime { get; set; }
        public DateTime DueDateTime { get; set; }
        public DateTime? CompletedDateTime { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public TimeSpan? DeliveryTime { get; set; }
    }
}