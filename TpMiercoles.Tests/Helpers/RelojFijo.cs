namespace TpMiercoles.Tests.Helpers
{
    public sealed class RelojFijo : TimeProvider
    {
        private DateTimeOffset _ahora;

        public RelojFijo(DateTimeOffset ahora) => _ahora = ahora;

        public void Fijar(DateTimeOffset ahora) => _ahora = ahora;

        public override DateTimeOffset GetUtcNow() => _ahora;
    }
}
