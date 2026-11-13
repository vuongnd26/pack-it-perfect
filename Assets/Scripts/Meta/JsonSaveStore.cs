// JSON file save: Application.persistentDataPath/progress.json, written through a temp file.
// Story: production/epics/pack-it-perfect/story-007-progression-meta.md

using System;
using System.IO;
using UnityEngine;

namespace PackItPerfect.Meta
{
    /// <summary>
    /// Stores <see cref="ProgressData"/> as JSON in one file. Writes go to a temp file first and then replace the save,
    /// so a crash mid-write leaves the previous save intact. An unreadable save is copied aside (".corrupt") and treated
    /// as no save.
    /// </summary>
    public sealed class JsonSaveStore : ISaveStore
    {
        /// <summary>File name of the save under the persistent data path.</summary>
        public const string FILE_NAME = "progress.json";

        const string TEMP_SUFFIX = ".tmp";
        const string CORRUPT_SUFFIX = ".corrupt";

        readonly string path;

        /// <summary>Default save location: <c>Application.persistentDataPath/progress.json</c>.</summary>
        public static string DefaultPath => Path.Combine(Application.persistentDataPath, FILE_NAME);

        /// <summary>File name of the Cozy Travel save. A separate file, so the 15-level build's progress.json is never rewritten.</summary>
        public const string COZY_FILE_NAME = "progress_cozy.json";
        /// <summary>
        /// Save location of the Cozy Travel build. On first use the stars of an existing progress.json are copied in
        /// (the original file is only read).
        /// </summary>
        public static string CozyPath
        {
            get
            {
                string cozy = Path.Combine(Application.persistentDataPath, COZY_FILE_NAME);
                try
                {
                    if (!File.Exists(cozy) && File.Exists(DefaultPath))
                    {
                        File.Copy(DefaultPath, cozy);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"JsonSaveStore: could not carry progress.json over to {COZY_FILE_NAME}: {e.Message}");
                }
                return cozy;
            }
        }
        /// <summary>Creates a store for the file at <paramref name="filePath"/>.</summary>
        public JsonSaveStore(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("Save path is empty.", nameof(filePath));
            }
            path = filePath;
        }

        /// <summary>Full path of the save file.</summary>
        public string FilePath => path;

        /// <inheritdoc/>
        public ProgressData Load()
        {
            if (!File.Exists(path))
            {
                return null;
            }
            try
            {
                var data = JsonUtility.FromJson<ProgressData>(File.ReadAllText(path));
                if (data == null)
                {
                    throw new FormatException("the file is empty");
                }
                return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"JsonSaveStore: could not read {path} ({e.Message}); starting with no progress.");
                TryCopyAside();
                return null;
            }
        }

        /// <inheritdoc/>
        public void Save(ProgressData data)
        {
            if (data == null)
            {
                return;
            }
            string temp = path + TEMP_SUFFIX;
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(temp, JsonUtility.ToJson(data, true));
                if (File.Exists(path))
                {
                    try
                    {
                        File.Replace(temp, path, null);
                        return;
                    }
                    catch (PlatformNotSupportedException)
                    {
                        File.Delete(path);
                    }
                }
                File.Move(temp, path);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"JsonSaveStore: could not write {path} ({e.Message}).");
            }
        }

        void TryCopyAside()
        {
            try
            {
                File.Copy(path, path + CORRUPT_SUFFIX, true);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"JsonSaveStore: could not keep a copy of the unreadable save ({e.Message}).");
            }
        }
    }
}
