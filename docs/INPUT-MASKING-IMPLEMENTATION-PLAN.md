# Input Masking — Implementation Plan

Roadmap for evolving the input-masking subsystem from a single `Simple`-grammar engine into a polymorphic, per-`MaskType` formatter framework that mirrors the conceptual model used by DevExpress and similar editor frameworks.

---

## Current state

What ships today:

- `MaskFormatter` (Core) — region/slot engine extracted from `WWFormattedTextBox`. Handles the "literal characters + placeholder slots" grammar (`(000) 000-0000`, `0+\.00`, `LLL-000`, etc.).
- `MaskInputBehavior` (WPF) — attached behavior that wires `MaskFormatter` onto any `TextBox`. Handles `PreviewTextInput`, Backspace/Delete, Tab between regions, paste, focus-enter (region select), focus-exit (finalize).
- `MaskFormatConverter` — display-only `IValueConverter` for one-shot formatting.
- `MaskDisplayProvider` — implements `IDisplayValueProvider` against `MaskFormatter`.
- `GridColumn.DisplayMask` — column-level fallback mask.
- `MaskType` enum — declares `Simple`, `Numeric`, `DateTime`, `DateOnly`, `TimeOnly`, `DateTimeOffset`, `TimeSpan`, `RegEx`, `SimpleRegEx`. **Only `Simple` is implemented**; the others throw `NotSupportedException` from `EnsureMaskTypeImplemented` in `TextEditSettings`.
- `TextEditSettings` — `Mask`, `MaskType`, `UseMaskAsDisplayFormat` DPs. Single masked-text editor (the former `MaskedEditSettings` was folded into this — set `UseMaskAsDisplayFormat=True` to get the same display-as-mask behavior).
- `DateEditSettings` — `Mask` DP applied to inner `DatePickerTextBox` via `MaskInputBehavior`. Default `00/00/0000`.

What's missing:

- A polymorphic seam. `MaskInputBehavior` is hard-wired to `MaskFormatter`. Every additional `MaskType` would need its own behavior class without a refactor.
- Engines for the other eight `MaskType` values.
- Unit tests for `MaskFormatter` (no regression safety net for refactors).

---

## Target architecture

```
                                  ┌─ SimpleMaskFormatter
                                  ├─ NumericMaskFormatter
MaskInputBehavior ──IMaskFormatter┼─ DateTimeMaskFormatter
                  ▲               ├─ TimeSpanMaskFormatter
                  │               └─ RegExMaskFormatter
       MaskFormatterFactory.Create(MaskType, mask, options, culture)
```

`IMaskFormatter` is the contract that `MaskInputBehavior` already implicitly depends on. Pulling it out makes every `MaskType` swap-in.

Proposed shape:

```csharp
public interface IMaskFormatter
{
    string Format(object value);
    string Parse(string display);            // returns raw / unmasked
    string StripLiterals(string text);
    bool IsMaskComplete { get; }
    string UnmaskedValue { get; }

    // Per-keystroke
    (string text, int caret) InsertChar(char c, int caret);
    (string text, int caret) DeleteChar(int caret, bool forward);
    void ClearSelection(int start, int length);
    (string text, int caret) Paste(string text, int caret, int selLength);
    string Finalize();

    // Region navigation — default no-op for non-region types (Numeric, RegEx)
    int DisplayLength { get; }
    (int regionIndex, int localOffset) GetRegionAtCaret(int caret);
    (int start, int length) GetEditableRegionBounds(int regionIndex);
    int GetNextEditableRegionStart(int fromIndex);
    int GetPrevEditableRegionStart(int fromIndex);
    int GetFirstEditableRegionIndex();
}
```

Numeric and RegEx don't have the "regions of literals + slots" model. The navigation methods return sentinel values for those types; `MaskInputBehavior` already handles the "no regions" case by falling back to default Tab navigation.

---

## Phase 0 — foundation refactor

**The most important phase, and the one easiest to skip. Do it first.**

