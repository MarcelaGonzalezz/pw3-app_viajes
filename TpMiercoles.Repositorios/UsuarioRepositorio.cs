using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TpMiercoles.Entidades;
using TpMiercoles.Entidades.Excepciones;

namespace TpMiercoles.Repositorios
{
    public interface IUsuarioRepositorio
    {
        Task RegistrarAsync(Usuario usuario);
        Task<bool> ExisteEmailAsync(string email);
        Task<Usuario?> BuscarPorEmailAsync(string email);
    }
        public class UsuarioRepositorio : IUsuarioRepositorio
    {
        private readonly ViajesDbContext _viajesDbContext;
        public UsuarioRepositorio(ViajesDbContext viajesDbContext)
        {
            _viajesDbContext = viajesDbContext;
        }

        public async Task RegistrarAsync(Usuario usuario)
        {
            try
            {
                _viajesDbContext.Add(usuario);
                await _viajesDbContext.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sql
                                               && (sql.Number == 2601 || sql.Number == 2627))
            {
                throw new EmailExistenteException("El email ya está registrado", ex);
            }
        }

        public async Task<bool> ExisteEmailAsync(string email)
        {
            return await _viajesDbContext.Usuarios.AnyAsync(u => u.Email == email);
        }

        public async Task<Usuario?> BuscarPorEmailAsync(string email)
        {
            return await _viajesDbContext.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}
