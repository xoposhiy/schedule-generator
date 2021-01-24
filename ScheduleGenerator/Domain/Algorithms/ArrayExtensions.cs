using System;

namespace Domain.Algorithms
{
    public static class ArrayExtensions
    {
        public static T[] Shuffled<T>(this T[] arr)
        {
            var rnd = new Random();
            var n = arr.Length;
            var shuffledArr = new T[n];
            for (var i = 0; i < n; ++i)
            {
                var j = rnd.Next(i + 1);
                shuffledArr[i] = shuffledArr[j];
                shuffledArr[j] = arr[i];
            }
            return shuffledArr;
        }
    }
}
