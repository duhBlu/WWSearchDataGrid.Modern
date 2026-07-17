# Changelog

## [Unreleased]

### Changed — WWTextBox: dropped date/time input masking
- **`WWTextBox` no longer accepts the date/time mask types** (`MaskType.DateTime` / `DateOnly` /
  `TimeOnly`) — applying one now throws `NotSupportedException` pointing to `WWDatePicker`, whose
  segmented editor handles date and time entry far better than a masked text box. `Simple`,
  `Numeric`, and `TimeSpan` masks are unchanged.
- The mask engines themselves (including `DateTimeMaskFormatter`) stay in `WWControls.Core` — they
  still back `WWDatePicker` / `SegmentedDateTimeEditor`, `MaskDisplayProvider`, and grid display
  formatting. Only WWTextBox's masked *input* path drops the date/time types.
- Masking sample trimmed to Simple / Numeric / TimeSpan (the date & time rows are gone).

### Changed — internal editors now use WWTextBox instead of a plain TextBox
- Plain themed text boxes (`PrimitiveThemeKeys.TextBox`) inside the library's own templates were
  replaced with `WWTextBox`, so they pick up its chrome, watermark, clear button, and themed
  right-click menu: the **WWPropertyGrid filter box** (its separate watermark overlay is gone —
  `WWTextBox.Watermark` + `ShowClearButton` replace it), the **GroupSummaryEditor** prefix/suffix
  inputs, and the **checked-list filter popup** search box (its `Delay=200` moved to
  `WWTextBox.UpdateDelay`).
- Left as plain `TextBox` by design: the required template parts that their controls' code-behind
  drives as a `TextBox` — ComboBox's `PART_EditableTextBox`, WWNumericUpDown / WWSearchTextBox /
  SegmentedDateTimeEditor `PART_TextBox`, and WWColorPicker's `PART_HexTextBox`.

### Added — WWTextBox: themed right-click menu
- **WWTextBox now shows a themed context menu** in place of the OS default — Undo / Redo, Cut /
  Copy / Paste, Select All — each with an icon and keyboard gesture, using the shared
  `PrimitiveThemeKeys.ContextMenu` flat-menu style. Items are the stock `ApplicationCommands` with
  `CommandTarget` bound to the menu's `PlacementTarget`, so the inner `TextBox` drives their
  enabled state (Paste greys out on an empty clipboard, Undo/Redo track history, Cut/Copy track the
  selection). Defined in the control template, so each instance gets its own menu.
- New monochrome `DrawingImage` icons under `IconKeys`: `IconCut`, `IconPaste`, `IconUndo`,
  `IconRedo`, `IconSelectAll` (Copy reuses the existing `IconCopy`).
- **Spell-check suggestions are preserved.** A custom `ContextMenu` normally replaces WPF's
  built-in editing menu, where suggestions live. When `IsSpellCheckEnabled` is on and the click
  lands on a misspelled word, `WWTextBox` injects that word's suggestions (bold) + "Ignore All" +
  a separator at the top of the menu (via `ContextMenuOpening` and the `SpellingError` API);
  applying a suggestion calls `SpellingError.Correct`, "Ignore All" calls `IgnoreAll`. Rebuilt on
  every open; a click that isn't on an error shows just the editing commands.

### Added — WWTextBox: full standard TextBox surface (multi-line, wrapping, scrollbars, …)
- **`WWTextBox` now surfaces the standard `TextBox` / `TextBoxBase` properties** so it can stand in
  for a plain multi-line `TextBox`, not just a single-line one: `AcceptsReturn`, `AcceptsTab`,
  `TextWrapping`, `MinLines`, `MaxLines`, `HorizontalScrollBarVisibility`,
  `VerticalScrollBarVisibility`, `TextDecorations`, `CharacterCasing`, `IsUndoEnabled`, `UndoLimit`,
  `IsSpellCheckEnabled` (feeds `SpellCheck.IsEnabled`), `CaretBrush`, `SelectionBrush`, and
  `SelectionOpacity`. Each is a pass-through, TemplateBound onto the inner `PART_TextBox`; every
  default matches `TextBox`, so a `WWTextBox` with none of them set is unchanged.
- **`VerticalContentAlignment` now flows through** to the text, watermark, glyph, and clear button
  (previously the inner input was hard-coded to center). Its style default stays `Center` for the
  single-line look; set it to `Top` for a multi-line box so content starts at the top edge. The
  watermark also follows `TextWrapping`.
- TextBox sample reorganized into purpose-built example cards — single-line input, multi-line memo
  (wrapping / scrollbars / line bounds / spell check), and an adorned glyph input — each editor
  grouped with only the options that shape it.

### Added — WWPropertyGrid: header layout (`HeaderShowMode`)
- **`HeaderShowMode` controls where a row places its header relative to its editor.** New
  `PropertyHeaderShowMode` enum: `Left` (default — the existing two-column `Name | Editor` row),
  `Top` (header stacked above the editor, full width), `Hidden` (editor only, no header), and
  `OnlyHeader` (header only, no editor — a caption row).
- **Set it grid-wide or per property.** `WWPropertyGrid.HeaderShowMode` (default `Left`) is the
  grid-wide default; `WWPropertyDefinition.HeaderShowMode` (nullable) overrides it per property and
  wins. Both are live — the grid-level value re-lays out inheriting rows without reassigning
  `SelectedObject`, and a per-property value flows through the same bindable-metadata (mechanism A)
  path as `IsReadOnly` / `IsVisible`, recomputing `WWPropertyItem.ActualHeaderShowMode` on change.
- The shared name-column splitter hides when the grid-wide default is not `Left` (it only acts on
  the left-header layout). New "Layout" sample under the Property Grid category.

### Changed — WWPropertyGrid: built-in typed editors, per-property settings & templates, live metadata, validation
- **Built-in typed editors.** A property with no per-property definition now gets an editor
  auto-picked from its CLR type (`string` → text, `bool` → checkbox, enum → combo auto-populated
  from the enum, numeric → a plain numeric text box, `DateTime` → date); only a type with no
  natural editor falls back to the read-only placeholder. The up/down spinner is never the
  automatic numeric default — it is opt-in via an explicit `NumericUpDownSettings` or
  `[PropertyGridEditor(EditorKind.Spin)]`, matching the SearchDataGrid's numeric column default.
  Editors bind straight to the model property and reuse the same `BaseEditorSettings` stack the
  SearchDataGrid builds its cells from.
- **Per-property definitions apply reliably from XAML.** The first property build is deferred to
  `OnInitialized` instead of running on the initial `SelectedObject` change. A XAML-bound
  `SelectedObject` resolves during start-tag processing — before the `PropertyDefinitions` child
  content is parsed — so the old timing built every row against an empty definition list and each
  property silently fell back to its CLR-type default editor (masks, bounded spinners, bound
  combos, and `DatePickerSettings` such as `PopupMode` all ignored). Building at `OnInitialized`
  (definitions now present) makes each row resolve against its matching definition; a
  `SelectedObject` set after initialization still rebuilds immediately.
- **Checkbox / combo editors work inline (hostless).** The editor templates ran grid-cell-only
  logic unconditionally — `AutoFocusOnLoad`, and arrow keys marked handled + routed to
  `ExitCellViaArrow` — which no-ops without a `DataGridCell` but still swallowed the keys. Those
  behaviors are now gated on the column having a `Host` (a grid). In a property-grid row:
  a combo's Up/Down arrows change the selection again (no longer intercepted); the checkbox is a
  tab stop (`IsTabStop` set on the hostless editor) and toggles on Space (`WWCheckBox` gained a
  `OnKeyDown` Space toggle, used when the control itself holds focus); and the checkbox commits on
  every toggle — its value binding now uses `UpdateSourceTrigger=PropertyChanged` instead of
  `LostFocus`, so a click that toggles the box but never moves focus still writes through.
  `WWEditorBase` also no longer forwards focus to a non-focusable inner element (it would bounce
  focus off the editor), keeping focus on the checkbox so its Space handler runs.
