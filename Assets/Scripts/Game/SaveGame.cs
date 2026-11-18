using System;
using System.Collections.Generic;
using UnityEngine;

namespace HueGates.Game
{
    [Serializable]
    public class SettingsData
    {
        public bool sound = true;
        public bool music = false;
        public bool haptics = true;
        public bool assistGlow = true;
        public bool assistGlowSet;
        public bool colorblindSymbols;
    }

    [Serializable]
    public class StarRecord
    {
        public int levelId;
        public int stars;
    }

    [Serializable]
    public class SaveData
    {
        public int currentLevel = 1;
        public int highestLevelCompleted;
        public int hints = 3;
        public int undos = 3;
        public string[] ownedProducts = new string[0];
        public SettingsData settings = new SettingsData();
        public StarRecord[] stars = new StarRecord[0];
    }

    public static class SaveFile
    {
        const string Key = "huegates.save.v1";

        public static SaveData Load()
        {
            if (!PlayerPrefs.HasKey(Key)) return new SaveData();
            try
            {
                SaveData data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(Key));
                if (data == null) return new SaveData();
                if (data.settings == null) data.settings = new SettingsData();
                if (data.ownedProducts == null) data.ownedProducts = new string[0];
                if (data.stars == null) data.stars = new StarRecord[0];
                return data;
            }
            catch (Exception)
            {
                return new SaveData();
            }
        }

        public static void Write(SaveData data)
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public static int StarsFor(SaveData data, int levelId)
        {
            for (int i = 0; i < data.stars.Length; i++)
            {
                if (data.stars[i].levelId == levelId) return data.stars[i].stars;
            }
            return 0;
        }

        public static void SetStars(SaveData data, int levelId, int stars)
        {
            int best = Math.Max(StarsFor(data, levelId), stars);
            var list = new List<StarRecord>();
            bool found = false;
            for (int i = 0; i < data.stars.Length; i++)
            {
                if (data.stars[i].levelId == levelId)
                {
                    data.stars[i].stars = best;
                    found = true;
                }
                list.Add(data.stars[i]);
            }
            if (!found) list.Add(new StarRecord { levelId = levelId, stars = best });
            data.stars = list.ToArray();
        }

        public static bool Owns(SaveData data, string productId)
        {
            for (int i = 0; i < data.ownedProducts.Length; i++)
            {
                if (data.ownedProducts[i] == productId) return true;
            }
            return false;
        }

        public static void Own(SaveData data, string productId)
        {
            if (Owns(data, productId)) return;
            var list = new List<string>(data.ownedProducts);
            list.Add(productId);
            data.ownedProducts = list.ToArray();
        }
    }
}
