using MinimalApi.Dominio.Entidades;

namespace Test.Domain.Entidades;

[TestClass]
public class VeiculoTest
{
    [TestMethod]
    public void TestarGetSetPropriedades()
    {
        // Arrange
        var veiculo = new Veiculo
        {
            //Act
            Id = 1,
            Nome = "Uno",
            Marca = "Fiat",
            Ano = 2013
        };

        //Assert
        Assert.AreEqual(1, veiculo.Id);
        Assert.AreEqual("Uno", veiculo.Nome);
        Assert.AreEqual("Fiat", veiculo.Marca);
        Assert.AreEqual(2013, veiculo.Ano);
    }
}