- **New `WWPropertyDefinition`** (supersedes `WWEditorDefinition`) + the **`PropertyDefinitions`**
  collection on `WWPropertyGrid`. A definition carries `EditSettings` (any `BaseEditorSettings`:
  mask / `ItemsSource` / bounds / …), a custom `EditTemplate` / `DisplayTemplate`, and bindable
  metadata overrides. Editor resolution precedence: custom template → `EditSettings` →
  `[PropertyGridEditor]` / `[DefaultEditor]` attribute → CLR type → placeholder. `EditorDefinitions`
  is kept working (superseded).
- **DataAnnotations metadata.** `WWPropertyItem` now reads `[Display(Name/GroupName/Order/Description)]`
  (preferred) and `[Editable(false)]` alongside the classic `System.ComponentModel` attributes.
- **Dynamic (live) metadata.** `WWPropertyItem` metadata is change-notifying and resolved by
  precedence (definition binding → runtime provider → attribute → default). Two live mechanisms:
  (A) `WWPropertyDefinition`'s bindable `IsReadOnly` / `IsVisible` / `DisplayName` / `Category` /
  `Description` / `PropertyOrder` / `ShowValidationErrors` overrides bound against the view model
  (the grid propagates its `DataContext` to each definition and its `EditSettings`); and (B) a new
  **`IObservablePropertyMetadataProvider`** (an `IPropertyMetadataProvider` that raises
  `PropertyMetadataChanged`). Rows show/hide live via the collection view's live filtering on
  `IsVisible` — no `SelectedObject` reassignment.
- **Validation.** New grid-level `ShowValidationErrors` (default `true`) + `AllowCommitOnValidationError`
  (default `false`) DPs (resolvable per-definition). Each row wraps its editor in the
  `ValidationCellPresenter` + `ValidationErrorIcon` badge — data-annotation attributes,
  `INotifyDataErrorInfo`, and `IValidationSeverityProvider` severity all surface on one badge,
  mirroring the SearchDataGrid.
- **Shared plumbing.** `ValidationErrorIcon` / `ValidationCellPresenter` moved up from the Grid
  assembly into Editors (`WWControls.Wpf.Editors` namespace; theme key
  `EditorThemeKeys.ValidationCellPresenter`) so both hosts share them; the presenter gained a
  `ValidatedItem` DP so the validated object can differ from the visual DataContext. New
  **`EditorSettingsFactory`** (Editors) centralizes the `EditorKind` / CLR-type → `BaseEditorSettings`
  mapping (repointed from `SmartColumnConfigurator`). `IEditorColumn` gained
  `AllowCommitOnValidationError`; `WWPropertyItem` implements `IEditorColumn`.
