using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ark.Models
{
    [Table("Files")]
    public class DbFile : IDatabaseObject, INotifyPropertyChanged
    {
        long id;
        [Key]
        [Column("Id")]
        public long Id {
            get { return id; }
            set
            {
                id = value;
            }
        }

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

        byte[] bytes = null!;
        [JsonIgnore]
        [Column("Bytes")]
        public byte[] Bytes {
            get { return bytes; }
            set
            {
                bytes = value;
                OnPropertyChanged(nameof(Bytes));
            }
        } 

        string? text;
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
                

        public object Keys() => new { Id };
        public static string TableName() => "Files";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
