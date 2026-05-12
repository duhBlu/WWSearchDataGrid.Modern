using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Base class for column editor configurations. Each concrete implementation produces a
    /// <see cref="DataTemplate"/> for the cell's display (read-only) and edit modes. When a
    /// <see cref="GridColumn"/> has its <see cref="GridColumn.EditSettings"/> set, the grid
    /// generates a <see cref="DataGridTemplateColumn"/> using these
    /// templates instead of the default text or checkbox column.
    /// </summary>
    /// <remarks>
    /// Inherits from <see cref="FrameworkContentElement"/> (not <see cref="Freezable"/>) so that
    /// XAML bindings on properties like <c>ItemsSource</c> can resolve against an inherited
    /// <c>DataContext</c>. <see cref="SearchDataGrid"/> propagates its DataContext down through
    /// <see cref="GridColumn"/> to its <see cref="GridColumn.EditSettings"/> when columns are
    /// generated, and re-propagates whenever the grid's DataContext changes.
    /// </remarks>
    public abstract class BaseEditSettings : FrameworkContentElement
    {
        /// <summary>
        /// Optional user-supplied template for the read-only display cell. When set, the library
        /// uses this template verbatim and skips <see cref="CreateDisplayTemplate"/>. Bindings
        /// inside the template should reach the owning ViewModel via
        /// <c>RelativeSource AncestorType=Window</c> (or similar) — the cell DataContext is the
        /// row item, so the column's value binds directly via the field name.
        /// </summary>
        public static readonly DependencyProperty DisplayTemplateProperty =
            DependencyProperty.Register(nameof(DisplayTemplate), typeof(DataTemplate), typeof(BaseEditSettings),
                new PropertyMetadata(null));

        /// <summary>
        /// Optional user-supplied template for the in-place edit cell. When set, the library uses
        /// this template verbatim and skips <see cref="CreateEditTemplate"/>. See
        /// <see cref="DisplayTemplate"/> for binding guidance.
        /// </summary>
        public static readonly DependencyProperty EditTemplateProperty =
            DependencyProperty.Register(nameof(EditTemplate), typeof(DataTemplate), typeof(BaseEditSettings),
                new PropertyMetadata(null));

        /// <summary>
        /// Optional <see cref="System.Windows.Style"/> applied to the display-mode element
        /// (TextBlock for most editors, CheckBox for <see cref="CheckBoxEditSettings"/>) when
        /// the library builds its default display template. Beats the library's default style.
        /// Ignored when <see cref="DisplayTemplate"/> is set (full template override wins).
        /// </summary>
        public static readonly DependencyProperty DisplayStyleProperty =
            DependencyProperty.Register(nameof(DisplayStyle), typeof(Style), typeof(BaseEditSettings),
                new PropertyMetadata(null));

        /// <summary>
        /// Optional <see cref="System.Windows.Style"/> applied to the edit-mode element
        /// (TextBox / ComboBox / DatePicker / CheckBox depending on editor) when the library builds
        /// its default edit template. Beats the library's default style. Ignored when
        /// <see cref="EditTemplate"/> is set.
        /// </summary>
        public static readonly DependencyProperty EditorStyleProperty =
            DependencyProperty.Register(nameof(EditorStyle), typeof(Style), typeof(BaseEditSettings),
                new PropertyMetadata(null));

        /// <summary>
        /// Controls when a click on a cell triggers edit mode. Defaults to
        /// <see cref="WPF.EditorShowMode.Default"/>, which inherits the grid-wide
        /// <see cref="SearchDataGrid.EditorShowMode"/>; an explicit value here overrides the
        /// grid-level setting for this column's editor.
        /// </summary>
        public static readonly DependencyProperty EditorShowModeProperty =
            DependencyProperty.Register(nameof(EditorShowMode), typeof(EditorShowMode), typeof(BaseEditSettings),
                new PropertyMetadata(EditorShowMode.Default));

        /// <summary>
        /// Controls when the editor's decoration buttons (combo toggle, spinner, calendar
        /// dropdown) are visible. Defaults to <see cref="WPF.EditorButtonShowMode.Default"/>
        /// which inherits the grid-wide <see cref="SearchDataGrid.EditorButtonShowMode"/>.
        /// </summary>
        public static readonly DependencyProperty EditorButtonShowModeProperty =
            DependencyProperty.Register(nameof(EditorButtonShowMode), typeof(EditorButtonShowMode), typeof(BaseEditSettings),
                new PropertyMetadata(EditorButtonShowMode.Default));

        public EditorShowMode EditorShowMode
        {
            get => (EditorShowMode)GetValue(EditorShowModeProperty);
            set => SetValue(EditorShowModeProperty, value);
        }

        public EditorButtonShowMode EditorButtonShowMode
        {
            get => (EditorButtonShowMode)GetValue(EditorButtonShowModeProperty);
            set => SetValue(EditorButtonShowModeProperty, value);
        }

        public DataTemplate DisplayTemplate
        {
            get => (DataTemplate)GetValue(DisplayTemplateProperty);
            set => SetValue(DisplayTemplateProperty, value);
        }

        public DataTemplate EditTemplate
        {
            get => (DataTemplate)GetValue(EditTemplateProperty);
            set => SetValue(EditTemplateProperty, value);
        }

        public Style DisplayStyle
        {
            get => (Style)GetValue(DisplayStyleProperty);
            set => SetValue(DisplayStyleProperty, value);
        }

        public Style EditorStyle
        {
            get => (Style)GetValue(EditorStyleProperty);
            set => SetValue(EditorStyleProperty, value);
        }

        /// <summary>
        /// Helper for subclasses: applies the user-supplied <see cref="DisplayStyle"/> as a
        /// local value if set; otherwise looks up the library's default style by
        /// <see cref="ComponentResourceKey"/> and applies that as a local value. Resolved at
        /// template-build time (not deferred) so it works reliably with
        /// <see cref="FrameworkElementFactory"/>, where SetResourceReference has known quirks
        /// for the StyleProperty.
        /// </summary>
        protected void ApplyDisplayStyle(FrameworkElementFactory factory, ComponentResourceKey defaultStyleKey)
        {
            var style = DisplayStyle ?? ResolveLibraryStyle(defaultStyleKey);
            if (style != null)
                factory.SetValue(FrameworkElement.StyleProperty, style);
        }

        /// <summary>
        /// Helper for subclasses: applies <see cref="EditorStyle"/> as a local value if set,
        /// else falls back to the library's keyed default Style looked up at build time.
        /// </summary>
        protected void ApplyEditorStyle(FrameworkElementFactory factory, ComponentResourceKey defaultStyleKey)
        {
            var style = EditorStyle ?? ResolveLibraryStyle(defaultStyleKey);
            if (style != null)
                factory.SetValue(FrameworkElement.StyleProperty, style);
        }

        // ComponentResourceKey lookup walks the standard resource chain (element tree → app)
        // AND, courtesy of the assembly's [ThemeInfo] attribute, falls through to the
        // library's Themes/Generic.xaml when the consumer hasn't merged anything explicitly.
        // That fallback is what makes the dropped-in defaults work without ceremony.
        private static Style ResolveLibraryStyle(ComponentResourceKey key)
        {
            if (key == null) return null;
            var app = Application.Current;
            if (app == null) return null;
            return app.TryFindResource(key) as Style;
        }

        /// <summary>
        /// Helper for subclasses that build composite editors with sub-elements (e.g. a dropdown
        /// button next to a TextBox): looks up the library-keyed default style and applies it.
        /// Unlike <see cref="ApplyEditorStyle"/> / <see cref="ApplyDisplayStyle"/>, there's no
        /// per-instance override DP — sub-elements aren't user-customizable today.
        /// </summary>
        protected static void ApplyKeyedStyle(FrameworkElementFactory factory, ComponentResourceKey key)
        {
            var style = ResolveLibraryStyle(key);
            if (style != null)
                factory.SetValue(FrameworkElement.StyleProperty, style);
        }

        /// <summary>
        /// Returns the user-supplied <see cref="DisplayTemplate"/> if set; otherwise builds the
        /// editor's default via <see cref="CreateDisplayTemplate"/>. This is the entry point
        /// <see cref="GridColumn"/> calls — subclasses still implement <see cref="CreateDisplayTemplate"/>
        /// for the default; users override at this layer.
        /// </summary>
        public DataTemplate ResolveDisplayTemplate(GridColumn column)
            => DisplayTemplate ?? CreateDisplayTemplate(column);

        /// <summary>
        /// Returns the user-supplied <see cref="EditTemplate"/> if set; otherwise builds the
        /// editor's default via <see cref="CreateEditTemplate"/>.
        /// </summary>
        public DataTemplate ResolveEditTemplate(GridColumn column)
            => EditTemplate ?? CreateEditTemplate(column);

        /// <summary>
        /// Builds the read-only display template. Receives the owning <see cref="GridColumn"/> so
        /// the implementation can reach the binding path, display formatting, and converters.
        /// </summary>
        public abstract DataTemplate CreateDisplayTemplate(GridColumn column);

        /// <summary>
        /// Builds the in-place edit template. Receives the owning <see cref="GridColumn"/> so the
        /// implementation can wire a two-way binding to the field.
        /// </summary>
        public abstract DataTemplate CreateEditTemplate(GridColumn column);

        /// <summary>
        /// Helper for subclasses: ensures the editor element grabs keyboard focus the moment it
        /// materializes in the cell, so the user can interact with it (type, pick a date, toggle)
        /// on the same click that triggered edit mode. Without this, focus stays on the
        /// <see cref="DataGridCell"/> and the user has to click again to
        /// land on the editor — the standard <see cref="DataGridTextColumn"/>
        /// avoids this because it special-cases focus-on-edit internally; our
        /// <see cref="DataGridTemplateColumn"/>-based path doesn't.
        /// </summary>
        /// <remarks>
        /// The <see cref="Dispatcher"/> hop defers the focus call until WPF's input/focus pipeline
        /// has finished routing the click that triggered edit mode. Calling
        /// <see cref="UIElement.Focus"/> synchronously inside <see cref="FrameworkElement.Loaded"/>
        /// can race that pipeline and leave focus stranded on the cell.
        /// </remarks>
        protected static void AutoFocusOnLoad(FrameworkElementFactory factory)
        {
            factory.AddHandler(FrameworkElement.LoadedEvent,
                new RoutedEventHandler((s, _) =>
                {
                    if (s is UIElement el)
                    {
                        el.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            Keyboard.Focus(el);
                        }), DispatcherPriority.Input);
                    }
                }));
        }

        /// <summary>
        /// Helper for TextBox-based edit templates: arrow keys exit the cell to navigate
        /// adjacent cells when the caret is at a text boundary or all text is selected.
        /// Otherwise the TextBox processes the arrow normally (moves caret).
        /// <list type="bullet">
        ///   <item>Left + caret at start (or all selected) → commit + navigate left</item>
        ///   <item>Right + caret at end (or all selected) → commit + navigate right</item>
        ///   <item>Up / Down → always commit + navigate (single-line TextBox)</item>
        ///   <item>Ctrl+Arrow → defer to TextBox (jump word / start-end of text)</item>
        ///   <item>Shift+Arrow → defer to TextBox (extend selection)</item>
        /// </list>
        /// </summary>
        protected static void AddTextBoxCaretAwareArrowExit(FrameworkElementFactory factory)
        {
            factory.AddHandler(UIElement.PreviewKeyDownEvent,
                new KeyEventHandler((s, e) =>
                {
                    // The TextBox might be the sender (plain TextBox factory: Text / Numeric /
                    // Masked) or a focused descendant (DatePicker's inner PART_TextBox).
                    // OriginalSource resolves to the actual focused element either way.
                    var tb = (e.OriginalSource as TextBox) ?? (s as TextBox);
                    if (tb == null) return;
                    if (e.Key != Key.Left && e.Key != Key.Right && e.Key != Key.Up && e.Key != Key.Down) return;

                    // Modified arrows: defer to TextBox default (caret jump / extend selection).
                    var modifiers = Keyboard.Modifiers;
                    if ((modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) != 0) return;

                    int caret = tb.CaretIndex;
                    int len = tb.Text?.Length ?? 0;
                    int selLen = tb.SelectionLength;
                    bool selectAll = selLen > 0 && selLen == len;

                    bool shouldExit = e.Key switch
                    {
                        Key.Left  => selectAll || (caret == 0 && selLen == 0),
                        Key.Right => selectAll || (caret == len && selLen == 0),
                        Key.Up    => true,
                        Key.Down  => true,
                        _         => false,
                    };

                    if (!shouldExit) return;

                    e.Handled = true;
                    ExitCellViaArrow(tb, e);
                }));
        }

        /// <summary>
        /// Commit the current edit, return focus to the cell, then re-raise the arrow
        /// keypress as a fresh <see cref="Keyboard.KeyDownEvent"/> on the cell so the parent
        /// <see cref="DataGrid"/>'s built-in arrow-key cell-navigation runs naturally —
        /// without our edit-template having to know anything about cell coordinates.
        /// Subclasses with custom arrow handling (ComboBox dropdown-state-aware exit,
        /// CheckBox arrow exit, SegmentedDateTimeEditor's region-nav vs. cell-nav split) call this
        /// directly after marking their own PreviewKeyDown as Handled.
        /// </summary>
        /// <remarks>
        /// Sets <see cref="SearchDataGrid"/>'s carry-edit flag <em>after</em> the source cell
        /// has been refocused. The carry flag tells <c>OnAnyDescendantGotFocus</c> to BeginEdit
        /// on the destination cell when it receives focus from the synthesized arrow; if the
        /// flag were set before <see cref="DataGrid.CommitEdit()"/> + <see cref="UIElement.Focus"/>,
        /// the source cell's own GotFocus would consume it first (re-entering edit mode on the
        /// source) and the destination cell would arrive with the flag false. Setting it after
        /// the source-cell focus pass routes the flag's single consumption to the destination.
        /// </remarks>
        internal static void ExitCellViaArrow(DependencyObject source, KeyEventArgs e)
        {
            var cell = VisualTreeHelperMethods.FindVisualParent<DataGridCell>(source);
            var grid = VisualTreeHelperMethods.FindVisualParent<DataGrid>(cell);
            if (grid == null || cell == null) return;

            grid.CommitEdit();
            cell.Focus();

            var searchGrid = grid as SearchDataGrid;
            if (searchGrid != null)
                searchGrid.SetCarryEditStateOnNextFocus();

            // Row-edge wrap for Left / Right: Right on the rightmost cell loops to the
            // FIRST cell of the NEXT row (and Left on the leftmost loops to the LAST cell
            // of the PREVIOUS row). Done here instead of letting the synthesized arrow
            // KeyDown bubble to DataGrid's native handler — the native handler stops at
            // the row edge rather than wrapping. Falls through (helper returns false) at
            // the grid's outer edge so there's no destination to jump to. Up / Down still
            // fall through so DataGrid's native row navigation moves focus to the adjacent
            // row in the same column.
            if (searchGrid != null && (e.Key == Key.Left || e.Key == Key.Right))
            {
                if (searchGrid.TryWrapArrowAtRowEdge(cell, forward: e.Key == Key.Right))
                    return;
            }

            var newArgs = new KeyEventArgs(e.KeyboardDevice, e.InputSource, e.Timestamp, e.Key)
            {
                RoutedEvent = Keyboard.KeyDownEvent,
            };
            cell.RaiseEvent(newArgs);
        }

        /// <summary>
        /// Builds a <see cref="MultiBinding"/> for the <see cref="UIElement.Visibility"/> of an
        /// editor decoration button (combo toggle, spinner up/down, date dropdown). The binding
        /// pulls together the editor's <see cref="EditorButtonShowMode"/>, the grid's default,
        /// and the surrounding cell/row state, then routes them through
        /// <see cref="Converters.EditorButtonVisibilityConverter"/>. Returns null when the column
        /// has no <see cref="GridColumn.Owner"/> (e.g. unit tests) — caller can leave the button
        /// unconditionally visible.
        /// </summary>
        /// 
        protected static MultiBinding BuildEditorButtonVisibilityBinding(BaseEditSettings settings, GridColumn column)
        {
            if (settings == null || column?.Owner == null) return null;

            var binding = new MultiBinding
            {
                Converter = new Converters.EditorButtonVisibilityConverter(),
                Mode = BindingMode.OneWay,
            };
            binding.Bindings.Add(new Binding { Source = settings, Path = new PropertyPath(EditorButtonShowModeProperty), Mode = BindingMode.OneWay });
            binding.Bindings.Add(new Binding { Source = column.Owner, Path = new PropertyPath(SearchDataGrid.EditorButtonShowModeProperty), Mode = BindingMode.OneWay });
            binding.Bindings.Add(new Binding
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor) { AncestorType = typeof(DataGridCell) },
                Path = new PropertyPath(DataGridCell.IsKeyboardFocusWithinProperty),
                Mode = BindingMode.OneWay,
            });
            binding.Bindings.Add(new Binding
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor) { AncestorType = typeof(DataGridCell) },
                Path = new PropertyPath(DataGridCell.IsEditingProperty),
                Mode = BindingMode.OneWay,
            });
            binding.Bindings.Add(new Binding
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor) { AncestorType = typeof(DataGridRow) },
                Path = new PropertyPath(DataGridRow.IsSelectedProperty),
                Mode = BindingMode.OneWay,
            });
            binding.Bindings.Add(new Binding
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor) { AncestorType = typeof(DataGridCell) },
                Path = new PropertyPath(DataGridCell.IsReadOnlyProperty),
                Mode = BindingMode.OneWay,
            });
            binding.Bindings.Add(new Binding
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor) { AncestorType = typeof(DataGrid) },
                Path = new PropertyPath(DataGrid.IsReadOnlyProperty),
                Mode = BindingMode.OneWay,
            });
            return binding;
        }

        /// <summary>
        /// Begins edit mode on the cell containing <paramref name="source"/>. Used by editor
        /// decoration buttons that are visible in display mode (per <see cref="EditorButtonShowMode"/>);
        /// clicking them should commit the user's intent to edit by entering edit mode immediately.
        /// </summary>
        protected internal static void EnsureCellEditing(DependencyObject source)
        {
            var cell = VisualTreeHelperMethods.FindVisualParent<DataGridCell>(source);
            if (cell == null || cell.IsEditing) return;
            var grid = VisualTreeHelperMethods.FindVisualParent<DataGrid>(cell);
            if (grid == null) return;
            cell.Focus();
            grid.BeginEdit();
        }

        /// <summary>
        /// Routes the column's <see cref="GridColumn.TextAlignment"/> to the right property on the
        /// generated editor element. Text controls (TextBlock, TextBox) take it directly via
        /// <c>TextAlignmentProperty</c>; ComboBox uses <c>HorizontalContentAlignment</c> so the
        /// selection text aligns inside the chrome; CheckBox uses its own
        /// <c>HorizontalAlignment</c> since the glyph itself is what shifts. The editor's outer
        /// stretching is unaffected — only the content within it moves.
        /// </summary>
        protected static void ApplyTextAlignment(FrameworkElementFactory factory, GridColumn column)
        {
            if (factory == null || column == null) return;
            var alignment = column.TextAlignment;
            var elementType = factory.Type;

            if (elementType == typeof(TextBlock))
            {
                // HorizontalAlignment=Stretch is required so the TextBlock occupies the full cell
                // width; otherwise the keyed display style's HorizontalAlignment would shrink it
                // to content size and TextAlignment within the block has nothing to align against.
                factory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
                factory.SetValue(TextBlock.TextAlignmentProperty, alignment);
            }
            else if (elementType == typeof(TextBox))
            {
                factory.SetValue(TextBox.TextAlignmentProperty, alignment);
            }
            else if (elementType == typeof(ComboBox))
            {
                factory.SetValue(Control.HorizontalContentAlignmentProperty, MapToHorizontalAlignment(alignment));
            }
            else if (elementType == typeof(CheckBox))
            {
                factory.SetValue(FrameworkElement.HorizontalAlignmentProperty, MapToHorizontalAlignment(alignment));
            }
            else if (elementType == typeof(SegmentedDateTimeEditor))
            {
                factory.SetValue(SegmentedDateTimeEditor.TextAlignmentProperty, alignment);
            }
        }

        /// <summary>
        /// Maps <see cref="TextAlignment"/> to <see cref="HorizontalAlignment"/> for non-text
        /// controls (CheckBox, ComboBox content). <see cref="TextAlignment.Justify"/> has no
        /// horizontal-alignment counterpart, so it falls through to <see cref="HorizontalAlignment.Stretch"/>.
        /// </summary>
        private static HorizontalAlignment MapToHorizontalAlignment(TextAlignment alignment) => alignment switch
        {
            TextAlignment.Center => HorizontalAlignment.Center,
            TextAlignment.Right => HorizontalAlignment.Right,
            TextAlignment.Justify => HorizontalAlignment.Stretch,
            _ => HorizontalAlignment.Left,
        };

        /// <summary>
        /// Helper for subclasses: build a two-way binding to the column's <see cref="GridColumn.FieldName"/>,
        /// updating the source when focus is lost (the standard editing UX).
        /// </summary>
        protected static Binding CreateValueBinding(GridColumn column, BindingMode mode = BindingMode.TwoWay)
        {
            return new Binding(column.FieldName)
            {
                Mode = mode,
                UpdateSourceTrigger = UpdateSourceTrigger.LostFocus,
                ValidatesOnDataErrors = true,
                ValidatesOnExceptions = true
            };
        }
    }
}