- **Flat editors inside the row.** The property row draws its own editor boundary (`EditorBorder`),
  so the editor it hosts now renders borderless (`WWEditorBase.FlattenEditors="True"` on the row's
  editor cell — the same inherited flag the grid's row-edit strip uses). This removes the
  double border that a bordered editor (text / combo / numeric / date) drew inside the row cell,
  matching how editors flatten inside a `DataGridCell`.
- **Editors sample app**: the single WWPropertyGrid sample is split into five under a new
  **Property Grid** category — Basics, Editor Settings, Custom Templates, Dynamic Metadata, and
  Validation.

### Added — WWDatePicker weekend disabling + US federal holiday highlighting
- **`DisableWeekends`** (`bool`, default `false`) on `SegmentedDateTimeEditor` / `WWDatePicker` /
  `DatePickerSettings` blocks Saturday/Sunday selection across every input path: the calendar
  renders weekend cells disabled, keyboard navigation onto a weekend is rejected in code
  (`IsDateAllowed` gate), a typed/spun weekend result reverts like an invalid composite, the
  scroll picker snaps a weekend to the nearest weekday (forward to Monday, back to Friday at the
  range's upper edge), and Today/Now skip the commit when the current day is a weekend. It never
  rewrites a bound `Value` that already holds a weekend — it only stops the user picking one.
- **`HighlightHolidays`** (`bool`, default `false`) accents the eleven US federal holidays in the
  calendar popup with a marker dot and a tooltip naming the holiday. Highlight only — holiday
  dates stay selectable; calendar-mode only.
- **New `UsFederalHolidays`** static helper (Editors assembly) — `IsHoliday(date)` /
  `GetHolidayName(date)`, computed per year with the federal observance rule (a fixed-date
  holiday on a weekend also resolves on its observed weekday; Juneteenth from 2021 on), cached
  per year.
- **New `CalendarDecorations`** attached properties (`DisableWeekends` / `HighlightHolidays`,
  both `Inherits`) — the editor sets them on the popup `Calendar` and the keyed `CalendarDayButton`
  style reads them via self-relative bindings, so an app can drive the same day-cell decoration
  on its own calendar. New `UsHolidayConverter` maps a day cell's date to holiday bool/name.
- **`DateTimeScrollPicker`** gained a `DisableWeekends` DP (weekend → weekday snap on commit).
- **Editors sample app**: DatePicker playground gained `DisableWeekends` / `HighlightHolidays`
  toggles.

### Changed — WWReorderableListBox replaced by WWListBox (selection glyphs + built-in reorder)
- **New `WWListBox`** (Editors assembly) — a general-purpose `ListBox` that absorbs the
  reorderable listbox: the traveling-hole drag engine is unchanged but now an opt-in feature.
  - **`SelectionMode`** is the platform property — `Single`, `Multiple` (plain click toggles),
    `Extended` (Ctrl/Shift multi-select) — no wrapper enum.
  - **`ItemKind`** (`ListBoxItemKind`: `Default` / `Checked` / `Radio`) picks the selection
    glyph rows render. Rows are generated as **`WWListBoxItem`** containers carrying a read-only
    `Kind` mirrored from the parent (the `WWComboBoxItem` pattern); the checkbox / radio glyphs
    are lit by the row's own `IsSelected`, and the row-highlight selection visual stays reserved
    for `Default`. Purely visual — `ItemKind` and `SelectionMode` compose freely.
  - **Reorder surface renames**: `EnableDragDrop` → **`AllowReorder`** (default now **`false`**
    — reordering is opt-in on a general list control), `MarginAnimationDuration` →
    **`ReorderAnimationDuration`**. `AutoScrollEdge` / `AutoScrollMinSpeed` / `AutoScrollMaxSpeed`
    / `AdornerOpacity` / `IsDragging` and the `ItemDragStarting` / `ItemReordered` events (and
    their args classes) carry over unchanged. `WWReorderableListBoxBehavior` →
    **`WWListBoxBehavior`**. Turning `AllowReorder` off mid-drag cancels the drag cleanly.
  - **Multi-select-safe dragging**: the engine only force-selects the pressed row and re-asserts
    `SelectedItem` on drop in `Single` mode; in `Multiple` / `Extended` the native click
    semantics own selection, so a drag never inverts a click-toggle or collapses a
    multi-selection.
  - New theme keys `EditorThemeKeys.ListBox` / `EditorThemeKeys.ListBoxItem`
    (`Themes/Editors/WWListBox.xaml`, replacing `WWReorderableListBox.xaml`). Virtualization
    and pixel-scroll defaults are unchanged from the reorderable control (containers must be
    realized for drags; opt virtualization back in per instance for large non-reordering lists).
- **`WWReorderableListBox` removed.** Migrated consumers: `ColumnChooser`'s three section lists
  (template + code-behind, `AllowReorder` bound to the per-section drag-enabled flags) and the
  Grid sample app's Options Playground navigation-order list.
- **Editors sample app**: new **WWListBox** sample under Editors — SelectionMode / ItemKind /
  AllowReorder / ReorderAnimationDuration driven live against one demo list.

### Added — WWDatePicker full picker surface (null input, popup modes, time editing, footer actions)
- **Segment cycling on plain Up/Down**: arrows now spin the focused segment by default (the
  numeric-spin-editor feel) instead of requiring Ctrl. New `CycleModifier` (`ModifierKeys`,
  default `None`) on `SegmentedDateTimeEditor` / `WWDatePicker` / `DatePickerSettings` sets the
  modifier that must be held for Up/Down to cycle; when one is required, unmodified Up/Down
  raises `CellExitRequested` (grid row navigation). `DatePickerSettings` defaults to
  `Control` — grid cell and filter-row date editors keep plain Up/Down for row navigation,
  matching the rest of the DataGrid — while the standalone control defaults to `None`.
- **Null input**: new `AllowNullInput` (default `true`) on `SegmentedDateTimeEditor` /
  `WWDatePicker` / `DatePickerSettings`. `Ctrl+0`, `Ctrl+Delete`, or `Ctrl+Backspace` clears
  every segment and commits `null` in one stroke; committing an all-empty editor writes `null`.
  With `AllowNullInput=False` the chords are inert and an emptied editor reverts to the bound
  value on commit.
- **Blank empty state + type-into-empty seeding**: a fully empty editor renders blank — no
  mask-literal skeleton (`//`) — whenever unfocused, and also while focused when
  `AllowNullInput`. The first keystroke into the empty editor seeds every other region from
  `DefaultDate` (or now) and lands in the first region, so the user types only the parts they
  want to change instead of navigating region by region. (Filter-row editors keep their
  existing display-only pre-fill; untouched regions there stay empty so a half-typed filter
  never commits made-up parts.) A focused non-nullable empty editor still shows the bare
  literals as a type-here affordance.
- **`DefaultDate`**: popup starting point while the value is null — the calendar opens on that
  month and the scroll columns position there (falls back to today / now). Never committed by
  itself; also seeds the year segment's first Ctrl+↑/↓ spin.
- **`PopupMode`** (`Calendar` default / `ScrollList`): the dropdown now offers two surfaces.
  `ScrollList` is a new **`DateTimeScrollPicker`** — looping month / day / year columns (day
  count tracks the selected month; year clamps over `MinDate`–`MaxDate`, default 1900–2100),
  plus hour / minute / AM-PM columns when time editing is on. Columns are built from the new
  **`WWLoopingSelector`** primitive (wheel / drag / click / arrow keys, wraparound, center
  selection band). Scroll selections commit live.
- **Time editing** via `TimeInput` (`Auto` default / `Enabled` / `Disabled`): `Auto` enables the
  popup's time surface exactly when the mask carries time specifiers. In `Calendar` mode the
  popup gains an inline **text-only segmented time editor** (keyed
  `EditorThemeKeys.SegmentedTimeEditor`) whose mask is the time tail of the main mask (or the
  culture short-time pattern); a calendar pick merges the picked day with the current
  time-of-day and keeps the popup open while time editing is on. Read-only `IsTimeEditingEnabled`
  and `HasDateParts` expose the resolved state (a time-only mask collapses the calendar
  entirely).
- **Popup footer actions**: `ShowTodayButton` / `ShowNowButton` / `ShowClearButton` (all default
  `false`) add Today / Now / Clear buttons (keyed `EditorThemeKeys.DatePickerPopupButton`). Each
  renders only when its target is editable: Today needs date parts (hidden on a time-only mask),
  Now needs the time surface (sets the full current date + time at whole-second precision, or
  just the time-of-day on a time-only editor), Clear needs `AllowNullInput`. Today preserves the
  time-of-day when time editing is on.
- **`ShowWeekNumbers`** (default `false`): a week-of-year strip beside the calendar month view,
  aligned to the calendar's real row slots and numbered per the culture's `CalendarWeekRule`.
- **`MinDate` / `MaxDate` are now enforced on commit** (typed input clamps into range; the
  scroll picker and Today/Clear clamp too) instead of only bounding the calendar's display
  range.
- **Unfocused display formatting**: new `DisplayFormat` + `UseMaskAsDisplayFormat` on
  `SegmentedDateTimeEditor` / `WWDatePicker`. Unfocused with a value, `DisplayFormat` renders a
  friendlier string (e.g. `D`); focusing swaps back to the editable mask.
  `UseMaskAsDisplayFormat=True` pins display text to the mask composition (the previous — and
  still default — behavior).
- `DatePickerSettings` forwards the whole new surface (`AllowNullInput`, `DefaultDate`,
  `PopupMode`, `TimeInput`, `ShowClearButton`, `ShowTodayButton`, `ShowWeekNumbers`) into both
  the cell edit template and the filter-row editor.
- **Modernized calendar** (`Themes/Editors/Calendar.xaml`): the popup's `Calendar` no longer
  renders the stock Windows look. Full retemplate — flat white chrome, `IconKeys` chevron
  navigation, semibold month-year header button, rounded hover day cells, accent-filled
  selection, accent-ringed today, red weekend days (faded when adjacent-month), faded
  adjacent-month days, dimmed blackout days, and matching month/year zoom-out cells. All four styles are **keyed**
  (`EditorThemeKeys.Calendar` / `CalendarItem` / `CalendarDayButton` / `CalendarButton`), never
  implicit, so consumer apps' own Calendars are untouched; the `CalendarItem` template keeps the
  stock part contract (`PART_MonthView` 7×7, `PART_YearView` 4×3, `DayTitleTemplate`) so the
  control's own population/zoom logic and the week-number strip keep working.

### Changed — WWReorderableListBox drag engine (mouse capture + traveling hole)
- **Reordering now runs on plain mouse capture, not OLE drag-drop** (`DragDrop.DoDragDrop`).
  Move events keep flowing at full rate even when the pointer leaves the control, so the
  edge dead-zones are gone and the `SetCursorPos` cursor-constraint P/Invoke hack is removed.
- **Visuals use a "traveling hole"**: the dragged item keeps its layout slot but is hidden in
  place (that slot IS the gap), a bitmap ghost follows the pointer, and the other items slide
  between slots with animated `RenderTransform`s. Layout never runs during a drag — slot
  geometry is frozen at drag start, so targeting is deterministic and can't oscillate with the
  slide animations, and the list's total height never changes.
- **New drag-lifecycle guarantees**: Escape cancels; losing mouse capture mid-drag (alt-tab, a
  dialog) cancels; an `Items` change during a drag cancels (the frozen snapshot would be
  invalid). All containers must be realized at drag start or the drag simply won't begin —
  so the control defaults `VirtualizingPanel.IsVirtualizing` to `false` (a scrolled
  virtualizing list would otherwise silently refuse every drag and fall back to native
  swipe-select). Override in XAML only for lists too large to realize.
- **Added** read-only `IsDragging` dependency property (true while a drag is in progress).
  `AdornerOpacity` changes now apply live to an active drag.
- **Removed** the `DragHelpers` static class (`DragHelpers.IsDragged` attached property). The
  control hides the dragged row itself; the [ColumnChooser] item-style collapse trigger that
  consumed it is deleted (collapsing the row would mutate layout mid-drag).
- `MarginAnimationDuration` keeps its name for API compatibility but now times the
  RenderTransform slide animation.
- **Pixel scrolling is forced** (`ScrollViewer.CanContentScroll = false`, local value in the
  constructor). Item scrolling re-arranges containers inside the items panel on every scroll,
  invalidating the frozen slot snapshot mid-drag — the gap visibly detached from the pointer
  after an auto-scroll. Auto-scroll speeds are now pixels per 20 ms tick and the defaults are
  retuned (`AutoScrollMinSpeed` 0.5 → 2.0, `AutoScrollMaxSpeed` 2.0 → 10.0); the theme's
  item-unit speed setters are removed.
- **Selection is frozen during a drag.** The ListBox's native subtree capture (taken on
  mouse down, powers swipe-select) is released before the drag takes element capture —
  re-capturing the same element does not change the capture mode, so without the release
  the items under the ghost kept receiving `MouseEnter` and selected themselves as the
  pointer passed. Keyboard input is also swallowed mid-drag so arrow keys can't move the
  selection under the drag.

### Added — Editor control foundation (WWBaseEdit + first-class editors)
- **`WWBaseEdit`** (lookless base) is the thin shared base for the editor family — the edited
  `Value`, `IsReadOnly`, the `ShowBorder` flag, focus-forwarding to the concrete input, and an
  in-cell self-flatten. It supplies **no** shared chrome template: each concrete editor owns its
  own default style, control template, and border (keyed `EditorThemeKeys.TextEdit` / `SpinEdit` /
  `ComboEdit` / `DateEdit` / `CheckEdit`) hosting named parts (`PART_TextBox`, `PART_ComboBox`,
  `PART_Editor`, `PART_CheckBox`, plus the spinner's `PART_UpButton` / `PART_DownButton`).
- **Border policy: bordered by default** (standalone use, the edit form). A grid cell flattens the
  editor — the control clears `ShowBorder` when it detects a stock `DataGridCell` ancestor — and the
  filter row sets `ShowBorder=false` explicitly. This resolves the long-standing spin / date border
  inconsistency without the inherited host-context flag.
- **Five concrete editors, each fully lookless with named parts** — `WWTextEdit` (mask support via
  `MaskInputBehavior`), `WWSpinEdit` (numeric entry + up/down buttons + Ctrl+Up/Down increment),
  `WWComboEdit` (hosts a flat ComboBox), `WWDateEdit` (wraps `SegmentedDateTimeEditor`), `WWCheckEdit`
  (two/three-state). They carry **zero grid references** and are usable standalone on any form.
- **`EditSettings` adapters now host these controls.** `TextEditSettings` / `SpinEditSettings` /
  `ComboBoxEditSettings` / `DateEditSettings` / `CheckBoxEditSettings` build their edit templates
  around the `WWxxxEdit` controls instead of assembling raw `TextBox` / `ComboBox` composites via
  `FrameworkElementFactory`. The column API, filter row, and edit form are unchanged.
- **`EditorHostBehavior`** (grid-side attached behavior) carries the three editor↔grid couplings
  (arrow-key cell exit, mouse-click caret, focus-on-edit) so the controls stay grid-agnostic.
- **Sample**: **Editing → Standalone Editors** now demonstrates all five controls used directly on a
  form (flat vs bordered chrome, masked, read-only), proving the controls are grid-independent.

### Removed — editor styling that the controls now subsume
- **`BaseEditSettings.EditorStyle`** (the per-column edit-mode `Style` DP) is removed. Edit-mode chrome
  is now owned by the editor controls (`WWBaseEdit`), so a `TargetType="TextBox"` style no longer maps
  onto the editor. To restyle: use `DisplayStyle` for the read-only display cell (unchanged), or
  `EditTemplate` to fully replace the in-place editor.
- **`EditSettingsThemeKeys.EditNumericTextBox`** (and its theme style) removed — it backed the old
  spinner's TextBox, which `WWSpinEdit` now replaces.
- **`EditorChrome.ShowEditorBorder`** (the inherited attached border flag) and the single shared
  `WWBaseEdit` chrome template + its `EditorThemeKeys.BaseEdit` key are removed. Each editor draws
  its own border per its own `ShowBorder` and flattens itself in a grid cell, so there is no
  host-context flag to inherit and no double-border suppression on nested controls.

### Added — Edit Entire Row (full-row edit mode)
- **Full-row editing.** A row can now be edited as a unit: clicking into it opens every cell as an
  editor at once behind a dimming overlay, with a row-scoped **Update / Cancel** action bar docked
  beneath the row. The grid swallows clicks and wheel input outside the bright strip, so interacting
  with the rest of the grid neither moves the current cell nor exits editing — the row stays open
  until the user commits or cancels.
- **`SearchDataGrid.RowEditTrigger`** (`RowEditTrigger` enum DP, default `Never`): gates when a row
  promotes into full-row edit mode. `Never` keeps stock single-cell editing; `OnCellEditorOpen`
  promotes the instant any cell editor opens; `OnCellValueChange` stays in single-cell editing until
  the open editor's value first changes, then promotes (the triggering change is folded into the
  row's transaction so Cancel reverts it too).
- **Commit / cancel as a unit** via the grid's row transaction. Update pushes the focused editor and
  calls `CommitEdit(Row)`; Cancel calls `CancelEdit(Row)`. Models implementing `IEditableObject` get
  a clean revert of every field. New API: `BeginRowEdit(item)` / `CommitRowEdit()` / `CancelRowEdit()`,
  read-only `IsRowEditing` / `RowEditItem`, and `RowEditStarted` / `RowEditEnded` events.
- **`RowEditPresenter`** (new control, `: ColumnAlignedRowPresenter`): the bright, column-aligned
  editor strip. Reuses the same `FilterRowPanel` alignment engine as the filter and total-summary
  rows; each cell hosts its column's own edit template bound to the editing item (read-only columns
  show their display template). Themed via `ThemeKeys.GridSearchDataGridRowEditPresenter`; the grid
  template gains a `PART_RowEditOverlay` / `PART_RowEditDim` / `PART_RowEditHost` /
  `PART_RowEditPresenter` layer. `SearchDataGridRow.IsRowEditing` marks the active row.
- **Sample**: the previously-planned **Editing → Edit Entire Row** sample is now implemented
  (`EditRowItem` row model with `IEditableObject`, a live `RowEditTrigger` switcher, and a
  last-action panel).

### Changed — Best fit (column auto-width engine)
- **Best Fit is measurement-based now.** The context-menu Best Fit / Best Fit All Columns no
  longer flip the column through `SizeToHeader` / `SizeToCells` with full `UpdateLayout()`
  passes (realized-rows-only, scroll-position-dependent, slow across many columns). The new
  engine (`SearchDataGrid.BestFit.cs`) measures realized cells and the column header directly
  with an infinite constraint (re-invalidating after, so live layout is undisturbed), and by
  default extends past the viewport: every filtered leaf row's display text is formatted
  through the same pipeline the cell renders with (mask > converter > edit-settings mask >
  string format > combo lookup) and measured as `FormattedText` (distinct strings only),
  calibrated with cell chrome derived from the realized cells. Result is clamped to
  Min/MaxWidth and frozen as a pixel width via the descriptor.
- **Public API**: `SearchDataGrid.BestFitColumn(GridColumn)` / `BestFitAllColumns()`, with
  overloads taking `BestFitArea` (All / Header / Rows), `BestFitMode` (Default / VisibleRows /
  AllRows), and a max-row-count cap. Columns that can't be text-measured (user cell templates,
  checkbox columns) automatically degrade to realized-only measurement.
