using System;
using System.IO;
using System.Text;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// Disk I/O for saved grid-view files. Writes are atomic (temp file → replace) and keep a
    /// <c>.bak</c> of the previous version, so an interrupted or failed write never corrupts an
    /// existing saved view.
    /// </summary>
    internal static class GridViewStateFile
    {
        /// <summary>
        /// Writes <paramref name="json"/> to <paramref name="path"/> atomically. Creates the target
        /// directory if needed. On replace, the previous file is preserved as <c>path + ".bak"</c>.
        /// </summary>
        public static void WriteAtomic(string path, string json)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var tmp = path + ".tmp";
            File.WriteAllText(tmp, json, new UTF8Encoding(false));

            if (File.Exists(path))
            {
                var bak = path + ".bak";
                try
                {
                    // Atomic on the same volume; also rolls the old file into the backup.
                    File.Replace(tmp, path, bak, ignoreMetadataErrors: true);
                }
                catch (IOException)
                {
                    // File.Replace can fail across volumes / on some network shares — fall back to a
                    // copy-then-delete that still leaves a backup of the prior version.
                    File.Copy(path, bak, overwrite: true);
                    File.Copy(tmp, path, overwrite: true);
                    File.Delete(tmp);
                }
            }
            else
            {
                File.Move(tmp, path);
            }
        }

        /// <summary>Reads the text of a saved-view file.</summary>
        public static string ReadText(string path) => File.ReadAllText(path);
    }
}
