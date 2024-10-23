using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MinimalApi.Dominio.Entidades;
using MinimalApi.Dominio.Servicos;
using MinimalApi.Infraestrutura.Db;

namespace Test.Domain.Servicos;

[TestClass]
public class AdministradorServicoTest
{
    private DbContexto CriarContextoDeTeste()
    {
        var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var path = Path.GetFullPath(Path.Combine(assemblyPath ?? "", "..", "..", ".."));
        var builder = new ConfigurationBuilder()
            .SetBasePath(path ?? Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables();

        var configuration = builder.Build();

        return new DbContexto(configuration);
    }

    [TestMethod]
    public async void TestandoSalvarAdministrador()
    {
        // Arrange
        var context = CriarContextoDeTeste();
        await context.Database.ExecuteSqlRawAsync("TRUNCATE administradores");

        var adm = new Administrador
        {
            Id = 1,
            Email = "teste@gmail.com",
            Senha = "teste",
            Perfil = "adm"
        };

        var administradorServico = new AdministradorServico(context);

        //Act
        administradorServico.Incluir(adm);
        //Assert
        Assert.AreEqual(1, administradorServico.Todos(1).Count());
    }

    [TestMethod]
    public async void TestandoBuscarPorId()
    {
        // Arrange
        var context = CriarContextoDeTeste();
        await context.Database.ExecuteSqlRawAsync("TRUNCATE administradores");

        var adm = new Administrador
        {
            Id = 1,
            Email = "teste@gmail.com",
            Senha = "teste",
            Perfil = "adm"
        };

        var administradorServico = new AdministradorServico(context);

        //Act
        administradorServico.Incluir(adm);
        administradorServico.BuscarPorId(adm.Id);
        //Assert
        Assert.AreEqual(1, adm.Id);
    }
}