- **`ColumnLayoutBase.ActualDataWidth`** (read-only DP): width of the widest measured data
  content including cell chrome; populated by best-fit runs (not live-tracked).
- The static `ContextMenuCommands.BestFitColumn(DataGrid, DataGridColumn)` helper was removed;
  both commands route through the grid API.
- **Configurable surface.** Column-level (`ColumnLayoutBase`): `AllowBestFit` (`bool?`,
  null = inherit) + read-only `ActualAllowBestFit`, `BestFitMode` (`Default` = inherit),
  `BestFitArea`, `BestFitMaxRowCount` (negative = unlimited). Grid-level: `AllowBestFit`
  (default `true`; runtime toggles re-resolve every column) and `BestFitMode` (default
  `AllRows` — explicit best-fit actions are accurate-by-default and don't depend on scroll
  position). `BestFitColumn(GridColumn)` / `BestFitAllColumns()` read the column DPs; the
  explicit-options overloads override them. `BestFitAllColumns` skips columns whose
  `ActualAllowBestFit` is `false` (it backs the "Best Fit (all columns)" menu); the
  single-column API runs regardless — the flag gates UI gestures, not code.
- **Gripper double-click is best-fit now.** Double-clicking a column-resize gripper runs the
  measurement-based best-fit instead of WPF's stock `Width = Auto` (realized-cells-only). The
  left gripper targets the previous visible column, matching stock semantics. Columns with
  `ActualAllowBestFit = false` keep the stock auto-size (opt-out disables the smarter fit, not
  resizing). The context-menu items are **removed** (collapsed), not disabled, when gated off —
  "Best Fit Column" per column via `ActualAllowBestFit`, "Best Fit All Columns" via grid
  `AllowBestFit` — in both the column-header menu and the group-panel pill menu; the section
  separator collapses with them. `CanExecute` stays as a backstop.
