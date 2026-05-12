using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF.Commands
{
    /// <summary>
    /// Collection of placeholder context menu commands for SearchDataGrid
    /// All commands are placeholder implementations that log the action and can be implemented later
    /// </summary>
    public static partial class ContextMenuCommands
    {
        #region Helper Methods

        /// <summary>
        /// Extracts the binding path from a DataGridColumn for context menu operations.
        /// Prefers the <see cref="GridColumn.FieldName"/> from the parent grid's descriptor when
        /// available — that is the canonical source identifier and resolves correctly through
        /// <see cref="System.ComponentModel.TypeDescriptor"/> for both POCO and DataRowView items,
        /// regardless of the binding-path syntax (dot path vs. indexer) the WPF column was
        /// generated with.
        /// </summary>
        private static string GetBindingPath(DataGridColumn column, SearchDataGrid grid = null)
        {
            if (column == null) return null;

            // GridColumn.FieldName is the descriptor-level identifier and matches the
            // PropertyDescriptor name returned by TypeDescriptor for both POCO and DataRowView
            // items. WPF Binding paths can use indexer syntax like "[ColumnName]" for DataTable
            // sources, which TypeDescriptor lookup can't dereference directly — using FieldName
            // sidesteps that.
            if (grid != null)
            {
                var descriptor = grid.FindGridColumnDescriptor(column);
                if (descriptor != null && !string.IsNullOrEmpty(descriptor.FieldName))
                    return descriptor.FieldName;
            }

            // ClipboardContentBinding is the universal "value for copy" binding — set explicitly
            // by GridColumn when generating a DataGridTemplateColumn from EditSettings, and
            // defaults to the bound Binding on DataGridBoundColumn.
            if (column.ClipboardContentBinding is Binding clipboardBinding)
            {
                var clipboardPath = NormalizePath(clipboardBinding.Path?.Path);
                if (!string.IsNullOrEmpty(clipboardPath)) return clipboardPath;
            }

            if (column is DataGridBoundColumn boundColumn && boundColumn.Binding is Binding binding)
            {
                var bindingPath = NormalizePath(binding.Path?.Path);
                if (!string.IsNullOrEmpty(bindingPath)) return bindingPath;
            }

            return column.SortMemberPath;
        }

        /// <summary>
        /// Strips outer indexer brackets from a binding path. WPF auto-generates DataTable column
        /// bindings as <c>[ColumnName]</c>; <see cref="ReflectionHelper.GetPropValue"/> needs the
        /// bare column name to resolve through <see cref="System.ComponentModel.TypeDescriptor"/>.
        /// </summary>
        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            if (path.Length >= 2 && path[0] == '[' && path[path.Length - 1] == ']')
                return path.Substring(1, path.Length - 2);
            return path;
        }

        #endregion
    }
}
