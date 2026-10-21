using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using TpMiercoles.Entidades;

namespace TpMiercoles.Repositorios
{
    public interface IUsuarioRepositorio
    {
        Task RegistrarAsync(Usuario usuario);
    }
        public class UsuarioRepositorio : IUsuarioRepositorio
    {
        private readonly ViajesDbContext _ViajesDbContext;
        public UsuarioRepositorio(ViajesDbContext viajesDbContext)
        {
            _ViajesDbContext = viajesDbContext;
        }

        public async Task RegistrarAsync(Usuario usuario)
        {
            _ViajesDbContext.Add(usuario);
            await _ViajesDbContext.SaveChangesAsync();
        }
    }
}