- **Auto best-fit on data load.** `SearchDataGrid.BestFitModeOnSourceChange` (default
  `Default` = off): when set to `VisibleRows` / `AllRows`, every real `ItemsSource` change
  schedules a coalesced best-fit pass over all columns at dispatcher idle (after column
  generation, filters, the group projection, and row realization settle; not-yet-loaded grids
  defer to `Loaded`). The value acts as the pass's grid default — an explicit column-level
  `BestFitMode` still wins. Internal source swaps (the grouping projection) and
  `Items.Refresh()` don't re-fire it; opted-out columns are skipped.
- **Fill the viewport.** Grid-level `BestFitFillViewport` (default `false`): when `true`, a
  best-fit-_all_ pass (the `BestFitAllColumns()` API, the "Best Fit All Columns" menu, and an
  auto best-fit on source change) sizes each column to its content and then makes the
  participating columns fill the horizontal viewport instead of leaving empty space on the
  right. The layout is **regime-aware**, re-evaluated live as the viewport width changes:
  - **Content fits the viewport** → columns are star-sized weighted by content width so they
    stretch to fill it; the slack keeps them resizable (dragging one redistributes among the
    rest).
  - **Content exceeds the viewport** → columns are frozen at their content pixel width, so the
    grid overflows to a horizontal scrollbar with every column at full width and resizable as a
    normal pixel column — rather than shrinking columns below their content (what star sizing
    alone does) or jamming them rigidly against the viewport edge.

  The regime flips at the fit/overflow boundary as the grid resizes (driven off the scroll
  viewport's viewport-width change), without re-measuring. Opted-out / skipped columns keep
  their own width and the participating columns fill around them. Single-column `BestFitColumn`
  always freezes a pixel width and pins that column out of fill management. Changing the flag
  does not itself resize — call `BestFitAllColumns()` to apply.
- **Sample**: Columns → "Best Fit (Auto-Width)" — 2,000 rows with long Customer/Job outliers
  planted past the first viewport (VisibleRows vs AllRows comparison), per-column
  `BestFitMode` / `AllowBestFit` / `BestFitArea` overrides, a live `ActualDataWidth` readout,
  a "Fill viewport" toggle (`BestFitFillViewport`) over a splitter so the fill/overflow behavior
  is visible as the grid widens and narrows, and a Reload Data button with alternating outlier
  widths demonstrating `BestFitModeOnSourceChange`.

### Added — Summaries (total summary row + group summaries)
- **Summary model.** `SummaryItemType` enum (Count / Sum / Min / Max / Average) and
  `SummaryCalculator` (null-skipping aggregates, decimal-first with double overflow fallback,
  `IsTypeSupported` capability gate) in Core; `SummaryItem` Freezable (SummaryType +
  `DisplayFormat` — composite `{0:…}` replaces the whole text, a plain specifier formats the
  value, unset falls back to the column's `DisplayStringFormat`) and `SummaryResult`
  (type / raw value / formatted text) in WPF.
- **Column surface** (`GridColumn`): `TotalSummaries` + `GroupSummaries` (ctor-seeded
  `FreezableCollection<SummaryItem>`; item edits and collection mutations recompute via the
  Freezable `Changed` event), read-only `HasTotalSummaries` / `TotalSummaryText` /
  `TotalSummaryTextInfo`, `AllowedTotalSummaries` (`AllowedSummaries` flags, default `All` —
  gates the runtime picker only), `TotalSummaryContentStyle` + `ActualTotalSummaryContentStyle`
  (inherits the grid default), and a read-only `IsSortedBySummary` stub (sort-groups-by-summary
  is a later phase).
- **Total summary row.** Pinned beneath the data area (above the horizontal scrollbar), aligned
  per-column by the same header-mirroring panel the filter row uses —
  `FilterRowPresenter`'s tracking/layout core was extracted into a shared
  `ColumnAlignedRowPresenter` base, with `TotalSummaryRowPresenter` + per-column
  `TotalSummaryCell` as the second consumer. Grid surface: `ShowTotalSummary` (default `false`,
  explicit opt-in like the fixed panel — no content-based auto-collapse, so a shown row stays
  visible even with no summaries defined and the per-cell right-click picker remains reachable;
  resolved with `TotalSummaryPosition ≠ None` into read-only `ActualShowTotalSummaryRow`), plus
  grid-level `TotalSummaryContentStyle`. Theme: `Grid/TotalSummaryRow.xaml`
  (`GridSearchDataGridTotalSummaryRow` / `…TotalSummaryCell` / `…TotalSummaryCellContextMenu`).
- **Totals compute over the filtered leaf rows** — the same set `FilteredItemCount` reports
  (grouping-aware, collapse-independent, never the header sentinels). Recompute is coalesced
  per dispatcher tick and triggered by every filter / source / projection change, cell edit
  commits, and summary-definition edits; `RefreshSummaries()` is the explicit synchronous
  trigger.
- **Runtime summary picker.** Right-click a totals cell → toggle Count / Sum / Min / Max /
  Average (check glyph marks active aggregates) or Clear Summaries. Items disable when blocked
  by `AllowedTotalSummaries` or unsupported by the column's `FieldType`
  (`SummaryCalculator.IsTypeSupported`).
- **Group summaries are one grid-level set, shared by every level.**
  `SearchDataGrid.GroupSummaries` (ctor-seeded `FreezableCollection<SummaryItem>`) defines the
  summaries rendered identically in EVERY group header at every level; each `SummaryItem`
  declares its aggregation target via **`FieldName`** (caption-qualified
  `Function(Caption)=value`), so the set can span many columns. Computed per `GroupNode` at
  projection time, surfaced as `GroupSummaryLeftText` / `GroupSummaryRightText` on both
  `GroupHeaderRow` and `FixedGroupHeaderEntry` so the in-body header and the pinned strip
  render identically. Entries are **right-aligned at the header row's right edge by default**
  (per-item `SummaryItem.Alignment` can put them inline after the header content instead),
  ordered by `SummaryItem.OrderIndex`. Grids with neither group summaries nor the row count
  skip the per-node aggregation entirely. (There is no per-column group-summary property.)
  A committed cell edit (and any summary-definition edit) recomputes the texts **in place** —
  the live `GroupNode`s update and both header surfaces refresh via `INotifyPropertyChanged`
  with no reflatten, so row containers, focus, and scroll position survive the edit.
- **Totals cells can mix columns.** A `GridColumn.TotalSummaries` entry may target another
  column's field via `SummaryItem.FieldName` — it computes over that field but renders under
  the owning column's cell, caption-qualified (`Min(Discount)=…`); own-column entries stay
  bare (`Min=…`). Value extraction is cached per distinct target path per recompute.
  The cell's quick-pick menu (Count / Sum / Min / Max / Average) reads and toggles ONLY the
  column's own entries — a foreign-target entry like `Max(OrderDate)` rendered under the cell
  neither checks the cell's Max item nor gets removed by toggling it; cross-column entries
  are managed through the Customize editor. (Clear Summaries still clears the whole cell.)
- **Group header row count is a summary now, not chrome (behavior change).** The hardcoded
  `(N)` count chip is gone from the default group header. Row count is the opt-in
  `SearchDataGrid.ShowGroupRowCount` summary entry ("Show row count" in the View Totals
  editor; default `false` → default `Count=N`, right-aligned), with formatting / placement
  configurable via `SearchDataGrid.GroupRowCountSummary`. Consumer `GroupHeaderTemplate`s that
  render their own counts are unaffected (`ItemCount` still exposed).
- **Group header content is viewport-pinned.** The in-body group header's content (gutter,
  chevron, text, summary runs) clamps to the viewport width and counter-translates during
  horizontal scroll, so the header text stays visible and the right-aligned summary run sits
  at the visible right edge — not at the column-extent edge offscreen.
- **`SummaryItem` formatting/placement surface**: `Prefix` / `Suffix` (entry renders
  `Prefix + value + Suffix` when either is set), `Alignment` (Left/Right, default Right),
  `OrderIndex` (run position, editor-written).
- **Summary editor, three modes.** Items tab: column list (names render **bold** when the
  column carries configured summaries in the editor's scope) + Max / Min / Average / Sum
  toggles per selected column (gated by `SummaryCalculator.IsTypeSupported`) + "Show row
  count" (hidden in column-totals mode). Order and Alignment tab: Left side / Right side
  entry lists, ▲▼ reorder + ◀▶ re-side arrows, per-entry Prefix / Display format / Suffix
  with a live `Example:` preview. In column-totals mode the tab is just "Order" — alignment
  doesn't apply to a vertically-stacked cell, so the left side and ◀▶ arrows disappear and a
  single full-width "Order:" list reorders with ▲▼ only. Each side list binds its own selection (a single shared
  SelectedItem across two ListBoxes self-clears in WPF — this was wiping the selection,
  leaving the editing fields blank and the move arrows permanently disabled); the first
  configured entry pre-selects on open. The Display-format combo's presets are built per
  selected entry — its current custom format first (so existing values like
  `"Latest: {0:MM/dd/yyyy}"` show and stay re-pickable), then the target column's
  `DisplayStringFormat`, then date- or numeric-type suggestions — and it stays editable for
  anything else. Edits a working copy; OK applies (one coalesced recompute / projection
  rebuild), Cancel discards.
  - `GroupSummaryEditor.ShowGroupDialog(grid)` ("View Totals") — the shared group-header set.
    Opened from the group-header and pinned-strip menus.
  - `GroupSummaryEditor.ShowColumnTotalsDialog(grid, column)` (**"Totals for 'X'"**) — one
    column's totals cell, mixing aggregation targets from any column. Opened from the
    totals-cell **Customize…** and the column header's **Customize Totals…**.
  - `GroupSummaryEditor.ShowFixedTotalsDialog(grid)` ("Customize Fixed Totals") — the fixed
    panel's own set; "Show row count" maps to the no-FieldName Count entry.
- **Fixed total summary panel owns its definitions.**
  `SearchDataGrid.FixedTotalSummaries` (ctor-seeded collection, `FieldName`-targeted entries;
  a no-FieldName Count entry is the grid row count) replaces the old derive-from-column-totals
  behavior. The panel stays visible while empty when `ShowFixedTotalSummary` is on, so its
  right-click menu — **Count** (check-marked toggle of the row-count entry) and
  **Customize…** — is always reachable. (`ActualShowFixedTotalSummary` removed.)
- **Totals context-menu coverage.** The totals-cell menu gains **Customize…** ("Totals for
  'X'"). The column-header menu gains a **Total Summaries** submenu — Show/Hide Total Summary
  Row (re-arms `TotalSummaryPosition` when it was `None`), Show/Hide Fixed Total Summary, and
  Customize Totals… (column-scoped) — so every summary surface stays reachable while hidden
  or collapsed.
- **Fixed total summary panel.** `SearchDataGrid.ShowFixedTotalSummary` (default `false`) shows
  one horizontal, non-scrolling run beneath the items combining every column's total summaries
  (caption-qualified entries, split left/right per `SummaryItem.Alignment`; read-only
  `FixedTotalSummaryLeftText` / `FixedTotalSummaryRightText`, resolved visibility
  `ActualShowFixedTotalSummary`).
- **`TotalSummaryPosition`** (`None` / `Top` / `Bottom`, default `Bottom`) docks the
  column-aligned totals row beneath the filter row or above the horizontal scrollbar; folded
  into `ActualShowTotalSummaryRow`.
- **Total summary cells stack vertically.** Multiple summaries on one column render one line
  per entry (bound to `TotalSummaryTextInfo`); `TotalSummaryContentStyle` /
  `ActualTotalSummaryContentStyle` now target the per-entry `TextBlock`.
- **Sample:** the planned "Total Summaries" slot (Data Shaping) is now a real
  `TotalSummariesSampleView` — declarative totals (Count / Sum / composite-formatted Average /
  Max date), `AllowedTotalSummaries` restriction, the runtime picker, `ShowTotalSummary` +
  `TotalSummaryPosition` + `ShowFixedTotalSummary` + `ShowGroupRowCount` knobs, and a
  "View Totals…" launcher. `BasicGroupingSampleView` opts into `ShowGroupRowCount` to keep its
  group counts.
- **Sort groups by summary value (3.2.d).** `SearchDataGrid.SortGroupsBySummary(summaryType,
  fieldName, direction)` orders the groups at every level by a summary aggregate over each
  group's leaf rows (default Descending — largest first) instead of by the group key; the
  group-key order survives as the stable tie-breaker, and leaf rows inside each group keep
  their order. `ClearGroupSummarySort()` restores key ordering; `IsGroupSummarySortActive`
  reports state; the target column's read-only `GridColumn.IsSortedBySummary` (formerly a
  stub) lights while active. `Count` with no `fieldName` sorts by group row count. The sort
  persists across reflattens (filters, toggles, regrouping). Value comparison reuses the
  summary engine's ranking (`SummaryCalculator.CompareValues`, now public — numerics across
  widths, same-type `IComparable`, string-form fallback, nulls first).
- **"Sort By Summary" on the group-panel pill menu.** Right-clicking a grouped column's pill
  now offers a Sort By Summary submenu built from the grid's configured group-summary
  content: an Ascending/Descending pair per `GroupSummaries` aggregate (e.g.
  `Sum by 'Total' - Ascending`, captioned by the target column), a Row Count pair when
  `ShowGroupRowCount` is on, and a trailing Clear Summary Sort (enabled while a summary sort
  is active). The active option renders check-marked; the submenu hides entirely when no
  group summaries are configured. Backed by the new read-only
  `SearchDataGrid.ActiveGroupSummarySort` (a `GroupSummarySortDescriptor` — fresh instance
  per change, so menu bindings re-evaluate) and
  `ContextMenuCommands.SortGroupsBySummaryCommand` over `GroupSummarySortOption` items.
  A direct sort on the grouped column — header click, menu Sort Ascending/Descending, or the
  pill's click-to-flip — supersedes the summary sort (clears it and restores key ordering;
  without this the click would look dead, since the summary sort overrides the key order).
  "Clear Sorting" clears it too. Sorts on non-grouped columns only reorder leaves within
  their groups and leave the summary sort in place.
