using Microsoft.EntityFrameworkCore;
using MinimalApi.Dominio.Entidades;
using MinimalApi.Dominio.Interfaces;
using MinimalApi.DTOs;
using MinimalApi.Infraestrutura.Db;

namespace MinimalApi.Dominio.Servicos
{
    public class AdministradorServico : IAdministradorServico
    {
        private readonly DbContexto _contexto;

        public AdministradorServico(DbContexto contexto)
        {
            _contexto = contexto;
        }


        Adminstrador? IAdministradorServico.Login(LoginDTO loginDTO)
        {
           var adm = _contexto.Administradores
                .FirstOrDefault(a => a.Email == loginDTO.Email && a.Senha == loginDTO.Senha);
            return adm;
        }
    }
}
