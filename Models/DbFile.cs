using System.ComponentModel;

namespace Ark.Models
{
    public class DbFile : IDatabaseObject, INotifyPropertyChanged
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;        
        public byte[] Bytes { get; set; } = null!;
        public string? Text { get; set; }

        private bool isSelected;
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
                
        public object Keys() => new { Id };
        public static string TableName() => "Files";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