- **Column-aligned group summaries (`GroupSummaryDisplayMode`).** Grid-level
  `GroupSummaryDisplayMode` (`Header` default / `AlignByColumns`): in AlignByColumns mode the
  group summaries render **in the group header row itself, each value aligned under its
  target column** — no separate summary row. The header template gains an extent-space layer
  beneath the viewport-pinned content: a `GroupSummaryCellsPresenter` hosting one
  `GroupSummaryCell` per visible column (width bound to the column, display order; the same
  cumulative layout the data cells use, so the values scroll with their columns while the
  group caption stays pinned). Cells rebuild on column reorder/visibility/mode flips via a
  grid-side presenter registry hooked into the column-state refresh; in Header mode the
  presenter builds no cells, so header rows pay nothing. The opt-in row count and entries
  that don't resolve to a column stay in the header runs. Per-node results live on the group
  tree (`GroupNode.AlignedSummaryResults`, read through `GroupHeaderRow`) and refresh in
  place with the other summary surfaces — no reflatten, ever (the row set never changes).
  Theme: `GridSearchDataGridGroupSummaryCell` / `…GroupSummaryCellsPresenter` in
  `GroupStyle.xaml`. The pinned fixed-group strip renders the same aligned values: its
  entries carry a `FixedGroupSummaryCellsPresenter` layer — built on the
  filter-row/totals-row `ColumnAlignedRowPresenter` base (header mirroring + horizontal
  scroll sync), since the strip is pinned chrome rather than scrolled content — hosting the
  same `GroupSummaryCell`s, fed by `FixedGroupHeaderEntry` through the shared
  `IAlignedGroupSummarySource`. The layer collapses entirely outside AlignByColumns mode.
