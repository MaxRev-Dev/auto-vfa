using System;

namespace AutoVFA.Misc
{
    /// <summary>
    /// From <see href="http://mihkeltt.blogspot.com/2009/04/dameraulevenshtein-distance.html"/>
    /// </summary>
    public static class DamerauLevenshteinDistance
    {
        /// <summary>
        /// Computes difference between two strings  
        /// </summary> 
        public static int Compute(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1))
            {
                return !string.IsNullOrEmpty(s2) ? s2.Length : 0;
            }

            if (string.IsNullOrEmpty(s2))
            {
                return !string.IsNullOrEmpty(s1) ? s1.Length : 0;
            }

            var length1 = s1.Length;
            var length2 = s2.Length;

            var d = new int[length1 + 1, length2 + 1];

            int cost, del, ins, sub;

            for (var i = 0; i <= d.GetUpperBound(0); i++)
                d[i, 0] = i;

            for (var i = 0; i <= d.GetUpperBound(1); i++)
                d[0, i] = i;

            for (var i = 1; i <= d.GetUpperBound(0); i++)
            {
                for (var j = 1; j <= d.GetUpperBound(1); j++)
                {
                    cost = s1[i - 1] == s2[j - 1] ? 0 : 1;

                    del = d[i - 1, j] + 1;
                    ins = d[i, j - 1] + 1;
                    sub = d[i - 1, j - 1] + cost;

                    d[i, j] = Math.Min(del, Math.Min(ins, sub));

                    if (i > 1 && j > 1 && s1[i - 1] == s2[j - 2] && s1[i - 2] == s2[j - 1])
                        d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + cost);
                }
            }

            return d[d.GetUpperBound(0), d.GetUpperBound(1)];
        }
    }
}