Without this seam, every subsequent phase ends up duplicating `MaskInputBehavior` plumbing per type.

### Deliverables

1. Define `IMaskFormatter` in `WWControls.Core.Display` with the surface above.
2. Rename `MaskFormatter` → `SimpleMaskFormatter`. Implement `IMaskFormatter`. Keep public class name available via type forward or `[Obsolete]` partial if external consumers exist (today: none).
3. Add `MaskFormatterFactory.Create(MaskType type, string mask, char promptChar, CultureInfo culture)`. Throws `NotSupportedException` for unimplemented types — same diagnostic as today, but in one place. `EnsureMaskTypeImplemented` in `TextEditSettings` collapses to a single factory call.
4. Update `MaskInputBehavior.Attach` to call the factory instead of `new MaskFormatter(...)`.
5. **Add a test project** (`WWControls.Core.Tests` if it doesn't exist). Lock down current `SimpleMaskFormatter` behavior: every example pattern in the docs (`(000) 000-0000`, `00/00/0000`, `0+\.00`, `LLL-000`, `00000-9999`, `LLL-000`, `0000-0000-0000-0000`) tested for Format / Parse / InsertChar / DeleteChar / Paste / Finalize round-trips. **This is the safety net for every subsequent phase.**

### Effort

~1–2 days. Zero behavior change. Reviewable in isolation.

### Acceptance

- `dotnet build` clean.
- All sample columns in `InputMaskingSample` render and behave identically to before.
- `WWControls.Core.Tests` runs green.

---

## Phase 1 — `MaskType.Numeric`

Highest user value, common ask, no architectural dependencies once Phase 0 is done.

### Scope

`NumericMaskFormatter` accepts standard .NET numeric format strings (`C`, `C2`, `N0`, `N2`, `P0`, `F2`, custom `#,##0.00`) and a `CultureInfo`. Internally it doesn't use the region/slot model — it tracks:

- `IntegerDigits`, `FractionalDigits`, `Sign` (`+` / `-` / none), `IsPercent` / `IsCurrency` (parsed from the format string)
- Allowed keystrokes per caret position (digit / culture decimal separator / culture group separator / culture negative sign / `()` for accounting style)
- `Format(decimal)` via `decimal.ToString(format, culture)`
- `Parse(string)` via `decimal.TryParse(stripped, NumberStyles.Any, culture)`

### Design decisions to make upfront

- **Culture changes mid-app.** Cache the formatter, but invalidate on `CultureInfo.CurrentCulture` change. Or accept culture as a constructor param and force callers to rebuild.
- **Negative zero, scientific notation, `BigInteger`, `nullable<decimal>`.** Decide what's in/out of scope.
- **Group separator while typing.** As digits accumulate, separators shift (`1234` → `1,234`). Simplest model: re-format on every keystroke, place caret intelligently. Excel does this and feels right.

### Deliverables

- `NumericMaskFormatter : IMaskFormatter`
- `MaskFormatterFactory.Create(MaskType.Numeric, ...)` returns it
- Remove the `Numeric` case from `EnsureMaskTypeImplemented`'s throw list
- Unit tests covering: `C2`, `N0`, `N2`, `P0`, `F2`, `#,##0.00`, custom; en-US, de-DE, fr-FR cultures; negative numbers, zero, max/min decimal
- Sample column: currency, percentage, fixed-precision, accounting parens

### Effort

~2–3 days including tests.

---

## Phase 2 — `MaskType.DateTime` (and trivially `DateOnly` / `TimeOnly`)

### Scope

`DateTimeMaskFormatter` translates a .NET date format string into an internal slot model and reuses `SimpleMaskFormatter`'s region engine under the hood. `MM/dd/yyyy` becomes `Fixed[2 digits]` + `Literal["/"]` + `Fixed[2 digits]` + `Literal["/"]` + `Fixed[4 digits]` — exactly what `SimpleMaskFormatter` already handles. So you write one translator and delegate.

Format strings to support: `d`, `D`, `f`, `F`, `g`, `G`, `M`, `O`, `R`, `s`, `t`, `T`, `u`, `U`, `Y`, custom.

### The harder part — calendar integration

Today `DateEditSettings` puts `MaskInputBehavior` on the inner `DatePickerTextBox` with a hard-coded `00/00/0000`. With `MaskType.DateTime`, the mask comes from `column.DisplayStringFormat` (or an explicit `Mask` DP). When the user picks via the calendar popup, the popup writes a `DateTime` to `SelectedDate`; you need to round-trip that through the mask formatter so the inner TextBox stays consistent.

### Per-slot validation (stretch)

Validating per slot — month 1–12, day 1–31, hour 0–23 — is a stretch goal. Without it, `13/45/2026` will type fine and only fail on commit. With it, you reject keystrokes that can't lead to a valid date. Excel does this; nice-to-have, not blocking.

### Deliverables

- `DateTimeMaskFormatter : IMaskFormatter` (delegating to `SimpleMaskFormatter` internally)
- `DateOnlyMaskFormatter`, `TimeOnlyMaskFormatter` — trivial subsets, same translator, different parse target type
- DatePicker round-trip integration (popup ↔ masked TextBox)
- Tests: format-string → mask translation, round-trip Format/Parse, locale variants
- Sample columns: short date, long date, time-only

### Effort

~3–5 days for the full set including calendar integration. Add ~1 day if you do per-slot validation.

---

## Phase 3 — `MaskType.TimeSpan`

### Scope

`TimeSpanMaskFormatter` follows the same translation-to-`SimpleMaskFormatter`-regions pattern with TimeSpan format strings (`g`, `G`, `c`, custom `d\.hh\:mm\:ss`). Less complexity than DateTime because there's no calendar popup to coordinate.

### Deliverables

- `TimeSpanMaskFormatter : IMaskFormatter`
- Tests for `g` / `G` / `c` / custom; positive and negative intervals
- Sample column: duration

### Effort

~2 days.

---

## Phase 4 — `MaskType.RegEx` and `SimpleRegEx`

**The hardest by a wide margin.** The model can't reuse the region engine — regex semantics fundamentally don't decompose into "literals + fixed slots" once you have alternates, quantifiers, and character classes.

### The core problem: partial-match validation

Given a regex `[A-Z]{3}-\d{4}` and input `AB`, accept the keystroke because `AB` is a valid prefix of some matching string. .NET's `Regex.Match` only does full matches; you need either:

- **Real solution:** an NFA simulator that tracks "states reachable after this prefix"
- **Pragmatic shortcut:** convert the regex into a "consumed pattern + remaining pattern" pair, run `Regex.Match` against `consumedPattern + ".*"` to check feasibility. Works for non-pathological regexes, fails on backreferences, lookaround, etc.

Auto-complete (DevExpress's draw card for this type) requires walking the regex AST to find required literals after the current cursor. That's its own subproject.

### Recommended approach

Start with the pragmatic shortcut, document the limitations, and only invest in a real NFA if real consumers run into the limits.

### Deliverables

- `RegExMaskFormatter : IMaskFormatter` (pragmatic prefix-feasibility validator)
- `SimpleRegEx` reuses the same engine with simpler grammar parsing — bundled essentially free
- Tests: char-class prefix acceptance, alternation, quantifiers, well-known patterns (email, IPv4, GUID)
- Documentation of unsupported regex features (backreferences, lookaround)

### Effort

~5–7 days for pragmatic, ~2 weeks for the NFA route.

---

## Phase 5 — `MaskType.DateTimeOffset`

### Scope

Builds on Phase 2 (DateTime) + a small offset region (`+05:00`, `-08:00`).

### Deliverables

- `DateTimeOffsetMaskFormatter : IMaskFormatter` (DateTime delegate + offset region)
- Tests: round-trip, DST boundaries, extreme offsets
- Sample column: timestamped record

### Effort

~2 days assuming Phase 2 is solid.

---

## Cross-cutting concerns

These don't fit cleanly into a phase but matter for a "well-rounded" engine.

### Formatter caching

Currently a new `MaskFormatter` is created per cell template build. Fine for Simple (cheap), wasteful for Numeric/DateTime. Cache by `(MaskType, mask, culture)` tuple, invalidate on culture change. Tackle alongside Phase 1 — Numeric is where the cost first becomes noticeable.

### Validation feedback

When commit fails (`Parse` throws or returns `null`), the cell should show an error state, not silently revert. Hook `Binding.ValidatesOnExceptions` or add a custom `ValidationRule`. Most useful in Numeric/DateTime where invalid commits are common. Tackle alongside Phase 1.

### IME / dead-key handling

Already correct because `MaskInputBehavior` uses `PreviewTextInput`, but worth a regression test in each phase.

### RTL / bidi text

Mostly orthogonal because masks are LTR-structured, but if you ever support Arabic/Hebrew columns, the caret math needs review.

### Documentation per type

Each `MaskType` needs a docs page with grammar reference + 5–10 worked examples. Without that, the API is hard to discover. Add a `MASK-TYPES.md` companion to this plan after Phase 1.

---

## Suggested sequence

| # | Phase | Why now | Dependencies |
|---|---|---|---|
| 0 | Foundation refactor | Leverage point for everything else; tiny but unblocking | none |
| 1 | Numeric | Highest user value; numeric columns are everywhere | Phase 0 |
| 2 | DateTime / DateOnly / TimeOnly | Natural progression; date columns are also high-frequency | Phase 0 |
| 3 | TimeSpan | If you have duration columns; otherwise defer | Phase 0 |
| 4 | RegEx / SimpleRegEx | When a real consumer asks; large design space | Phase 0 |
| 5 | DateTimeOffset | When needed; often skipped in line-of-business apps | Phase 2 |

### Shippability between phases

After Phase 0 you can ship; nothing breaks, the API is shaped right, and `NotSupportedException` is the only diagnostic for unimplemented types. Each subsequent phase is independently shippable — you can release Numeric without DateTime, DateTime without RegEx, etc. The user-facing story stays consistent: "set `MaskType` to what your column needs; if it throws, it's not implemented yet, here's the workaround."

---

## Risks and open questions

- **Round-trip integrity.** `Format(Parse(formatted)) == formatted` and `Parse(Format(value)) == value` should hold for every type. Edge cases: leading/trailing zeros, negative zero, `DateTime.MinValue`, max-precision decimals, daylight-saving boundaries.
- **Calendar popup coupling.** The DatePicker popup pushes `DateTime` values into the underlying TextBox text via WPF's own pipeline. Coordinating that with `MaskInputBehavior` without double-formatting needs careful sequencing.
- **Type-to-edit interaction.** `OnGridPreviewTextInput` injects the seed character via `tb.Text = typedText`, bypassing `MaskInputBehavior.OnPreviewTextInput`. The seed keystroke is unvalidated; subsequent keystrokes are validated. Either accept this rough edge or route the seed through the formatter — decide per phase.
- **Regex engine choice.** Pragmatic prefix feasibility vs. real NFA. Defer until a consumer needs it; the design space is large and what users actually want varies.

---

## Success criteria for the full engine

- Every `MaskType` enum value has a working formatter.
- Every formatter has unit tests covering format / parse / per-keystroke / paste / finalize round-trips.
- Every formatter has at least one sample column in `InputMaskingSample`.
- Cross-culture tests (en-US, de-DE, fr-FR, ja-JP) for numeric and date types.
- Documentation page (`MASK-TYPES.md`) covers grammar + examples for each type.
- Validation feedback in cells: invalid commits visibly fail rather than silently reverting.
- Formatter cache prevents per-cell allocation overhead in large grids.

When all of the above is true, the input-masking subsystem matches DevExpress in conceptual coverage and behavioral feel.
