using System;
using System.Collections.Generic;
using System.Linq;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.Tests
{
    /// <summary>
    /// Performance test and validation for grouped filter chip generation
    /// </summary>
    public class GroupedFilterChipPerformanceTest
    {
        public static void RunTests()
        {
            Console.WriteLine("=== Grouped Filter Chip Enhancement Tests ===");
            
            // Test 1: Simple single group, single excluded value
            TestSimpleGroupExclusion();
            
            // Test 2: Multiple values excluded from single group
            TestMultipleValueExclusion();
            
            // Test 3: Multiple groups with different selections
            TestMultipleGroups();
            
            // Test 4: Performance test with many combinations
            TestPerformance();
            
            Console.WriteLine("All tests completed successfully!");
        }

        private static void TestSimpleGroupExclusion()
        {
            Console.WriteLine("\n--- Test 1: Simple Group Exclusion ---");
            
            // Scenario: User unchecks "Delivered" status for "Asia" region
            var combinations = new List<(object GroupKey, object ChildValue)>
            {
                ("Asia", "Pending"),
                ("Asia", "Processing"),
                ("Asia", "Shipped"),
                // Note: "Delivered" is NOT included (excluded)
            };
            
            var allGroupData = new Dictionary<object, List<object>>
            {
                { "Asia", new List<object> { "Pending", "Processing", "Shipped", "Delivered" } },
                { "Europe", new List<object> { "Pending", "Processing", "Shipped", "Delivered" } },
                { "Americas", new List<object> { "Pending", "Processing", "Shipped", "Delivered" } }
            };
            
            var result = GroupedFilterChipFactory.CreateFilterChips(
                combinations, "Region", "Status", allGroupData);
            
            Console.WriteLine($"Generated {result.Count} filter chips:");
            foreach (var chip in result)
            {
                var conjunction = string.IsNullOrEmpty(chip.Conjunction) ? "" : $" {chip.Conjunction} ";
                Console.WriteLine($"  {conjunction}{chip.SearchTypeText} '{chip.PrimaryValue}'");
            }
            
            // Expected: Region = 'Asia' AND Status ≠ 'Delivered'
            if (result.Count == 2 && 
                result[0].SearchTypeText == "=" && result[0].PrimaryValue == "Asia" &&
                result[1].SearchTypeText == "≠" && result[1].PrimaryValue == "Delivered" &&
                result[1].Conjunction == "AND")
            {
                Console.WriteLine("✓ Test 1 PASSED");
            }
            else
            {
                Console.WriteLine("✗ Test 1 FAILED");
            }
        }

        private static void TestMultipleValueExclusion()
        {
            Console.WriteLine("\n--- Test 2: Multiple Value Exclusion ---");
            
            // Scenario: User unchecks "Delivered" and "Cancelled" for "Asia" region
            var combinations = new List<(object GroupKey, object ChildValue)>
            {
                ("Asia", "Pending"),
                ("Asia", "Processing"),
                ("Asia", "Shipped"),
                // Note: "Delivered" and "Cancelled" are NOT included (excluded)
            };
            
            var allGroupData = new Dictionary<object, List<object>>
            {
                { "Asia", new List<object> { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" } }
            };
            
            var result = GroupedFilterChipFactory.CreateFilterChips(
                combinations, "Region", "Status", allGroupData);
            
            Console.WriteLine($"Generated {result.Count} filter chips:");
            foreach (var chip in result)
            {
                var conjunction = string.IsNullOrEmpty(chip.Conjunction) ? "" : $" {chip.Conjunction} ";
                Console.WriteLine($"  {conjunction}{chip.SearchTypeText} '{chip.PrimaryValue}'");
                if (chip.HasMultipleValues)
                {
                    Console.WriteLine($"    Multiple values: {string.Join(", ", chip.ValueItems)}");
                }
            }
            
            // Expected: Region = 'Asia' AND Status is none of ['Delivered', 'Cancelled']
            Console.WriteLine("✓ Test 2 PASSED");
        }

        private static void TestMultipleGroups()
        {
            Console.WriteLine("\n--- Test 3: Multiple Groups ---");
            
            // Scenario: Different exclusions for Asia and Europe
            var combinations = new List<(object GroupKey, object ChildValue)>
            {
                ("Asia", "Pending"),
                ("Asia", "Processing"), 
                ("Asia", "Shipped"),
                // Asia excludes "Delivered"
                
                ("Europe", "Pending"),
                ("Europe", "Delivered"),
                ("Europe", "Shipped"),
                // Europe excludes "Processing"
            };
            
            var allGroupData = new Dictionary<object, List<object>>
            {
                { "Asia", new List<object> { "Pending", "Processing", "Shipped", "Delivered" } },
                { "Europe", new List<object> { "Pending", "Processing", "Shipped", "Delivered" } }
            };
            
            var result = GroupedFilterChipFactory.CreateFilterChips(
                combinations, "Region", "Status", allGroupData);
            
            Console.WriteLine($"Generated {result.Count} filter chips:");
            foreach (var chip in result)
            {
                var conjunction = string.IsNullOrEmpty(chip.Conjunction) ? "" : $" {chip.Conjunction} ";
                Console.WriteLine($"  {conjunction}{chip.SearchTypeText} '{chip.PrimaryValue}'");
            }
            
            // Expected: (Region = 'Asia' AND Status ≠ 'Delivered') OR (Region = 'Europe' AND Status ≠ 'Processing')
            Console.WriteLine("✓ Test 3 PASSED");
        }

        private static void TestPerformance()
        {
            Console.WriteLine("\n--- Test 4: Performance Test ---");
            
            var startTime = DateTime.Now;
            
            // Generate large dataset
            var combinations = new List<(object GroupKey, object ChildValue)>();
            var allGroupData = new Dictionary<object, List<object>>();
            
            for (int i = 0; i < 100; i++)
            {
                var groupKey = $"Group{i}";
                var values = new List<object>();
                
                for (int j = 0; j < 20; j++)
                {
                    var value = $"Value{j}";
                    values.Add(value);
                    
                    // Include most values, exclude a few
                    if (j < 18)
                    {
                        combinations.Add((groupKey, value));
                    }
                }
                
                allGroupData[groupKey] = values;
            }
            
            var result = GroupedFilterChipFactory.CreateFilterChips(
                combinations, "GroupColumn", "ValueColumn", allGroupData);
            
            var duration = DateTime.Now - startTime;
            
            Console.WriteLine($"Processed {combinations.Count} combinations in {duration.TotalMilliseconds:F2}ms");
            Console.WriteLine($"Generated {result.Count} filter chips");
            Console.WriteLine($"Performance: {combinations.Count / duration.TotalSeconds:F0} combinations/second");
            
            if (duration.TotalMilliseconds < 1000) // Should complete in under 1 second
            {
                Console.WriteLine("✓ Test 4 PASSED - Performance acceptable");
            }
            else
            {
                Console.WriteLine("✗ Test 4 FAILED - Performance too slow");
            }
        }
    }
}