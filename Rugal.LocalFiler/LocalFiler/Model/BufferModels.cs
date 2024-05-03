namespace Rugal.LocalFiler.Model
{
    public class ReadBufferInfo
    {
        public long StartPosition { get; set; }
        public long EndPosition { get; set; }
        public long ReadLength => EndPosition - StartPosition;
    }
}
