using TpMiercoles.Repositorios;
using TpMiercoles.Entidades;
using TpMiercoles.Entidades.Excepciones;
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

       private readonly IPasswordHasher<Usuario> _hasher;
        private readonly IUsuarioRepositorio _repositorio;
        private readonly IMapper _mapper;
        public UsuarioServicio(IMapper mapper, IUsuarioRepositorio repositorio, IPasswordHasher<Usuario> hasher)
        {
            _mapper = mapper;
            _repositorio = repositorio;
            _hasher = hasher;
         }
        public async Task RegistrarAsync(UsuarioDTO usuario)
        {
            if (await _repositorio.ExisteEmailAsync(usuario.Email))
            {
                throw new EmailExistenteException("El email ya está registrado");
            }
            Usuario nuevoUsuario = _mapper.Map<Usuario>(usuario);
            string contraseniaHash = _hasher.HashPassword(nuevoUsuario, usuario.ContraseniaPlana);
            nuevoUsuario.ContraseniaHash = contraseniaHash;
            await _repositorio.RegistrarAsync(nuevoUsuario);
        }
    }
}
