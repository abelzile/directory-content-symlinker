using System;


namespace DirectoryContentSymlinker.Core.Utils
{
    // Borrowed from http://blogs.msdn.com/b/pfxteam/archive/2009/02/19/9434171.aspx

    public static class ThreadSafeRandom
    {
        private static readonly Random Global = new Random();

        [ThreadStatic]
        private static Random local;

        public static int Next()
        {
            Random inst = local;

            if (inst == null)
            {
                int seed;
                lock (Global) seed = Global.Next();
                local = inst = new Random(seed);
            }

            return inst.Next();
        }
    }
}
