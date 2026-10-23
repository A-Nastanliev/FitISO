namespace FitISO.Sharing.Session
{
    public sealed class SnapshotVersionSource
    {
        readonly Func<DateTime> _utcNow;
        readonly object _gate = new();
        long _last;

        public SnapshotVersionSource(Func<DateTime> utcNow = null)
        {
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public long Next()
        {
            lock (_gate)
            {
                _last = Math.Max(_last + 1, _utcNow().Ticks);
                return _last;
            }
        }
    }
}
