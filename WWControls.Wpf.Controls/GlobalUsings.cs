// The primitive and editor controls live in one assembly under two folder-aligned namespaces. These
// global usings let any file here reach across the two without per-file usings. The namespaces share
// no type names, so pulling them both into scope can never introduce an ambiguity.
global using WWControls.Wpf.Controls.Primitives;
global using WWControls.Wpf.Controls.Editors;
