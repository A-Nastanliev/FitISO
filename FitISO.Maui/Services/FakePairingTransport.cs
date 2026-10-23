using FitISO.Sharing.Protocol;
using FitISO.Sharing.Session;
using FitISO.Sharing.Snapshots;
using FitISO.Sharing.Transport;
#if DEBUG
namespace FitISO.Maui.Services
{
    public sealed class FakePairingTransport : IPairingTransport
    {
        const string FakePartnerName = "Fake partner";

        static readonly TimeSpan PairDelay = TimeSpan.FromSeconds(2);
        static readonly TimeSpan TickDelay = TimeSpan.FromSeconds(3);
        const int ChaosTicks = 30;

        const int MinTicks = 14;

        readonly SnapshotVersionSource _versions = new();
        CancellationTokenSource? _cts;
        bool _running;

        public TransportState State { get; private set; } = TransportState.Disconnected;
        public string? PartnerName { get; private set; }
        public bool ConnectionLost { get; private set; }

        public event EventHandler<TransportState>? StateChanged;
        public event EventHandler<byte[]>? PayloadReceived;
        public event EventHandler<PairingRequest>? PairingRequested;

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_running) return Task.CompletedTask;

            _running = true;
            ConnectionLost = false;
            PartnerName = null;
            _cts = new CancellationTokenSource();

            SetState(TransportState.Connecting);
            _ = PairAsync(_cts.Token);
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            _running = false;
            _cts?.Cancel();
            _cts = null;

            PartnerName = null;
            ConnectionLost = false;