- **Group-header viewport pinning is code-owned.** The in-body header's counter-translation
  moved from a XAML `RelativeSource` binding on the `TranslateTransform` (a Freezable with no
  governing FrameworkElement — silently fails to resolve inside row templates) to the new
  `ViewportPinBehavior` attached behavior, which tracks the ancestor `ScrollViewer`'s
  `HorizontalOffset` in code.
- **Sample:** `TotalSummariesSampleView` adds a group-summary display-mode picker
  (Header / Align by columns) and Sort-groups-by-summary buttons (Sum(Total) / row count / clear).

### Changed — Unified window chrome (`ThemeKeys.PrimitivesWindow`)
- One generic window style now serves every window the library opens — Filter Editor, group
  summary editor ("View Totals"), and Column Chooser. The chrome is the SampleApp's window
  style promoted into the theme: borderless DWM window with rounded corners, drop shadow, and
  accent border (active `#0078D4`, inactive `#DDDDDD`), 30px caption, Segoe UI Variable, and an
  `AdornerDecorator` around the content (the old per-dialog templates lacked one, breaking
  adorner lookups inside the hosted content).
- `DwmWindowHelper` moved from the SampleApp into `WWControls.Wpf` (attached
  properties for DWM shadow / border color / corner rounding / taskbar-respecting maximize);
  the SampleApp consumes the library copy and its `SampleAppWindowStyle` is gone —
  `LauncherWindow` uses `PrimitivesWindow`.
- Caption buttons are taskbar-aware: Close always; Max/Restore when the window is resizable;
  Min only when `ShowInTaskbar` is `true` (the library's dialogs aren't taskbar windows, so
  they get Close — plus Max when resizable — instead of a minimizable modal). The buttons
  invoke `SystemCommands`; the library wires per-window command bindings on its own hosts
  (`WindowHostHelper`), so no app-level registration is needed.
- Closing a dialog via the chrome's X is equivalent to Cancel (`DialogResult` stays false);
  the Column Chooser's X closes the window exactly as its old `CloseCommand` chrome did.

### Removed
- `ThemeKeys.FilterEditorWindow` and `ThemeKeys.ColumnChooserWindow` (and their near-duplicate
  styles) — superseded by `ThemeKeys.PrimitivesWindow`. `ColumnChooser.WindowStyle` remains the
  per-instance override hook.

### Changed — Grouping rebuilt on a flat row projection (replaces `GroupItem`/`Expander`)
- Grouping no longer uses WPF's `CollectionView` `GroupDescriptions` → `GroupStyle`/`GroupItem`/
  `Expander` hierarchical virtualization. When a grid is grouped, the group tree is now projected
  into a single flat list of group-header sentinels (`GroupHeaderRow`) interleaved with the raw
  data rows and set as the DataGrid's effective `ItemsSource`, so the rows panel virtualizes it as
  one uniform list. This removes the per-`GroupItem` realize/measure cost that made grouped
  scrolling stutter at depth and with collapsed nested groups; grouped scrolling now matches
  ungrouped smoothness.
- This is the only grouping engine — there is no opt-in flag. (The transitional `UseFlatGrouping`
  switch added earlier in this unreleased cycle is gone.)
- All grouping behaviors are preserved: declarative `GroupIndex`, `GroupBy`/`Ungroup`/
  `ClearGrouping`, group-leads-sorting, `ShowGroupedColumns`, `GroupInterval` buckets, per-group
  expansion persistence, Expand/Collapse all, recursive expand (`ExpandGroupsRecursively`), the
  sticky fixed-group strip (`AllowFixedGroups`), the group panel, and the filter row while grouped.
- Group headers render as full-width `DataGridRow`s (via `SearchDataGridRow` + an `IsGroupHeader`
  trigger) and are non-selectable / non-editable / skipped by keyboard navigation and copy. The
  in-body header right-click menu is preserved (`ExpandGroupCommand` / `CollapseGroupCommand` /
  `ExpandAllAtLevelCommand` / `CollapseAllAtLevelCommand` / `UngroupAtLevelCommand`, taking a
  `GroupHeaderRow`). The sticky strip's resolver is now an index lookup into the projected rows
  rather than a visual-tree `GroupItem` walk.

### Removed
- `SearchDataGrid.UseGroupExpansionAnimation` and the animated group-`Expander` template /
  `GroupExpansionAnimator` — there is no `Expander` chrome to animate.
- `SearchDataGrid.TrackGroupExpansion` attached property — expansion state now persists in the
  engine's path-keyed map, not per-`Expander`.
- The default `GroupStyle` (`GroupItem`/`Expander`) chrome and its legacy group-header context menu.

