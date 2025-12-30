using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;

namespace Ark.Models
{
    [Table("Files")]
    public class DbFile : IDatabaseObject, INotifyPropertyChanged
    {
        [Key]
        [Column("Id")]
        public long Id { get; set; }

        private string name = null!;
        [Column("Name")]
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        private string fullPath = null!;
        [Column("FullPath")]
        public string FullPath
        {
            get { return fullPath; }
            set
            {
                fullPath = value;
                OnPropertyChanged(nameof(FullPath));
            }
        }

        private string extension = null!;
        [Column("Extension")]
        public string Extension
        {
            get { return extension; }
            set
            {
                extension = value;
                OnPropertyChanged(nameof(Extension));
            }
        }

        [JsonIgnore]
        [Column("Bytes")]
        public FileStream BytesStream { get; set; } = null!;

        private string? text;
        [JsonIgnore]
        [Column("Text")]
        public string? Text
        {
            get { return text; }
            set
            {
                text = value;
                OnPropertyChanged(nameof(Text));
            }
        }


        private long sourceId;
        [Column("SourceId")]
        public long SourceId
        {
            get { return sourceId; }
            set
            {
                sourceId = value;
                OnPropertyChanged(nameof(SourceId));
            }
        }

        private string? foundText;
        [JsonIgnore]
        public string? FoundText
        {
            get { return foundText; }
            set
            {
                foundText = value;
                OnPropertyChanged(nameof(FoundText));
            }
        }

        private bool isChecked;
        [JsonIgnore]
        public bool IsChecked
        {
            get { return isChecked; }
            set
            {
                isChecked = value;
                OnPropertyChanged(nameof(IsChecked));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
