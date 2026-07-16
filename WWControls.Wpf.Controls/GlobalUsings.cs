// The primitive and editor controls live in one assembly but keep their distinct namespaces. These
// global usings let any file here reach its siblings across those namespaces without per-file usings.
// WWControls.Wpf.Behaviors holds MaskInputBehavior (used by the editors). The sub-namespaces share no
// type names, so pulling them all into scope can never introduce an ambiguity.
global using WWControls.Wpf.Primitives;
global using WWControls.Wpf.Editors;
global using WWControls.Wpf.Behaviors;
