using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace WWControls.Wpf
{
    /// <summary>
    /// Abstract <see cref="FrameworkContentElement"/> base for descriptor classes that participate
    /// in WPF binding and DataContext inheritance but never render visually. Hides inherited
    /// <see cref="FrameworkContentElement"/> properties that have no meaning on a non-rendering
    /// descriptor (cursor, focus visual, styling, triggers, resources, etc.) so XAML IntelliSense
    /// surfaces only the descriptor's own surface.
    /// </summary>
    /// <remarks>
    /// Hiding is metadata-only — the inherited properties still exist and work if accessed.
    /// </remarks>
    public abstract class ColumnDescriptorElement : FrameworkContentElement
    {
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new Style Style
        {
            get => base.Style;
            set => base.Style = value;
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new bool OverridesDefaultStyle
        {
            get => base.OverridesDefaultStyle;
            set => base.OverridesDefaultStyle = value;
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new ResourceDictionary Resources
        {
            get => base.Resources;
            set => base.Resources = value;
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new BindingGroup BindingGroup
        {
            get => base.BindingGroup;
            set => base.BindingGroup = value;
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new System.Windows.Input.Cursor Cursor
        {
            get => base.Cursor;
            set => base.Cursor = value;
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new bool ForceCursor
        {
            get => base.ForceCursor;
            set => base.ForceCursor = value;
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new Style FocusVisualStyle
        {
            get => base.FocusVisualStyle;
            set => base.FocusVisualStyle = value;
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new System.Windows.Input.InputBindingCollection InputBindings => base.InputBindings;

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new System.Windows.Input.CommandBindingCollection CommandBindings => base.CommandBindings;
    }
}
