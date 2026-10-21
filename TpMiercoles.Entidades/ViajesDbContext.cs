using Microsoft.EntityFrameworkCore;


namespace TpMiercoles.Entidades
{
    public class ViajesDbContext : DbContext
    {
        public ViajesDbContext(DbContextOptions<ViajesDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Ruta>()
                .HasOne(r => r.DireccionOrigen)
                .WithMany()
                .HasForeignKey(r => r.DireccionOrigenId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ruta>()
                .HasOne(r => r.DireccionDestino)
                .WithMany()
                .HasForeignKey(r => r.DireccionDestinoId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Envio>()
               .HasOne(e => e.DireccionRetiro)
               .WithMany()
               .HasForeignKey(e => e.DireccionRetiroId)
               .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Envio>()
                .HasOne(e => e.DireccionEntrega)
                .WithMany()
                .HasForeignKey(e => e.DireccionEntregaId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Viaje>()
              .HasOne(v => v.DireccionOrigen)
              .WithMany()
              .HasForeignKey(v => v.DireccionOrigenId)
              .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Viaje>()
                .HasOne(v => v.DireccionDestino)
                .WithMany()
                .HasForeignKey(v => v.DireccionDestinoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Asignacion>()
                .HasOne(a => a.Viaje)
                .WithMany(v => v.Asignaciones)
                .HasForeignKey(a => a.ViajeId)
                .OnDelete(DeleteBehavior.Restrict); 
        }
    }

    
}
