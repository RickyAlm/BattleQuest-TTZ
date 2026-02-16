using BattleQuest.API.Security;
using BattleQuest.Infrastructure.Database;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;
using BattleQuest.Application.Queries.Events;
using BattleQuest.Infrastructure.Queries.Events;
using BattleQuest.Application.Queries.Players;
using BattleQuest.Infrastructure.Queries.Players;
using BattleQuest.Application.Queries.Items;
using BattleQuest.Infrastructure.Queries.Items;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo
	{
		Title = "BattleQuest API",
		Version = "v1",
		Description = "API para consulta de estatísticas e eventos do jogo BattleQuest"
	});

	// Incluir documentação XML
	var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
	var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
	c.IncludeXmlComments(xmlPath);

	// Swagger: token via header X-API-TOKEN
	var headerName = builder.Configuration["ApiTokenAuth:HeaderName"] ?? "X-API-TOKEN";

	c.AddSecurityDefinition(ApiTokenDefaults.Scheme, new OpenApiSecurityScheme
	{
		Name = headerName,
		Type = SecuritySchemeType.ApiKey,
		In = ParameterLocation.Header,
		Scheme = ApiTokenDefaults.Scheme,
		Description = $"Informe o token no header '{headerName}'."
	});

	c.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference
				{
					Type = ReferenceType.SecurityScheme,
					Id = ApiTokenDefaults.Scheme
				}
			},
			Array.Empty<string>()
		}
	});
});

builder.Services.AddDbContext<BattleQuestDbContext>(options =>
{
	options.UseNpgsql(builder.Configuration.GetConnectionString("BattleQuestDb"))
		.UseSnakeCaseNamingConvention();
});

// Auth por token via header (X-API-TOKEN)
builder.Services.AddAuthentication(ApiTokenDefaults.Scheme)
	.AddScheme<ApiTokenOptions, ApiTokenAuthenticationHandler>(
		ApiTokenDefaults.Scheme,
		options =>
		{
			builder.Configuration.GetSection("ApiTokenAuth").Bind(options);

			// Validação explícita em Development
			if (builder.Environment.IsDevelopment() && 
				(string.IsNullOrWhiteSpace(options.Token) || options.Token == "CHANGE_ME"))
			{
				throw new InvalidOperationException(
					"ApiTokenAuth:Token não está configurado ou usa valor padrão. " +
					"Configure um token válido em appsettings.Development.json.");
			}
		});

builder.Services.AddAuthorization(options =>
{
	// Política default: exige auth em tudo, exceto endpoints [AllowAnonymous]
	options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
		.AddAuthenticationSchemes(ApiTokenDefaults.Scheme)
		.RequireAuthenticatedUser()
		.Build();
});

builder.Services.AddScoped<IEventQueries, EfEventQueries>();
builder.Services.AddScoped<IPlayerQueries, EfPlayerQueries>();
builder.Services.AddScoped<IItemQueries, EfItemQueries>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
