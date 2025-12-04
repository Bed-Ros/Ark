namespace Ark.Models
{
    public class Document
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public byte[] Bytes { get; set; } = null!;
    }
}
