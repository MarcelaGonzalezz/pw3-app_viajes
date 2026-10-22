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
        public Task<UsuarioSesionDTO?> LoginAsync(UsuarioLoginDTO usuario);
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

        public async Task<UsuarioSesionDTO?> LoginAsync(UsuarioLoginDTO usuarioDTO)
        {
        
            Usuario? usuario = await _repositorio.BuscarPorEmailAsync(usuarioDTO.Email);
            if (usuario is null)
            {
                return null;
            }
            PasswordVerificationResult resultado = this.ValidarContrasenia(usuario, usuarioDTO.ContraseniaPlana);
            if (resultado == PasswordVerificationResult.Failed)
                return null;

            return _mapper.Map<UsuarioSesionDTO>(usuario);
        }

        private PasswordVerificationResult ValidarContrasenia(Usuario usuario, string contraseniaPlana)
        {
            return _hasher.VerifyHashedPassword(usuario, usuario.ContraseniaHash, contraseniaPlana);
        }
    }
}
