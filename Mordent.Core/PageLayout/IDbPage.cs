namespace Mordent.Core
{
    public interface IDbPage
    {
        public ref DbPageHeader Header { get; }
    }
}