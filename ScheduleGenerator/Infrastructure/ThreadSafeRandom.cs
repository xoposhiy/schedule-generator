using System;

namespace Infrastructure
{
    public class ThreadSafeRandom : Random
    {
        private static readonly Random Global = new(42);

        // ReSharper disable once InconsistentNaming
        [ThreadStatic] private static Random? _local;

        public override int Next()
        {
            if (_local == null)
            {
                int seed;
                lock (Global)
                {
                    seed = Global.Next();
                }

                _local = new(seed);
            }

            return _local.Next();
        }
    }
}