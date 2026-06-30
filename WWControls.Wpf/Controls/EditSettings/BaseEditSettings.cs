using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WWControls.Core;

namespace WWControls.Wpf
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
        /// Inheritable attached flag marking that an editor is hosted somewhere it should render its
        /// own border — set on the <see cref="EditFormPresenter"/> so every editor it hosts shows a
        /// border, while the same editor templates stay flat (borderless) in a grid cell or the
        /// filter row, where this defaults to <c>false</c>. The migrated <c>WWxxxEdit</c> controls
        /// bind <see cref="WWBaseEdit.ShowBorder"/> to this flag (chrome is owned once by
        /// <see cref="WWBaseEdit"/>); the <c>EditTextBox</c> style keyed on it still drives the inner
        /// TextBox of <see cref="SegmentedDateTimeEditor"/>. Checkbox and read-only display editors
        /// carry no such trigger and stay flat regardless.
        /// </summary>
        public static readonly DependencyProperty ShowEditorBorderProperty =
            DependencyProperty.RegisterAttached(
                "ShowEditorBorder",
                typeof(bool),
                typeof(BaseEditSettings),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>Sets <see cref="ShowEditorBorderProperty"/> on <paramref name="element"/>.</summary>
        public static void SetShowEditorBorder(DependencyObject element, bool value)
            => element.SetValue(ShowEditorBorderProperty, value);

        /// <summary>Reads <see cref="ShowEditorBorderProperty"/> from <paramref name="element"/>.</summary>
        public static bool GetShowEditorBorder(DependencyObject element)
            => (bool)element.GetValue(ShowEditorBorderProperty);

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
        /// Controls when a click on a cell triggers edit mode. Defaults to
        /// <see cref="Wpf.EditorShowMode.Default"/>, which inherits the grid-wide
        /// <see cref="SearchDataGrid.EditorShowMode"/>; an explicit value here overrides the
        /// grid-level setting for this column's editor.
        /// </summary>
        public static readonly DependencyProperty EditorShowModeProperty =
            DependencyProperty.Register(nameof(EditorShowMode), typeof(EditorShowMode), typeof(BaseEditSettings),
                new PropertyMetadata(EditorShowMode.Default));

        /// <summary>
        /// Controls when the editor's decoration buttons (combo toggle, spinner, calendar
        /// dropdown) are visible. Defaults to <see cref="Wpf.EditorButtonShowMode.Default"/>
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
        /// Strips the default WPF validation error adorner — the red border WPF draws around an
        /// editor whose binding holds a <see cref="System.Windows.Controls.ValidationError"/> —
        /// from <paramref name="factory"/> by nulling its <see cref="Validation.ErrorTemplate"/>.
        /// The library surfaces data-annotation errors through the cell's
        /// <see cref="ValidationErrorIcon"/> badge instead, so the red border is redundant chrome.
        /// Only the adorner is suppressed: <see cref="Binding.NotifyOnValidationError"/> stays on
        /// and the error still registers on the binding, so the commit gate keeps working.
        /// Call on the element that carries the <see cref="CreateValueBinding"/> value binding.
        /// </summary>
        protected static void SuppressValidationErrorAdorner(FrameworkElementFactory factory)
        {
            factory?.SetValue(Validation.ErrorTemplateProperty, null);
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
        /// Unlike <see cref="ApplyDisplayStyle"/>, there's no per-instance override DP —
        /// sub-elements aren't user-customizable today.
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
        public DataTemplate ResolveDisplayTemplate(ColumnDataBase column)
            => DisplayTemplate ?? CreateDisplayTemplate(column);

        /// <summary>
        /// Returns the user-supplied <see cref="EditTemplate"/> if set; otherwise builds the
        /// editor's default via <see cref="CreateEditTemplate"/>.
        /// </summary>
        public DataTemplate ResolveEditTemplate(ColumnDataBase column)
            => EditTemplate ?? CreateEditTemplate(column);

        /// <summary>
        /// Builds the read-only display template. Receives the owning <see cref="GridColumn"/> so
        /// the implementation can reach the binding path, display formatting, and converters.
        /// </summary>
        public abstract DataTemplate CreateDisplayTemplate(ColumnDataBase column);

        /// <summary>
        /// Builds the in-place edit template. Receives the owning <see cref="GridColumn"/> so the
        /// implementation can wire a two-way binding to the field.
        /// </summary>
        public abstract DataTemplate CreateEditTemplate(ColumnDataBase column);

        /// <summary>
        /// Builds the editor element placed in the per-column cell of the
        /// <see cref="FilterRowPresenter"/>. Mirrors <see cref="CreateEditTemplate"/> in shape
        /// (same editor type, same keyed style, same theming) but binds the editor's value DP to
        /// the supplied <paramref name="host"/>'s filter DPs (<c>SearchText</c> for text editors,
        /// <c>SearchValue</c> for typed editors, <c>FilterCheckboxState</c> for checkboxes)
        /// instead of a row-item property path.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Override per subtype so the filter row reuses the cell editor's look without
        /// duplicating template XAML in the filter-row resource dictionary.
        /// </para>
        /// <para>
        /// Default implementation returns a styled <see cref="TextBox"/> bound to
        /// <see cref="IColumnFilterHost.SearchText"/>, which is the correct behavior for
        /// <c>TextEditSettings</c> and any consumer-defined subtype that hasn't customized the
        /// filter-row editor.
        /// </para>
        /// </remarks>
        public virtual UIElement CreateFilterEditor(IColumnFilterHost host)
        {
            return BuildDefaultTextEditor(host);
        }

        /// <summary>
        /// Builds the read-only display surface placed in the per-column cell of the
        /// <see cref="FilterRowPresenter"/> when the filter cell is NOT in keyboard focus.
        /// Mirrors <see cref="CreateFilterEditor"/> but renders a <see cref="TextBlock"/>-shaped
        /// presentation of the filter value rather than an editable control — same display /
        /// edit split that <see cref="CreateDisplayTemplate"/> / <see cref="CreateEditTemplate"/>
        /// use for the row's actual data cell.
        /// </summary>
        /// <remarks>
        /// Decoration buttons (combo toggle, calendar dropdown, spinner arrows) are intentionally
        /// omitted from the display surface — the user enters the cell to edit, which swaps the
        /// editor to the full <see cref="CreateFilterEditor"/> surface where the decorations live.
        /// Default implementation returns a <see cref="TextBlock"/> bound to
        /// <see cref="IColumnFilterHost.SearchText"/>; subclasses override to bind to
        /// <see cref="IColumnFilterHost.SearchValue"/> with type-appropriate formatting.
        /// </remarks>
        public virtual UIElement CreateFilterDisplay(IColumnFilterHost host)
        {
            return BuildDefaultTextDisplay(host);
        }

        /// <summary>
        /// Helper shared with the default <see cref="CreateFilterDisplay"/> and any subclass that
        /// wants to fall back to a plain <see cref="TextBlock"/> rendering of
        /// <see cref="IColumnFilterHost.SearchText"/>. Wears the library's
        /// <see cref="EditSettingsThemeKeys.DisplayTextBlock"/> style so colors / margins match
        /// the cell editor's display mode.
        /// </summary>
        protected static TextBlock BuildDefaultTextDisplay(IColumnFilterHost host)
        {
            var tb = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(4, 0, 4, 0),
            };
            var style = Application.Current?.TryFindResource(EditSettingsThemeKeys.DisplayTextBlock) as Style;
            if (style != null) tb.Style = style;

            BindingOperations.SetBinding(tb, TextBlock.TextProperty, new Binding(nameof(IColumnFilterHost.SearchText))
            {
                Source = host,
                Mode = BindingMode.OneWay,
            });
            return tb;
        }

        /// <summary>
        /// Returns the set of <see cref="SearchType"/>s this editor exposes in the
        /// <see cref="SearchTypeSelector"/> dropdown for filter-row use. Each subtype scopes
        /// the list to the operators that make sense given the editor's value shape —
        /// e.g. a <see cref="ComboBoxEditSettings"/> only exposes <c>Equals</c> / <c>NotEquals</c>
        /// (the user picks a discrete value, so <c>Contains</c> / <c>StartsWith</c> don't apply),
        /// while a <see cref="DateEditSettings"/> exposes equality + range comparison and skips
        /// the text-shape operators.
        /// </summary>
        /// <param name="columnDataType">Data type detected from the column's sampled values.</param>
        /// <param name="isNullable">Whether the column's CLR type / observed values include null. When true the returned set may include <see cref="SearchType.IsNull"/> / <see cref="SearchType.IsNotNull"/>.</param>
        /// <remarks>
        /// Default returns every type valid for the data type per
        /// <see cref="SearchTypeRegistry.GetFiltersForDataType(ColumnDataType, bool)"/> — preserves
        /// the pre-EditSettings-aware behavior for callers that don't override.
        /// </remarks>
        public virtual IEnumerable<SearchType> GetSupportedFilterSearchTypes(ColumnDataType columnDataType, bool isNullable)
        {
            return SearchTypeRegistry.GetFiltersForDataType(columnDataType, isNullable)
                .Select(m => m.SearchType);
        }

        /// <summary>
        /// Editor-specific preference for the column's <see cref="GridColumn.DefaultSearchType"/>.
        /// Returns non-null when the editor shape constrains the allowed operator set so tightly
        /// that the CLR-type-based default (set by <see cref="GridColumn.ApplyTypeBasedDefaults"/>)
        /// can fall outside <see cref="GetSupportedFilterSearchTypes"/> — e.g. a
        /// <see cref="ComboBoxEditSettings"/> on a string-typed field would otherwise inherit
        /// <see cref="Wpf.DefaultSearchType.StartsWith"/> and disable the FilterRow cell.
        /// Default returns <c>null</c> (no preference; type-based default wins). Honored by
        /// <see cref="GridColumn"/> only when the user hasn't set
        /// <see cref="GridColumn.DefaultSearchType"/> explicitly.
        /// </summary>
        public virtual DefaultSearchType? GetPreferredDefaultSearchType() => null;

        /// <summary>
        /// Helper for subclass overrides that want to declare a fixed base set and let the
        /// nullability suffix flow in automatically. Appends <see cref="SearchType.IsNull"/> /
        /// <see cref="SearchType.IsNotNull"/> when <paramref name="isNullable"/> is <c>true</c>.
        /// </summary>
        protected static IEnumerable<SearchType> WithNullability(IEnumerable<SearchType> baseSet, bool isNullable)
        {
            foreach (var t in baseSet) yield return t;
            if (isNullable)
            {
                yield return SearchType.IsNull;
                yield return SearchType.IsNotNull;
            }
        }

        /// <summary>
        /// Helper shared with the default <see cref="CreateFilterEditor"/> and any subclass that
        /// wants to fall back to the text-editor shape. Produces a <see cref="TextBox"/> wearing
        /// the library's <see cref="EditSettingsThemeKeys.EditTextBox"/> style, with its
        /// <see cref="TextBox.Text"/> DP two-way bound to <see cref="IColumnFilterHost.SearchText"/>
        /// updating on every keystroke so the filter pipeline's debounce hooks fire correctly.
        /// </summary>
        protected static TextBox BuildDefaultTextEditor(IColumnFilterHost host)
        {
            var tb = new TextBox
            {
                VerticalContentAlignment = VerticalAlignment.Center,
            };
            var style = Application.Current?.TryFindResource(EditSettingsThemeKeys.EditTextBox) as Style;
            if (style != null) tb.Style = style;

            BindingOperations.SetBinding(tb, TextBox.TextProperty, new Binding(nameof(IColumnFilterHost.SearchText))
            {
                Source = host,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            });
            return tb;
        }

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
        /// Decides whether an unmodified arrow keypress should exit the cell rather than move the
        /// caret: Left at the start (or all selected), Right at the end (or all selected), Up / Down
        /// always (single-line). Used by the grid-side editor host
        /// (<see cref="EditorHostBehavior"/>) so the hosted TextBox-based editors apply identical
        /// boundary rules.
        /// </summary>
        internal static bool ShouldExitCellOnArrow(TextBox tb, Key key)
        {
            int caret = tb.CaretIndex;
            int len = tb.Text?.Length ?? 0;
            int selLen = tb.SelectionLength;
            bool selectAll = selLen > 0 && selLen == len;

            return key switch
            {
                Key.Left  => selectAll || (caret == 0 && selLen == 0),
                Key.Right => selectAll || (caret == len && selLen == 0),
                Key.Up    => true,
                Key.Down  => true,
                _         => false,
            };
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
            var cell = VisualTreeHelperMethods.FindVisualAncestor<DataGridCell>(source);
            if (cell == null)
            {
                // Filter-row hosting: the editor isn't inside a DataGridCell, it's inside a
                // ColumnFilterControl in the FilterRow. Same intent (caret-at-boundary
                // wants to step to the adjacent cell), different host plumbing — route the
                // request through FilterRowNavigator which knows how to walk the filter row
                // in DisplayIndex order and hand off Down / end-of-row Tab to the data area.
                // Editors that own this path (SegmentedDateTimeEditor, ComboBoxEditSettings,
                // CheckBoxEditSettings) mark the event Handled after the call regardless of
                // whether navigation actually moved, which matches data-cell semantics: at
                // the filter row's outer edge the key is consumed and focus stays put.
                var filter = VisualTreeHelperMethods.FindVisualAncestor<ColumnFilterControl>(source);
                if (filter != null)
                    FilterRowNavigator.TryNavigate(filter, e);
                return;
            }

            var grid = VisualTreeHelperMethods.FindVisualAncestor<DataGrid>(cell);
            if (grid == null) return;

            // Validation edit lock: while the editing cell holds an unresolved error and
            // commit-on-error is off, an arrow at a boundary must not carry focus to another cell.
            // The caller already marked the key handled, so returning simply consumes the arrow
            // and keeps the user in the editor.
            if (grid is SearchDataGrid lockedGrid && lockedGrid.IsEditLockActive())
                return;

            grid.CommitEdit();
            cell.Focus();

            var searchGrid = grid as SearchDataGrid;
            if (searchGrid != null)
                searchGrid.SetCarryEditStateOnNextFocus();

            // Row-edge wrap for Left / Right: Right on the rightmost cell loops to the FIRST cell
            // of the SAME row (and Left on the leftmost loops to the LAST cell of the same row).
            // Done here instead of letting the synthesized arrow KeyDown bubble to DataGrid's native
            // handler — the native handler stops at the row edge rather than wrapping. Falls through
            // (helper returns false) when not at a row edge. Up / Down still fall through so
            // DataGrid's native row navigation moves focus to the adjacent row; Tab (not arrows)
            // crosses to the next/previous row.
            if (searchGrid != null && (e.Key == Key.Left || e.Key == Key.Right))
            {
                if (searchGrid.TryWrapArrowWithinRow(cell, forward: e.Key == Key.Right))
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
        /// has no <see cref="ColumnLayoutBase.View"/> (e.g. unit tests) — caller can leave the button
        /// unconditionally visible.
        /// </summary>
        ///
        protected static MultiBinding BuildEditorButtonVisibilityBinding(BaseEditSettings settings, ColumnDataBase column)
        {
            if (settings == null || column?.View == null) return null;

            var binding = new MultiBinding
            {
                Converter = new Converters.EditorButtonVisibilityConverter(),
                Mode = BindingMode.OneWay,
            };
            binding.Bindings.Add(new Binding { Source = settings, Path = new PropertyPath(EditorButtonShowModeProperty), Mode = BindingMode.OneWay });
            binding.Bindings.Add(new Binding { Source = column.View, Path = new PropertyPath(SearchDataGrid.EditorButtonShowModeProperty), Mode = BindingMode.OneWay });
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
            var cell = VisualTreeHelperMethods.FindVisualAncestor<DataGridCell>(source);
            if (cell == null || cell.IsEditing) return;
            var grid = VisualTreeHelperMethods.FindVisualAncestor<DataGrid>(cell);
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
        protected static void ApplyTextAlignment(FrameworkElementFactory factory, ColumnDataBase column)
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
        /// Helper for subclasses: build a two-way binding to the column's effective value path,
        /// updating the source when focus is lost (the standard editing UX). The path/source come
        /// from <see cref="ColumnDataBase.CreateFieldBinding"/> — the column's explicit
        /// <see cref="ColumnDataBase.Binding"/> override when set, otherwise its
        /// <see cref="GridColumn.FieldName"/>. Validation stays keyed on <c>FieldName</c> (the
        /// identity), independent of any binding-path override.
        /// </summary>
        protected static Binding CreateValueBinding(ColumnDataBase column, BindingMode mode = BindingMode.TwoWay)
        {
            var binding = column.CreateFieldBinding();
            binding.Mode = mode;
            binding.UpdateSourceTrigger = UpdateSourceTrigger.LostFocus;
            binding.ValidatesOnDataErrors = true;
            binding.ValidatesOnExceptions = true;

            // validate the edited value against the property's data-annotation
            // attributes. The rule no-ops when the column resolves
            // ActualShowValidationAttributeErrors to false, so the grid/column toggle controls
            // whether errors surface. NotifyOnValidationError lets the grid raise Validation.Error
            // for the message tooltip and for commit gating.
            if (!string.IsNullOrEmpty(column.FieldName) && column.FieldName.IndexOf('.') < 0)
            {
                binding.NotifyOnValidationError = true;
                binding.ValidationRules.Add(new DataAnnotationsValidationRule(column, column.FieldName));
            }

            return binding;
        }
    }
}
