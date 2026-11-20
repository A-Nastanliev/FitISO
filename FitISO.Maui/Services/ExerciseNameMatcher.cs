using System.Text.RegularExpressions;

namespace FitISO.Maui.Services
{
    public static partial class ExerciseNameMatcher
    {
        const double MinScore = 0.5;       
        const double AmbiguityMargin = 0.05; 
        const double MinTokenSimilarity = 0.75;

        [GeneratedRegex(@"[^\p{L}\p{N}]+")]
        private static partial Regex NonAlphaNumeric();

        public static T? FindBest<T>(string spoken, IEnumerable<T> candidates, Func<T, string> nameOf) where T : class
        {
            var scored = candidates
                .Select(c => (Item: c, Score: Score(spoken, nameOf(c))))
                .OrderByDescending(x => x.Score)
                .ToList();

            if (scored.Count == 0 || scored[0].Score < MinScore) return null;

            if (scored[0].Score < 1 && scored.Count > 1 && scored[0].Score - scored[1].Score < AmbiguityMargin)
                return null;

            return scored[0].Item;
        }

        static double Score(string spoken, string candidate)
        {
            string s = Normalize(spoken);
            string c = Normalize(candidate);

            if (s.Length == 0 || c.Length == 0) return 0;
            if (s == c) return 1;

            double joined = Similarity(s.Replace(" ", ""), c.Replace(" ", ""));

            var sTokens = s.Split(' ');
            var cTokens = c.Split(' ');

            double matched = sTokens.Sum(st => cTokens.Max(ct =>
            {
                double sim = Similarity(st, ct);
                return sim >= MinTokenSimilarity ? sim : 0;
            }));

            double tokenScore = matched / Math.Max(sTokens.Length, cTokens.Length);

            return Math.Max(joined, tokenScore);
        }

        static string Normalize(string s) => NonAlphaNumeric().Replace(s.ToLowerInvariant(), " ").Trim();

        static double Similarity(string a, string b)
        {
            if (a == b) return 1;
            int max = Math.Max(a.Length, b.Length);
            return max == 0 ? 1 : 1.0 - (double)Levenshtein(a, b) / max;
        }

        static int Levenshtein(string a, string b)
        {
            var prev = new int[b.Length + 1];
            var curr = new int[b.Length + 1];

            for (int j = 0; j <= b.Length; j++) prev[j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                curr[0] = i;
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    curr[j] = Math.Min(Math.Min(curr[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
                }
                (prev, curr) = (curr, prev);
            }

            return prev[b.Length];
        }
    }
}