            SetState(TransportState.Disconnected);
            return Task.CompletedTask;
        }

        public Task ReconnectAsync() => Task.CompletedTask;

        public Task SendAsync(byte[] payload, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        async Task PairAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(PairDelay, token);

                var handler = PairingRequested;
                if (handler is null) return;

                handler(this, new PairingRequest(
                    FakePartnerName,
                    "1234",
                    Accept: () => Connect(token),
                    Reject: () => { }));
            }
            catch (OperationCanceledException)
            {
            }
        }

        void Connect(CancellationToken token)
        {
            if (token.IsCancellationRequested) return;

            PartnerName = FakePartnerName;
            SetState(TransportState.Connected);
            _ = RunPartnerAsync(token);
        }

        async Task RunPartnerAsync(CancellationToken token)
        {
            try
            {
                var started = DateTime.UtcNow;
                var workout = new FakeWorkout();

                SendSnapshot(workout, started);

                for (int tick = 1; ; tick++)
                {
                    await Task.Delay(TickDelay, token);

                    workout.Step(randomEdits: tick <= ChaosTicks);
                    SendSnapshot(workout, started);

                    if (tick >= MinTicks && workout.IsFinished)
                        break;
                }

                await Task.Delay(TickDelay, token);
                PayloadReceived?.Invoke(this,
                    WorkoutSnapshotCodec.EncodeSessionEnded(_versions.Next(), DateTime.UtcNow));
            }
            catch (OperationCanceledException)
            {
            }
        }

        void SendSnapshot(FakeWorkout workout, DateTime startedUtc) =>
            PayloadReceived?.Invoke(this,
                WorkoutSnapshotCodec.Encode(workout.ToSnapshot(_versions.Next(), startedUtc), DateTime.UtcNow));

        void SetState(TransportState state)
        {
            if (State == state) return;
            State = state;
            StateChanged?.Invoke(this, state);
        }

        sealed class FakeSet
        {
            public double? Weight { get; set; }
            public double? Reps { get; set; }
            public bool IsComplete { get; set; }
        }

        sealed class FakeExercise
        {
            public FakeExercise(string name, double plannedWeight, int sets)
            {
                Name = name;
                PlannedWeight = plannedWeight;
                for (int i = 0; i < sets; i++)
                    Sets.Add(new FakeSet());
            }

            public string Name { get; }
            public double PlannedWeight { get; }
            public List<FakeSet> Sets { get; } = new();
        }

        sealed class FakeWorkout
        {
            const int MaxSetsPerExercise = 6;

            static readonly (string Name, double Weight)[] StartingExercises =
            {
                ("Bench press", 60),
                ("Barbell row", 50),
                ("Biceps curl", 20)
            };

            static readonly (string Name, double Weight)[] ExtraExercises =
            {
                ("Overhead press", 40),
                ("Incline dumbbell press", 24),
                ("Lateral raise", 8),
                ("Triceps pushdown", 30),
                ("Cable fly", 15),
                ("Face pull", 25)
            };

            readonly Random _rng = new();
            readonly List<FakeExercise> _exercises =
                StartingExercises.Select(e => new FakeExercise(e.Name, e.Weight, 3)).ToList();

            DateTime? _restStartUtc;

            public bool IsFinished =>
                _exercises.Any(e => e.Sets.Count > 0)
                && _exercises.All(e => e.Sets.All(s => s.IsComplete));

            bool HasIncompleteSet => _exercises.Any(e => e.Sets.Any(s => !s.IsComplete));

            public void Step(bool randomEdits)
            {
                bool applied = false;

                if (randomEdits)
                {
                    int roll = _rng.Next(100);

                    if (roll < 45)
                        applied = CompleteNextSet();
                    else if (roll < 57)
                        applied = AddSet();
                    else if (roll < 69)
                        applied = RemoveSet();
                    else if (roll < 79)
                        applied = AddExercise();
                    else
                        applied = EditSet();
                }

                if (applied) return;

                if (HasIncompleteSet)
                    CompleteNextSet();
                else if (randomEdits)
                    AddSet();
            }

            public WorkoutSnapshot ToSnapshot(long version, DateTime startedUtc) =>
                new(version, "Fake push day", startedUtc, null,
                    _exercises
                        .Select(e => new ExerciseSnapshot(
                            e.Name,
                            e.Sets.Select(s => new SetSnapshot(s.Weight, s.Reps, s.IsComplete)).ToList()))
                        .ToList(),
                    new RestStopwatchSnapshot(IsFinished ? null : _restStartUtc, false));

            bool CompleteNextSet()
            {
                foreach (var exercise in _exercises)
                {
                    var set = exercise.Sets.FirstOrDefault(s => !s.IsComplete);
                    if (set is null) continue;

                    set.Weight ??= exercise.PlannedWeight;
                    set.Reps = 6 + _rng.Next(5);
                    set.IsComplete = true;
                    _restStartUtc = DateTime.UtcNow;
                    return true;
                }

                return false;
            }

            bool AddSet()
            {
                var candidates = _exercises.Where(e => e.Sets.Count < MaxSetsPerExercise).ToList();
                if (candidates.Count == 0) return false;

                Pick(candidates).Sets.Add(new FakeSet());
                return true;
            }

            bool RemoveSet()
            {
                var candidates = _exercises.Where(e => e.Sets.Count > 1).ToList();
                if (candidates.Count == 0) return false;

                var exercise = Pick(candidates);

                int index = exercise.Sets.FindLastIndex(s => !s.IsComplete);
                if (index < 0 || _rng.Next(5) == 0)
                    index = _rng.Next(exercise.Sets.Count);

                exercise.Sets.RemoveAt(index);
                return true;
            }

            bool AddExercise()
            {
                var used = _exercises.Select(e => e.Name).ToHashSet();
                var candidates = ExtraExercises.Where(e => !used.Contains(e.Name)).ToList();
                if (candidates.Count == 0) return false;

                var (name, weight) = Pick(candidates);
                _exercises.Add(new FakeExercise(name, weight, 2 + _rng.Next(2)));
                return true;
            }

            bool EditSet()
            {
                bool editFinished = _rng.Next(10) < 7;

                if (editFinished)
                {
                    var finished = _exercises
                        .SelectMany(e => e.Sets.Select(s => (Exercise: e, Set: s)))
                        .Where(x => x.Set.IsComplete)
                        .ToList();

                    if (finished.Count > 0)
                    {
                        var (_, set) = Pick(finished);

                        if (_rng.Next(2) == 0)
                            set.Weight = Math.Max(2.5, (set.Weight ?? 0) + (_rng.Next(2) == 0 ? 2.5 : -2.5));
                        else
                            set.Reps = Math.Max(1, (set.Reps ?? 0) + (_rng.Next(2) == 0 ? 1 : -1));

                        return true;
                    }
                }

                var empty = _exercises
                    .SelectMany(e => e.Sets.Select(s => (Exercise: e, Set: s)))
                    .Where(x => !x.Set.IsComplete && x.Set.Weight is null)
                    .ToList();

                if (empty.Count == 0) return false;

                var (exercise, emptySet) = Pick(empty);
                emptySet.Weight = exercise.PlannedWeight;
                return true;
            }

            T Pick<T>(IReadOnlyList<T> items) => items[_rng.Next(items.Count)];
        }
    }
}
#endif