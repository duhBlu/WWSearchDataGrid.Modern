// The WWControls.Wpf code is organized into sub-namespaces by folder (Editors / Primitives /
// SearchDataGrid) on top of the shared root and the existing Behaviors / Commands / Converters /
// Display namespaces. These global usings let any file in the assembly reference types across
// those sub-namespaces without per-file usings — they were a single flat namespace before the
// split, so no two of them share a type name and these can never introduce an ambiguity.
global using WWControls.Wpf;
global using WWControls.Wpf.Controls.Editors;
global using WWControls.Wpf.Controls.Editors.Settings;
global using WWControls.Wpf.Controls.Primitives;
global using WWControls.Wpf.Grids;
global using WWControls.Wpf.Behaviors;
global using WWControls.Wpf.Commands;
global using WWControls.Wpf.Converters;
global using WWControls.Wpf.Display;
global using WWControls.Wpf.Icons;
