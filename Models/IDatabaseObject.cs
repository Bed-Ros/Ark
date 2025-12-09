namespace Ark.Models
{
    public interface IDatabaseObject
    {
        public abstract object Keys();
        public static string TableName() => throw new System.NotImplementedException();
    }
}
