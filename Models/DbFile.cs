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

        string name = null!;
        [Column("Name")]
        public string Name {
            get { return name; }
            set
            {
                name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        string path = null!;
        [Column("Path")]
        public string Path
        {
            get { return path; }
            set
            {
                path = value;
                OnPropertyChanged(nameof(Path));
            }
        }

        string extension = null!;
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
        public FileStream? BytesStream { get; set; }

        string? text;
        [JsonIgnore]
        [Column("Text")]
        public string? Text {
            get { return text; }
            set
            {
                text = value;
                OnPropertyChanged(nameof(Text));
            }
        }

        string? foundText;
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

        private bool isSelected;
        [JsonIgnore]
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
