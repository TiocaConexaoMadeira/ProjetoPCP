// Program.cs (Exemplo para .NET 6+)
using ConexaoMadeiraPCP.Repositories;
using ConexaoMadeiraPCP.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Xml.Linq;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Registra os repositórios e o serviço.
// Scoped é um bom padrão: uma instância por requisição HTTP.
builder.Services.AddScoped<IProdutoRepository, ProdutoRepository>();
builder.Services.AddScoped<ILaminaRepository, LaminaRepository>();
builder.Services.AddScoped<PlywoodService>();

builder.Services.AddRouting();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "ConexaoMadeiraInventarioWebApi",
        Description = "Backend da aplicaçãoo Conexão Madeira inventário",
        TermsOfService = new Uri("https://www.conexaomadeira.com//terms"),
        Contact = new OpenApiContact
        {
            Name = "Conexão Madeira",
            Email = "contato@conexaomadeira.com.br",
            Url = new Uri("https://www.conexaomadeira.com/"),
        },
        License = new OpenApiLicense
        {
            Name = "Usar sobre LICX",
            Url = new Uri("https://www.conexaomadeira.com//license"),
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (!File.Exists(xmlPath))
    {
        //File.Create(xmlPath);
        var xmlStructure = new XElement("RootElement");
        xmlStructure.Save(xmlPath);
    }


    c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

app.UseSwagger();

app.UseSwaggerUI(opt =>
{
    opt.SwaggerEndpoint("/swagger/v1/swagger.json", "ForSellInventarioWebAPI V1");
});

app.UseRouting();

app.UseCors(opt =>
{
    opt.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
});

app.UseEndpoints(endpoints =>
{
    // Everything that doesn't explicitly allow anonymous users requires authentication.
    endpoints.MapControllers();
});

app.MapControllers();

app.Run();