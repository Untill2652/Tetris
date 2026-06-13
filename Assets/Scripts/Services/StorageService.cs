using System;
using System.Collections.Generic;
using UnityEngine;

namespace TetrisCourse
{
    [Serializable]
    public sealed class UserSettings
    {
        public bool soundOn = true;
        public bool musicOn = true;
        public bool contrastMode;
        public bool tutorialShown;
    }

    [Serializable]
    public sealed class AppFlags
    {
        public bool firstLaunchResolved;
        public int schemaVersion = 1;
    }

    [Serializable]
    public sealed class HighScoreEntry
    {
        public int score;
        public int lines;
        public int level;
        public int durationSec;
        public string dateIso;
    }

    [Serializable]
    public sealed class HighScoreTable
    {
        public List<HighScoreEntry> entries = new List<HighScoreEntry>();
    }

    public sealed class StorageService
    {
        private const string SettingsKey = "tetris.settings";
        private const string FlagsKey = "tetris.flags";
        private const string ScoresKey = "tetris.scores";

        public UserSettings LoadSettings()
        {
            return Load(SettingsKey, new UserSettings());
        }

        public void SaveSettings(UserSettings settings)
        {
            Save(SettingsKey, settings);
        }

        public AppFlags LoadFlags()
        {
            return Load(FlagsKey, new AppFlags());
        }

        public void SaveFlags(AppFlags flags)
        {
            Save(FlagsKey, flags);
        }

        public HighScoreTable LoadScores()
        {
            HighScoreTable table = Load(ScoresKey, new HighScoreTable());
            NormalizeScores(table);
            return table;
        }

        public void SaveScores(HighScoreTable table)
        {
            NormalizeScores(table);
            Save(ScoresKey, table);
        }

        public bool AddHighScore(HighScoreEntry entry)
        {
            HighScoreTable table = LoadScores();
            bool wasTopScore = table.entries.Count < 5 || entry.score > table.entries[table.entries.Count - 1].score;

            table.entries.Add(entry);
            SaveScores(table);
            return wasTopScore;
        }

        private T Load<T>(string key, T fallback)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                return fallback;
            }

            string json = PlayerPrefs.GetString(key);
            if (string.IsNullOrWhiteSpace(json))
            {
                return fallback;
            }

            try
            {
                T result = JsonUtility.FromJson<T>(json);
                return result == null ? fallback : result;
            }
            catch (Exception)
            {
                return fallback;
            }
        }

        private void Save<T>(string key, T value)
        {
            PlayerPrefs.SetString(key, JsonUtility.ToJson(value));
            PlayerPrefs.Save();
        }

        private void NormalizeScores(HighScoreTable table)
        {
            if (table.entries == null)
            {
                table.entries = new List<HighScoreEntry>();
            }

            table.entries.Sort((a, b) => b.score.CompareTo(a.score));

            while (table.entries.Count > 5)
            {
                table.entries.RemoveAt(table.entries.Count - 1);
            }
        }
    }
}
