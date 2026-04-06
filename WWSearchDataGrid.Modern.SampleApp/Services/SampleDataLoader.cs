using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using WWSearchDataGrid.Modern.SampleApp.Models;

namespace WWSearchDataGrid.Modern.SampleApp.Services
{
    /// <summary>
    /// Loads anonymized sample order data from the embedded JSON file.
    /// </summary>
    public static class SampleDataLoader
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Loads all sample orders from the embedded Data/SampleOrders.json file.
        /// </summary>
        public static List<OrderItem> LoadSampleOrders()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "WWSearchDataGrid.Modern.SampleApp.Data.SampleOrders.json";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                // Fallback: try loading from file path relative to the assembly
                var assemblyDir = Path.GetDirectoryName(assembly.Location) ?? ".";
                var filePath = Path.Combine(assemblyDir, "Data", "SampleOrders.json");

                if (File.Exists(filePath))
                {
                    var fileJson = File.ReadAllText(filePath);
                    return JsonSerializer.Deserialize<List<OrderItem>>(fileJson, JsonOptions) ?? new List<OrderItem>();
                }

                throw new FileNotFoundException(
                    $"Could not find sample data. Tried embedded resource '{resourceName}' and file '{filePath}'.");
            }

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<List<OrderItem>>(json, JsonOptions) ?? new List<OrderItem>();
        }
    }
}
