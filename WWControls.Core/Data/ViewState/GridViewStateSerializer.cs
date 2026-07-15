using System.Text.Json;
using System.Text.Json.Serialization;

namespace WWControls.Core
{
    /// <summary>
    /// Converts a <see cref="GridViewState"/> to and from its on-disk JSON form. This is the single
    /// definition of the saved-view file format.
    /// </summary>
    /// <remarks>
    /// System.Text.Json only — no polymorphic type discriminators, enums written as names. The
    /// options are case-insensitive on read and omit null properties on write, so a filters-only
    /// or layout-only view stays compact and forward/backward tolerant.
    /// </remarks>
    public static class GridViewStateSerializer
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() },
        };

        /// <summary>Serializes a view state to indented JSON.</summary>
        public static string Serialize(GridViewState state)
        {
            return JsonSerializer.Serialize(state, Options);
        }

        /// <summary>Deserializes a view state from JSON. Returns <c>null</c> for null/blank input.</summary>
        public static GridViewState Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonSerializer.Deserialize<GridViewState>(json, Options);
        }
    }
}
