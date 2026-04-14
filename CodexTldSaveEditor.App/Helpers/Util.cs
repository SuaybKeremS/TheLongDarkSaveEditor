using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using The_Long_Dark_Save_Editor_2.Game_data;

namespace The_Long_Dark_Save_Editor_2.Helpers
{

    public class SlotDataDisplayNameProxy
    {
        public string m_DisplayName { get; set; }
    }

    public static class Util
    {
        public static T DeserializeObject<T>(string json) where T : class
        {

            if (json == null)
                return null;

            return JsonConvert.DeserializeObject<T>(json);
        }

        public static T DeserializeObjectOrDefault<T>(string json) where T : class, new()
        {
            if (json == null)
                return new T();
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static string SerializeObject(object o)
        {
            if (o == null)
                return null;
            return JsonConvert.SerializeObject(o);
        }

        public static ObservableCollection<EnumerationMember> GetSaveFiles(string folder)
        {

            Regex reg = new Regex("^(ep[0-9])?(sandbox|challenge|story|checkpoint|autosave|quicksave|relentless)[0-9]*$");
            var saves = new List<string>();
            if (Directory.Exists(folder))
                saves.AddRange((from f in Directory.GetFiles(folder) orderby new FileInfo(f).LastWriteTime descending where reg.IsMatch(Path.GetFileName(f)) select f).ToList<string>());

            var result = new ObservableCollection<EnumerationMember>();
            foreach (string saveFile in saves)
            {
                try
                {
                    var member = CreateSaveEnumerationMember(saveFile, Path.GetFileName(saveFile));
                    result.Add(member);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    continue;
                }
            }

            return result;
        }

        private static EnumerationMember CreateSaveEnumerationMember(string file, string name)
        {
            var member = new EnumerationMember();
            member.Value = file;

            var slotJson = EncryptString.Decompress(File.ReadAllBytes(file));
            var slotData = JsonConvert.DeserializeObject<SlotDataDisplayNameProxy>(slotJson);

            member.Description = slotData.m_DisplayName + " (" + name + ")";

            return member;
        }

        public static string GetLocalPath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }
    }
}
