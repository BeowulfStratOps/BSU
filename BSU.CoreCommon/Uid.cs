namespace BSU.CoreCommon
{
    public class Uid
    {
        private static int _next = 1;
        private static readonly object Lock = new object();

        public int Id { get; private set; }

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
