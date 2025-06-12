// Program.cs (Exemplo para .NET 6+)
using ConexaoMadeiraPCP.Repositories;
using ConexaoMadeiraPCP.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Registra os repositórios e o serviço.
// Scoped é um bom padrão: uma instância por requisição HTTP.
builder.Services.AddScoped<IProdutoRepository, ProdutoRepository>();
builder.Services.AddScoped<ILaminaRepository, LaminaRepository>();
builder.Services.AddScoped<PlywoodService>();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.Run();