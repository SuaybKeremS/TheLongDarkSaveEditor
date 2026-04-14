using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using The_Long_Dark_Save_Editor_2.Game_data;
using The_Long_Dark_Save_Editor_2.Helpers;
using The_Long_Dark_Save_Editor_2.Serialization;

namespace The_Long_Dark_Save_Editor_2
{
    public class Profile
    {
        public string path;

        private DynamicSerializable<ProfileState> dynamicState;
        public ProfileState State { get { return dynamicState.Obj; } }
        public string RawJson => dynamicState.Serialize();

        public Profile(string path)
        {
            this.path = path;

            var json = EncryptString.Decompress(File.ReadAllBytes(path));

            // m_StatsDictionary is invalid json (unquoted keys), so fix it
            json = Regex.Replace(json, @"(\\*\""m_StatsDictionary\\*\"":\{)((?:[-0-9\.]+:\\*\""[-+0-9eE\.]+\\*\""\,?)+)(\})", delegate (Match match)
            {
                string jsonSubStr = Regex.Replace(match.Groups[2].ToString(), @"([-0-9]+):(\\*\"")", delegate (Match matchSub)
                {
                    var escapeStr = matchSub.Groups[2].ToString();
                    return escapeStr + matchSub.Groups[1].ToString() + escapeStr + @":" + escapeStr;
                });
                return match.Groups[1].ToString() + jsonSubStr + match.Groups[3].ToString();
            });

            dynamicState = new DynamicSerializable<ProfileState>(json);
        }

        public void ReplaceRawJson(string json)
        {
            dynamicState = new DynamicSerializable<ProfileState>(json);
        }

        public void Save()
        {
            Backup();

            string json = dynamicState.Serialize();

            // Game cannot read valid json for m_StatsDictionary so remove quotes from keys
            json = Regex.Replace(json, @"(\\*\""m_StatsDictionary\\*\"":\{)((?:\\*\""[-0-9\.]+\\*\"":\\*\""[-+0-9eE\.]+\\*\""\,?)+)(\})", delegate (Match match)
            {
                string jsonSubStr = Regex.Replace(match.Groups[2].ToString(), @"\\*\""([-0-9]+)\\*\"":", delegate (Match matchSub)
                {
                    return matchSub.Groups[1].ToString() + @":";
                });
                return match.Groups[1].ToString() + jsonSubStr + match.Groups[3].ToString();
            });

            File.WriteAllBytes(path, EncryptString.Compress(json));
        }

        private void Backup()
        {
            var directory = Path.GetDirectoryName(path);
            if (directory == null)
                return;

            var backupDirectory = Path.Combine(directory, "backups");
            Directory.CreateDirectory(backupDirectory);

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss", CultureInfo.InvariantCulture);
            var backupPath = Path.Combine(backupDirectory, $"{timestamp}-{Path.GetFileName(path)}.backup");
            var index = 1;
            while (File.Exists(backupPath))
            {
                backupPath = Path.Combine(backupDirectory, $"{timestamp}-{Path.GetFileName(path)}({index++}).backup");
            }

            File.Copy(path, backupPath);
        }
    }
}
