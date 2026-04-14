using The_Long_Dark_Save_Editor_2.Game_data;

namespace CodexTldSaveEditor.App.Models
{
    public class CategoryFilterOption
    {
        public string Label { get; set; }
        public ItemCategory? Category { get; set; }

        public override string ToString()
        {
            return Label ?? base.ToString();
        }
    }
}
