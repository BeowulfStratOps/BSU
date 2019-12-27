namespace BSU.CoreCommon
{
    public class Uid
    {
        private static int _next = 1;
        private static readonly object Lock = new object();

        private int Id { get; }

        public Uid()
        {
            lock (Lock)
            {
                Id = _next;
                _next++;
            }
        }

        public override string ToString() => $"Id/{Id}";
    }
}