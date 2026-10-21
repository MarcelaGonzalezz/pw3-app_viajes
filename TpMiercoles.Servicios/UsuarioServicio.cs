using TpMiercoles.Repositorios;
using TpMiercoles.Entidades;
using TpMiercoles.Servicios.DTOs;
using Microsoft.AspNetCore.Identity;
using AutoMapper;

namespace TpMiercoles.Servicios
{
    public interface IUsuarioServicio
    {
        public Task RegistrarAsync(UsuarioDTO usuario);
    }

    public class UsuarioServicio : IUsuarioServicio
    {

       private readonly PasswordHasher<Usuario> _hasher = new ();
        private readonly IUsuarioRepositorio _repositorio;
        private readonly IMapper _mapper;
        public UsuarioServicio(IMapper mapper, IUsuarioRepositorio repositorio)
        {
            _mapper = mapper;
            _repositorio = repositorio;
         }
        public async Task RegistrarAsync(UsuarioDTO usuario)
        {
            Usuario nuevoUsuario = _mapper.Map<Usuario>(usuario);
            string contraseniaHash = _hasher.HashPassword(nuevoUsuario, usuario.ContraseniaPlana);
            nuevoUsuario.ContraseniaHash = contraseniaHash;
            await _repositorio.RegistrarAsync(nuevoUsuario);

        }
    }
}
