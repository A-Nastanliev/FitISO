using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FitISO.Maui.Models;

namespace FitISO.Maui.Services
{
    public static partial class VoiceCommandParser
    {
        const int Small = 0, Teen = 1, TensKind = 2, Hundred = 3;

        static readonly Dictionary<string, int> Units = new()
        {
            ["zero"] = 0,
            ["one"] = 1,
            ["two"] = 2,
            ["three"] = 3,
            ["four"] = 4,
            ["five"] = 5,
            ["six"] = 6,
            ["seven"] = 7,
            ["eight"] = 8,
            ["nine"] = 9,
            ["ten"] = 10,
            ["eleven"] = 11,
            ["twelve"] = 12,
            ["thirteen"] = 13,
            ["fourteen"] = 14,
            ["fifteen"] = 15,
            ["sixteen"] = 16,
            ["seventeen"] = 17,
            ["eighteen"] = 18,
            ["nineteen"] = 19
        };

        static readonly Dictionary<string, int> Tens = new()
        {
            ["twenty"] = 20,
            ["thirty"] = 30,
            ["forty"] = 40,
            ["fifty"] = 50,
            ["sixty"] = 60,
            ["seventy"] = 70,
            ["eighty"] = 80,
            ["ninety"] = 90
        };

        static readonly HashSet<string> RepsWords = ["rep", "reps", "repetition", "repetitions"];
        static readonly HashSet<string> WeightWords = ["kg", "kgs", "kilo", "kilos", "kilogram", "kilograms"];
        static readonly HashSet<string> FillerWords = ["for", "x", "by", "at", "and", "times", "with", "of", "weight"];
        static readonly HashSet<string> LeadingWords =
            ["log", "add", "record", "enter", "now", "then", "next", "okay", "ok", "and", "set",
             "a", "another", "same", "again", "for", "at", "with"];

        [GeneratedRegex(@"(?<=\d),(?=\d)")]
        private static partial Regex DecimalComma();

        [GeneratedRegex(@"(?<=\d)(?=\p{L})|(?<=\p{L})(?=\d)")]
        private static partial Regex LetterDigitBoundary();

        [GeneratedRegex(@"[^\p{L}\p{N}. ]")]
        private static partial Regex Punctuation();

        public static VoiceSetCommand? Parse(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            var tokens = Tokenize(text);

            int firstNumber = tokens.FindIndex(IsNumber);
            if (firstNumber < 0) return null;
            var nameTokens = tokens.Take(firstNumber).ToList();
            while (nameTokens.Count > 0 && LeadingWords.Contains(nameTokens[0]))
                nameTokens.RemoveAt(0);
            while (nameTokens.Count > 0 && FillerWords.Contains(nameTokens[^1]))
                nameTokens.RemoveAt(nameTokens.Count - 1);

            string? exerciseName = nameTokens.Count == 0 ? null : string.Join(' ', nameTokens);

            double? weight = null;
            int? reps = null;
            var loose = new List<double>();

            for (int i = firstNumber; i < tokens.Count; i++)
            {
                if (!TryNumber(tokens[i], out double n)) continue;

                string? next = i + 1 < tokens.Count ? tokens[i + 1] : null;

                if (next is not null && RepsWords.Contains(next)) reps = (int)Math.Round(n);
                else if (next is not null && WeightWords.Contains(next)) weight = n;
                else loose.Add(n);
            }

            if (loose.Count >= 2)
            {
                weight ??= loose[0];
                reps ??= (int)Math.Round(loose[1]);
            }
            else if (loose.Count == 1)
            {
                if (reps is null) reps = (int)Math.Round(loose[0]);
                else weight ??= loose[0];
            }

            if (reps is not > 0 or > 200) return null;
            if (weight is < 0 or > 1000) return null;

            return new VoiceSetCommand(exerciseName, weight, reps.Value);
        }


        static List<string> Tokenize(string text)
        {
            text = text.ToLowerInvariant();
            text = DecimalComma().Replace(text, ".");        
            text = LetterDigitBoundary().Replace(text, " ");  
            text = text.Replace('-', ' ');                    
            text = Punctuation().Replace(text, " ");

            var raw = text
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim('.'))
                .Where(t => t.Length > 0)
                .ToList();

            return MergeNumbers(raw);
        }

        static List<string> MergeNumbers(List<string> tokens)
        {
            var merged = new List<string>();

            for (int i = 0; i < tokens.Count; i++)
            {
                string t = tokens[i];

                if (NumberKind(t) is not null)
                {
                    int j = i, value = 0, prev = -1;

                    while (j < tokens.Count)
                    {
                        if (prev == Hundred && tokens[j] == "and" && j + 1 < tokens.Count
                            && NumberKind(tokens[j + 1]) is int following && CanFollow(prev, following))
                        {
                            j++;
                            continue;
                        }

                        if (NumberKind(tokens[j]) is not int kind || !CanFollow(prev, kind))
                            break;

                        string w = tokens[j];
                        value = kind == Hundred
                            ? Math.Max(value, 1) * 100
                            : value + (Units.TryGetValue(w, out int u) ? u : Tens[w]);
                        prev = kind;
                        j++;
                    }

                    string number = value.ToString(CultureInfo.InvariantCulture);

                    if (j + 2 < tokens.Count && tokens[j] == "and" && tokens[j + 1] == "a" && tokens[j + 2] == "half")
                    {
                        number += ".5";
                        j += 3;
                    }

                    merged.Add(number);
                    i = j - 1;
                }
                else if (t == "point" && merged.Count > 0 && IsInteger(merged[^1]))
                {
                    var dec = new StringBuilder();
                    int j = i + 1;

                    while (j < tokens.Count && SingleDigit(tokens[j]) is char d)
                    {
                        dec.Append(d);
                        j++;
                    }

                    if (dec.Length == 0 && j < tokens.Count && IsInteger(tokens[j]))
                    {
                        dec.Append(tokens[j]);
                        j++;
                    }

                    if (dec.Length > 0)
                    {
                        merged[^1] += "." + dec;
                        i = j - 1;
                    }
                    else merged.Add(t);
                }
                else merged.Add(t);
            }

            return merged;
        }

        static int? NumberKind(string token)
        {
            if (token == "hundred") return Hundred;
            if (Tens.ContainsKey(token)) return TensKind;
            if (Units.TryGetValue(token, out int u)) return u < 10 ? Small : Teen;
            return null;
        }

        static bool CanFollow(int prev, int next) => prev switch
        {
            -1 => true,
            Small or Teen => next == Hundred,
            TensKind => next == Small,
            Hundred => next is Small or Teen or TensKind,
            _ => false
        };

        static char? SingleDigit(string token)
        {
            if (token.Length == 1 && char.IsDigit(token[0])) return token[0];
            if (Units.TryGetValue(token, out int u) && u < 10) return (char)('0' + u);
            return null;
        }

        static bool IsInteger(string s) => s.Length > 0 && s.All(char.IsDigit);

        static bool IsNumber(string s) => TryNumber(s, out _);

        static bool TryNumber(string s, out double value)
        {
            value = 0;
            return s.Length > 0 && char.IsDigit(s[0])
                && double.TryParse(s, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
        }
    }
}