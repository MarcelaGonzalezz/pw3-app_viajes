using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using TpMiercoles.Entidades;

namespace TpMiercoles.Tests.Helpers
{
    public sealed class DbContextMockFactory
    {
        public Mock<ViajesDbContext> Contexto { get; }
        public Mock<DbSet<Usuario>> Usuarios { get; }

        public DbContextMockFactory(IEnumerable<Usuario>? usuariosIniciales = null)
        {
            var usuarios = usuariosIniciales?.ToList() ?? [];
            Usuarios = usuarios.BuildMockDbSet();

            Contexto = new Mock<ViajesDbContext>(
                new DbContextOptionsBuilder<ViajesDbContext>().Options);
            Contexto.Object.Usuarios = Usuarios.Object;
            Contexto
                .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
        }
    }
}
