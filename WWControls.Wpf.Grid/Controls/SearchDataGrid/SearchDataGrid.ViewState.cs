using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace WWControls.Wpf.Grids
{
    public partial class SearchDataGrid
    {
        #region View-state persistence configuration

        /// <summary>
        /// Gets or sets a stable key that identifies this grid for view-state persistence. Used to
        /// scope the auto-remembered "last view" (and any per-grid defaults) so two grids never
        /// collide. When unset, <see cref="FrameworkElement.Name"/> is used as a fallback; if
        /// neither is set, auto-remember is disabled.
        /// </summary>
        public static readonly DependencyProperty PersistenceIdProperty =
            DependencyProperty.Register(
                nameof(PersistenceId),
                typeof(string),
                typeof(SearchDataGrid),
                new PropertyMetadata(null));

        public string PersistenceId
        {
            get => (string)GetValue(PersistenceIdProperty);
            set => SetValue(PersistenceIdProperty, value);
        }

        /// <summary>
        /// Gets or sets the default folder that the Save/Load-to-file dialogs open to. When
        /// <see cref="AllowUserPresetLocation"/> is <c>false</c>, users are confined to this folder.
        /// When unset, a computed per-user default under <c>%APPDATA%</c> is used (see
        /// <see cref="ResolveEffectivePresetDirectory"/>).
        /// </summary>
        public static readonly DependencyProperty PresetDirectoryProperty =
            DependencyProperty.Register(
                nameof(PresetDirectory),
                typeof(string),
                typeof(SearchDataGrid),
                new PropertyMetadata(null));

        public string PresetDirectory
        {
            get => (string)GetValue(PresetDirectoryProperty);
            set => SetValue(PresetDirectoryProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the user may save/load presets to an arbitrary file location of
        /// their choosing. <c>true</c> (default) presents a free file dialog; <c>false</c> confines
        /// them to <see cref="PresetDirectory"/> (the "restricted" mode).
        /// </summary>
        public static readonly DependencyProperty AllowUserPresetLocationProperty =
            DependencyProperty.Register(
                nameof(AllowUserPresetLocation),
                typeof(bool),
                typeof(SearchDataGrid),
                new PropertyMetadata(true));

        public bool AllowUserPresetLocation
        {
            get => (bool)GetValue(AllowUserPresetLocationProperty);
            set => SetValue(AllowUserPresetLocationProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the grid automatically remembers the user's last view (layout +
        /// filters) between sessions and re-applies it on open. Defaults to <c>true</c>. Requires
        /// <see cref="PersistenceId"/> (or <see cref="FrameworkElement.Name"/>) to be set.
        /// </summary>
        public static readonly DependencyProperty RememberViewStateProperty =
            DependencyProperty.Register(
                nameof(RememberViewState),
                typeof(bool),
                typeof(SearchDataGrid),
                new PropertyMetadata(true));

        public bool RememberViewState
        {
            get => (bool)GetValue(RememberViewStateProperty);
            set => SetValue(RememberViewStateProperty, value);
        }

        #endregion

        #region Path resolution

        /// <summary>
        /// The stable key used to scope this grid's persisted state: <see cref="PersistenceId"/> if
        /// set, otherwise <see cref="FrameworkElement.Name"/>, otherwise <c>null</c> (persistence off).
        /// </summary>
        internal string ResolvePersistenceKey()
        {
            if (!string.IsNullOrWhiteSpace(PersistenceId)) return PersistenceId;
            if (!string.IsNullOrWhiteSpace(Name)) return Name;
            return null;
        }

        /// <summary>
        /// Resolves the folder used for preset files and the auto-remembered last view: the
        /// dev-supplied <see cref="PresetDirectory"/> when set, otherwise a per-user default of
        /// <c>%APPDATA%\{Company}\{Product}\GridViewState</c> derived from the entry assembly.
        /// Does not create the directory.
        /// </summary>
        internal string ResolveEffectivePresetDirectory()
        {
            if (!string.IsNullOrWhiteSpace(PresetDirectory)) return PresetDirectory;

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var entry = Assembly.GetEntryAssembly();
            var company = entry?.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;
            var product = entry?.GetName().Name;

            if (string.IsNullOrWhiteSpace(company)) company = "WWControls";
            if (string.IsNullOrWhiteSpace(product)) product = "App";

            return Path.Combine(appData, company, product, "GridViewState");
        }

        #endregion
    }
}
