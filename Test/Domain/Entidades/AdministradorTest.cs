using MinimalApi.Dominio.Entidades;

namespace Test.Domain.Entidades;

[TestClass]
public class AdministradorTest
{
    [TestMethod]
    public void TestarGetSetPropriedades()
    {
        // Arrange
        var adm = new Administrador
        {
            //Act
            Id = 1,
            Email = "teste@gmail.com",
            Senha = "teste",
            Perfil = "adm"
        };

        //Assert
        Assert.AreEqual(1, adm.Id);
        Assert.AreEqual("teste@gmail.com", adm.Email);
        Assert.AreEqual("teste", adm.Senha);
        Assert.AreEqual("adm", adm.Perfil);
    }
}