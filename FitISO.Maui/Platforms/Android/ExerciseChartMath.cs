using System;
using System.Collections.Generic;
using System.Text;

namespace FitISO.Maui.Platforms.Android
{
    public static class ExerciseChartMath
    {
        public const double RepsInfluence = 0.01;
        public const double MaxRepsOffset = 0.5;

        public static double WeightWithRepsTiebreak(double weight, double reps) =>
            weight + Math.Min(reps * RepsInfluence, MaxRepsOffset);
    }
}
