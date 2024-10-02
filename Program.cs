using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MinimalApi.Dominio.DTOs;
using MinimalApi.Dominio.DTOs.Enuns;
using MinimalApi.Dominio.Entidades;
using MinimalApi.Dominio.Interfaces;
using MinimalApi.Dominio.ModelViews;
using MinimalApi.Dominio.Servicos;
using MinimalApi.DTOs;
using MinimalApi.Infraestrutura.Db;

#region Builder
var builder = WebApplication.CreateBuilder(args);
var key = builder.Configuration.GetSection("Jwt").ToString();
if (string.IsNullOrEmpty(key))
    key = "admin";

builder
    .Services.AddAuthentication(option =>
    {
        option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(option =>
    {
        option.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateIssuer = false,
            ValidateAudience = false,
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddScoped<IAdministradorServico, AdministradorServico>();
builder.Services.AddScoped<IVeiculoServico, VeiculoServico>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(option =>
{
    option.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Insira o token JWT desta maneira: Bearer {seu token}",
        }
    );
    option.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                },
                Array.Empty<string>()
            },
        }
    );
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "Minha API", Version = "v1" });
});

builder.Services.AddDbContext<DbContexto>(options =>
{
    options.UseMySql(
        builder.Configuration.GetConnectionString("MySql"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("MySql"))
    );
});

var app = builder.Build();
#endregion

#region Home
app.MapGet("/", () => Results.Json(new Home())).AllowAnonymous().WithTags("Home");
#endregion

#region  Administradores
string GerarTokenKwt(Administrador administrador)
{
    if (string.IsNullOrEmpty(key))
        return string.Empty;
    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>()
    {
        new("Email", administrador.Email),
        new("Perfil", administrador.Perfil),
    };
    var token = new JwtSecurityToken(
        claims: claims,
        expires: DateTime.Now.AddDays(1),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}
;

app.MapPost(
        "administradores/login",
        ([FromBody] LoginDTO loginDTO, IAdministradorServico administradorServico) =>
        {
            var adm = administradorServico.Login(loginDTO);
            if (adm != null)
            {
                string token = GerarTokenKwt(adm);
                return Results.Ok(
                    new AdministradorLogado
                    {
                        Email = adm.Email,
                        Perfil = adm.Perfil,
                        Token = token,
                    }
                );
            }
            else
                return Results.Unauthorized();
        }
    )
    .AllowAnonymous()
    .WithTags("Administradores");

app.MapPost(
        "administradores",
        (
            [FromBody] AdministradorDTO administradorDTO,
            IAdministradorServico administradorServico
        ) =>
        {
            var validacao = new ErrosDeValidacao { Mensagens = new List<string>() };
            if (string.IsNullOrEmpty(administradorDTO.Email))
                validacao.Mensagens.Add("Email não pode ser vazio");
            if (string.IsNullOrEmpty(administradorDTO.Senha))
                validacao.Mensagens.Add("Senha não pode ser vazia");
            if (administradorDTO.Perfil == null)
                validacao.Mensagens.Add("Perfil não pode ser vazio");
            if (validacao.Mensagens.Count > 0)
                return Results.BadRequest(validacao);

            var administrador = new Administrador
            {
                Email = administradorDTO.Email,
                Senha = administradorDTO.Senha,
                Perfil = administradorDTO.Perfil.ToString() ?? Perfil.editor.ToString(),
            };
            administradorServico.Incluir(administrador);
            return Results.Created($"/administrador/{administrador.Id}", administrador);
        }
    )
    .RequireAuthorization()
    .WithTags("Administradores");

app.MapGet(
        "/administradores",
        (int? pagina, IAdministradorServico administradorServico) =>
        {
            var administradores = administradorServico.Todos(pagina);
            return Results.Ok(administradores);
        }
    )
    .RequireAuthorization()
    .WithTags("Administradores");

app.MapGet(
        "/administradores/{id}",
        ([FromRoute] int id, IAdministradorServico administradorServico) =>
        {
            var administrador = administradorServico.BuscarPorId(id);

            if (administrador == null)
                return Results.NotFound();
            return Results.Ok(administrador);
        }
    )
    .RequireAuthorization()
    .WithTags("Administradores");
#endregion

#region Veiculos

static ErrosDeValidacao validaDTO(VeiculoDTO veiculoDTO)
{
    var validacao = new ErrosDeValidacao { Mensagens = [] };

    if (string.IsNullOrEmpty(veiculoDTO.Nome))
        validacao.Mensagens.Add("O nome não pode ser vazio");

    if (string.IsNullOrEmpty(veiculoDTO.Marca))
        validacao.Mensagens.Add("A marca não pode ser vazia");

    if (veiculoDTO.Ano < 1950)
        validacao.Mensagens.Add("O veículo é muito, aceito somente anos superiores a 1950");

    return validacao;
}

app.MapPost(
        "/veiculos",
        ([FromBody] VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
        {
            var validacao = validaDTO(veiculoDTO);
            if (validacao.Mensagens.Count > 0)
                return Results.BadRequest(validacao);

            var veiculo = new Veiculo
            {
                Nome = veiculoDTO.Nome,
                Marca = veiculoDTO.Marca,
                Ano = veiculoDTO.Ano,
            };
            veiculoServico.Incluir(veiculo);
            return Results.Created($"/veiculo/{veiculo.Id}", veiculo);
        }
    )
    .RequireAuthorization()
    .WithTags("Veículos");

app.MapGet(
        "/veiculos",
        (int? pagina, IVeiculoServico veiculoServico) =>
        {
            var veiculos = veiculoServico.Todos(pagina);
            return Results.Ok(veiculos);
        }
    )
    .RequireAuthorization()
    .WithTags("Veículos");

app.MapGet(
        "/veiculos/{id}",
        ([FromRoute] int id, IVeiculoServico veiculoServico) =>
        {
            var veiculo = veiculoServico.BuscarPorId(id);

            if (veiculo == null)
                return Results.NotFound();
            return Results.Ok(veiculo);
        }
    )
    .RequireAuthorization()
    .WithTags("Veículos");

app.MapPut(
        "/veiculos/{id}",
        ([FromRoute] int id, VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
        {
            var veiculo = veiculoServico.BuscarPorId(id);

            if (veiculo == null)
                return Results.NotFound();
            veiculo.Nome = veiculoDTO.Nome;
            veiculo.Marca = veiculoDTO.Marca;
            veiculo.Ano = veiculoDTO.Ano;
            veiculoServico.Atualizar(veiculo);
            return Results.Ok(veiculo);
        }
    )
    .RequireAuthorization()
    .WithTags("Veículos");

app.MapDelete(
        "/veiculos/{id}",
        ([FromRoute] int id, IVeiculoServico veiculoServico) =>
        {
            var veiculo = veiculoServico.BuscarPorId(id);

            if (veiculo == null)
                return Results.NotFound();

            veiculoServico.Apagar(veiculo);
            return Results.NoContent();
        }
    )
    .RequireAuthorization()
    .WithTags("Veículos");
#endregion

#region App
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.Run();
#endregion
