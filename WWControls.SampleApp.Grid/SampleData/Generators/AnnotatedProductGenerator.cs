using System;
using WWControls.SampleApp.Grid.Models;

namespace WWControls.SampleApp.Grid.SampleData.Generators
{
    public static class AnnotatedProductGenerator
    {
        private static readonly char[] Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

        private static readonly string[] Adjectives =
            { "Classic", "Premium", "Standard", "Heavy-Duty", "Compact", "Deluxe" };

        private static readonly string[] Nouns =
            { "Hinge", "Panel", "Drawer Slide", "Knob", "Clamp", "Router Bit", "Sealer", "Bracket" };

        public static AnnotatedProduct Create(Random rnd, int index)
        {
            // Sku / SupportPhone hold raw values — the SimpleMask on the column inserts the
            // dash / parentheses at display time.
            return new AnnotatedProduct
            {
                Sku = $"{Letters[rnd.Next(26)]}{Letters[rnd.Next(26)]}{rnd.Next(1000, 9999)}",
                Name = $"{Adjectives[rnd.Next(Adjectives.Length)]} {Nouns[rnd.Next(Nouns.Length)]}",
                Category = (ProductCategory)rnd.Next(Enum.GetValues(typeof(ProductCategory)).Length),
                UnitPrice = Math.Round((decimal)(rnd.NextDouble() * 480 + 20), 2),
                Discount = Math.Round(rnd.NextDouble() * 0.35, 3),
                Quantity = rnd.Next(0, 500),
                RestockDate = DateTime.Today.AddDays(rnd.Next(-60, 120)),
                SupportPhone = $"555{rnd.Next(1000000, 9999999)}",
                Discontinued = rnd.NextDouble() < 0.15,
            };
        }
    }
}