### Added — Allow Fixed Groups (sticky group headers)
- `SearchDataGrid.AllowFixedGroups` (bool, default `false`). When `true` on a grouped grid the
  group header(s) for the topmost visible row stay pinned to the top of the data area; as the
  next sibling rises into the strip, the topmost pinned header slides up to make room (push
  transition). Outer-to-inner stacking matches the in-place stair-step indent.
- `FixedGroupHeadersPresenter` (new `ItemsControl` in `Controls/Grouping/`) overlaid in the
  grid template; `FixedGroupHeaderEntry` (per-pinned-level view-model with TwoWay `IsExpanded`);
  `FixedGroupIndentConverter` (per-level `Margin.Left` matching `GroupHeaderMarginConverter`'s
  output formula).
- Pinned chrome carries its own context menu: `ContextMenuCommands.ExpandFixedGroupCommand` /
  `CollapseFixedGroupCommand` / `ExpandAllAtFixedLevelCommand` / `CollapseAllAtFixedLevelCommand` /
  `UngroupAtFixedLevelCommand`. Sibling set to the in-place commands but takes a
  `FixedGroupHeaderEntry` instead of an `Expander`.
- Click on a pinned header routes through `SearchDataGrid.ApplyFixedGroupExpansion`, which
  toggles the realized real `Expander` (firing the existing class handler that updates
  `_groupExpandState`) or writes straight to the persistence map when the represented group is
  virtualized — so the in-place chrome and the persistence layer stay coherent.
- Sample: `BasicGroupingSampleView` (Data Shaping › Grouping) ships an `Allow fixed groups`
  toggle in the OPTIONS group alongside the rest of the grouping switches. Use the GROUP BY
  buttons in the sample to add nested levels and watch the strip stair-step.

### Added — AutoFilterRow spec conformance
- Grid-level: `ShowCriteriaInAutoFilterRow`, `AutoFilterRowCellStyle`, `FilterRowDelay`, `AutoFilterRowClearButtonMode`, `EnableLiveFiltering`
- Column-level: `AllowAutoFilter`, `ShowCriteriaInAutoFilterRow`, `AutoFilterRowCellStyle`, `AutoFilterRowDisplayTemplate`, `AutoFilterRowEditTemplate`, `DefaultSearchType`, `RoundDateTime`
- `EditGridCellData` hierarchy (`DataObjectBase` → `EditableDataObject` → `GridDataBase` → `GridColumnData` → `GridCellData` → `EditGridCellData`) for template authors
- `DispatcherTimer`-based keystroke debounce replacing the dead `System.Timers.Timer` infrastructure
- `docs/column-filter-mode.md` documenting how display properties drive raw vs display-text filter comparison

### Changed
- String columns default to `DefaultSearchType.StartsWith` (was `Contains`). Spec-aligned.
- Live-filtering is now a single grid-level DP, `SearchDataGrid.EnableLiveFiltering` (default `true`). When `false`, popup edits and auto-filter-row typing defer until commit (Enter / Tab / focus loss / popup close). Replaces the popup's per-session "Live filter" checkbox, the per-column `GridColumn.ImmediateUpdateAutoFilter` DP, and the 100,000-row auto-disable threshold (`SearchDataGrid.LiveFilteringRowCountThreshold`).
- `AutoFilterRowClearButtonMode` enum reshaped to `Never` / `Always` / `Display` / `Edit` (was `Default` / `WhenFilterApplied` / `Always` / `Never`). Default is now `Always`. `Display` / `Edit` now resolve from the filter cell's display/edit state — see below.
- **Auto-filter row now has a display / edit state machine** mirroring the row's data-cell editor. `ColumnFilterControl.IsFilterCellEditing` tracks `IsKeyboardFocusWithin`; the host swaps between a read-only `TextBlock`-shaped display surface (no decoration buttons) and the full editor produced by `BaseEditSettings.CreateFilterEditor` (with chevron / calendar / spinner chrome). Click into a filter cell or tab to it to enter edit mode; focus out demotes back to display. `BaseEditSettings` gains a `CreateFilterDisplay(host)` virtual method overridden by `ComboBoxEditSettings`, `DateEditSettings`, and `SpinEditSettings` for type-appropriate formatting.

### Fixed
- Clear button now appears on all editor surfaces (combobox, date, checkbox via `EditSettings`) when a filter is active — previously visibility only tracked `SearchText.Length`, so typed-value editors never surfaced the button
- DateTime column `=` (and the rest of `<` / `<=` / `>` / `>=` / `Between`) now honors `SearchCondition.RoundDateTime`. Previously only `BetweenDates` / `NotBetweenDates` rounded, so picking a date in the auto-filter row never matched column values that carried a non-zero time-of-day. `SearchEngine.CompareValues` now compares via `.Date` when the flag is set. The column-level `RoundDateTime` DP (nullable `bool`) overrides the auto-detection that samples bound values for time-of-day; `null` (default) rounds when the column has no times and keeps the full instant when it does — matching the editor shape produced by `BuildAutoDateTimeEditSettings`

### Removed
- Deprecated `DefaultSearchMode` enum and CLR property (use `DefaultSearchType`)
- Deprecated `ColumnFilterControl.IsLiveFilteringOverride` DP (use `SearchDataGrid.EnableLiveFiltering`)
- `GridColumn.ImmediateUpdateAutoFilter` DP, `SearchDataGrid.LiveFilteringRowCountThreshold` const, and the popup footer's "Live filter" checkbox — all replaced by `SearchDataGrid.EnableLiveFiltering`

## [0.2.0] - 2026-03-30

### Added
- Column chooser with drag-drop reordering and show/hide toggles
- `IsColumnChooserEnabled`, `IsColumnChooserVisible`, `IsColumnChooserConfinedToGrid` properties
- Select-all checkbox in boolean column headers with `IsSelectAllColumn` and `SelectAllScope`
- `ColumnDisplayName` attached property for custom display names in filter panel and column chooser
- `DefaultSearchMode` attached property (Contains, StartsWith, EndsWith, Equals)

### Changed
- Split `SearchDataGrid.cs` (2,700 lines) into 5 focused partial class files
- Split `ColumnSearchBox.cs` (1,700 lines) into 3 focused partial class files
- Cached all RelayCommand instances (45 properties) to eliminate per-access allocations
- Added reflection caching in `ReflectionHelper.GetPropValue` for filtering performance
- Added compiled Regex caching in `IsLikeEvaluator`
- Improved thread safety in `SearchEvaluatorFactory` and `CommandManager`

### Fixed
- Event handler memory leaks in `SearchDataGrid.OnApplyTemplate` (events never unsubscribed on re-template)
- `DependencyPropertyDescriptor.AddValueChanged` memory leak in constructor
- `FilterExpressionBuilder` returning pass-all expression for collection-context-only filters
- Empty catch block in `ColumnFilterEditor.OnAutoApplyFilter` silently swallowing all exceptions
- Removed forced `GC.Collect()` from library code in `ClearAllCachedData`
- Eliminated duplicate `IsNumericType` and `ConvertToDouble` implementations

## [0.1.0] - Initial Development

### Added
- `SearchDataGrid` control extending WPF DataGrid with per-column filtering
- 25+ search types via pluggable evaluator architecture
- `ColumnSearchBox` with simple text search and advanced filter popup
- `ColumnFilterEditor` for multi-criteria filter rule building
- `FilterPanel` displaying active filters as chips
- Expression tree compilation for high-performance filtering
- `GridColumn` attached properties for column configuration
- Checkbox column support with three-state cycling
- Built-in context menus (copy, sort, best-fit, filter management)
- Auto-size columns feature
- Cell value change tracking with `CellValueChanged` event
- Full WPF theming via Generic.xaml pattern
- Sample application demonstrating all features
