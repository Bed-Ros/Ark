using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ark.Models
{
    [Table("Sources")]
    public class Source : IDatabaseObject, INotifyPropertyChanged
    {
        [Key]
        [Column("Id")]
        public long Id { get; set; }

        private string? machineName;
        [Column("MachineName")]
        public string? MachineName
        {
            get { return machineName; }
            set
            {
                machineName = value;
                OnPropertyChanged(nameof(MachineName));
            }
        }

        private string rootPath = null!;
        [Column("RootPath")]
        public string RootPath
        {
            get { return rootPath; }
            set
            {
                rootPath = value;
                OnPropertyChanged(nameof(RootPath));
            }
        }

        public override string ToString()
        {
            string a = string.Empty;
            if (MachineName != null) a = $"{MachineName} ";
            return $"{a}{RootPath}";
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || obj is not Source)
                return false;
            return ((Source)obj).Id == Id &&
                ((Source)obj).MachineName == MachineName &&
                ((Source)obj).RootPath == RootPath;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
