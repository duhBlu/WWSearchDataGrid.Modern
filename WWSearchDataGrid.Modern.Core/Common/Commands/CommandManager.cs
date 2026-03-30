using System;

namespace WWSearchDataGrid.Modern.Core
{
    internal static class CommandManager
    {
        private static readonly object _eventLock = new object();
        private static EventHandler _requerySuggested;

        public static event EventHandler RequerySuggested
        {
            add { lock (_eventLock) { _requerySuggested += value; } }
            remove { lock (_eventLock) { _requerySuggested -= value; } }
        }

        public static void InvalidateRequerySuggested()
        {
            EventHandler handler;
            lock (_eventLock) { handler = _requerySuggested; }
            handler?.Invoke(null, EventArgs.Empty);
        }
    }
}
