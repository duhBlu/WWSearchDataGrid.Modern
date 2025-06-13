# Project Structure - WWSearchDataGrid.Modern.WPF

This document outlines the organization of the WPF project following best practices for custom controls and XAML-based UI components.

## Folder Structure

### `/Controls/` - Custom Controls (Code-Only)
Contains custom controls that inherit from base WPF controls and use Generic.xaml for styling.
**No XAML files should be in this folder** - only `.cs` files.

- `AdvancedFilterControl.cs` - Custom control for advanced filtering UI
- `FilterEditDialog.cs` - Custom control for editing multiple column filters
- `FilterPanel.cs` - Custom control for displaying active filter chips
- `NumericUpDown.cs` - Custom numeric input control  
- `SearchControl.cs` - Custom control for column search functionality
- `SearchDataGrid.cs` - Main custom DataGrid with search capabilities

### `/Themes/Controls/` - Custom Control Styles
Contains XAML theme files that define the default styles and templates for custom controls.
**Only styling XAML files** - no code-behind.

- `AdvancedFilterControl.xaml` - Default style and template for AdvancedFilterControl
- `FilterEditDialog.xaml` - Default style and template for FilterEditDialog
- `FilterPanel.xaml` - Default style and template for FilterPanel  
- `NumericUpDown.xaml` - Default style and template for NumericUpDown
- `SearchControl.xaml` - Default style and template for SearchControl
- `SearchDataGrid.xaml` - Default style and template for SearchDataGrid

### `/Themes/Generic.xaml`
Main theme file that merges all control-specific theme files. This is the standard WPF convention for custom control libraries.

### `/Templates/` - Reusable Templates
Contains reusable DataTemplates and TemplateSelectors.

- `FilterValueTemplates.xaml` - Templates for different filter value input types
- `FilterValueTemplateSelector.cs` - Logic for selecting appropriate templates

### `/Converters/` - Value Converters
Contains IValueConverter implementations for data binding.

### `/Resources/` - Static Resources
Contains icons, images, and other static resources.

## Design Principles

1. **Custom Controls Only** (`/Controls/`) - All UI components are custom controls for maximum reusability and styling flexibility
2. **Generic Styling** - All controls use Generic.xaml approach for easy end-user customization
3. **Separation of Concerns** - Logic in `.cs`, styling/presentation in `/Themes/`
4. **Reusability** - Templates and converters are designed for reuse across components
5. **Maintainability** - Clear folder structure makes it easy to find and modify components
6. **End-User Friendly** - Easy for consumers to create custom styles without dealing with code-behind

## Naming Conventions

- **Custom Controls**: `{Name}Control.cs` or `{Name}Dialog.cs` in `/Controls/`, styled in `/Themes/Controls/{Name}Control.xaml` or `/Themes/Controls/{Name}Dialog.xaml`
- **Converters**: `{Purpose}Converter.cs` in `/Converters/`
- **Templates**: `{Purpose}Templates.xaml` in `/Templates/`

## Benefits of This Approach

- **Consistent Architecture** - All components follow the same custom control pattern
- **Easy Styling** - End users can easily override any control's appearance via Generic.xaml
- **No Code-Behind** - Eliminates XAML code-behind files that are harder to customize
- **Professional Library Feel** - Behaves like built-in WPF controls
- **Better IntelliSense** - Custom controls provide better design-time support

This structure follows WPF best practices and makes the codebase more maintainable and easier to understand.