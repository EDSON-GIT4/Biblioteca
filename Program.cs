using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using LibraryAPI.Data;
using LibraryAPI.Services;
using System.Text;
using LibraryAPI.Middleware;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;

namespace AppAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // Configuração que já carrega tudo automaticamente: 1. appsettings.json 2. appsettings.{Environment}.json 3. Variáveis de ambiente (sobrescreve tudo) 4. Arquivo .env (se configurado)

            builder.Services.AddOpenApi();

            // Add services
            builder.Services.AddControllers().AddJsonOptions(options =>
                {
                    // Aceitar nomes de propriedades em minúsculo (camelCase)
                    options.JsonSerializerOptions.PropertyNamingPolicy = 
                        System.Text.Json.JsonNamingPolicy.CamelCase;
                    
                    // Ou para ser mais flexível:
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                });
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Configurar Swagger com suporte a JWT
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Library API", Version = "v1" });
                
                // Configurar autenticação JWT no Swagger
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header. Exemplo: 'Bearer {token}'",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });

            // Database connect
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            
            builder.Services.AddDbContext<LibraryDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // HTTP Context Accessor
            builder.Services.AddHttpContextAccessor();
            // Adicionar Memory Cache para Rate Limiting
            builder.Services.AddMemoryCache();
            
            //

            //teste de autenticar usuários e clients:
            // ==========================================
            // CONFIGURAÇÃO JWT UNIFICADA
            // ==========================================

            var jwtSecret = builder.Configuration["Jwt:Secret"] 
                ?? throw new InvalidOperationException("JWT Secret não configurado");

            // Use UTF8 em vez de ASCII para suportar mais caracteres
            var key = Encoding.UTF8.GetBytes(jwtSecret); 
            var symmetricKey = new SymmetricSecurityKey(key);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            // Esquema para USUÁRIOS
            .AddJwtBearer("UserScheme", options =>
            {
                options.RequireHttpsMetadata = false; // true em produção!
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = symmetricKey,
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["Jwt:Audience"], // "LibraryAPI-Users"
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero, // Sem tolerância para usuários
                    
                    // Validação adicional para usuários
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role
                };
                
                // Eventos para log e validações extras
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        // Verificar se é realmente um token de usuário
                        var tokenType = context.Principal?.FindFirst("token_type")?.Value;
                        if (tokenType != "user")
                        {
                            context.Fail("Token não é do tipo 'user'");
                        }
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<Program>>();
                        logger.LogWarning("Falha na autenticação de usuário: {Error}", 
                            context.Exception.Message);
                        return Task.CompletedTask;
                    }
                };
            })
            // Esquema para CLIENTES API
            .AddJwtBearer("ClientScheme", options =>
            {
                options.RequireHttpsMetadata = false; // true em produção!
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = symmetricKey,
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["Jwt:ClientAudience"], // "LibraryAPI-Clients"
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1), // Pequena tolerância para clientes
                    
                    // Claims específicas para clientes
                    NameClaimType = "client_id",
                    RoleClaimType = "client_type"
                };
                
                // Eventos para clientes
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        // Verificar se é realmente um token de cliente
                        var tokenType = context.Principal?.FindFirst("token_type")?.Value;
                        if (tokenType != "client")
                        {
                            context.Fail("Token não é do tipo 'client'");
                        }
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<Program>>();
                        logger.LogWarning("Falha na autenticação de cliente: {Error}", 
                            context.Exception.Message);
                        return Task.CompletedTask;
                    }
                };
            })
            // POLÍTICA PADRÃO: Aceita AMBOS os esquemas
            .AddPolicyScheme(JwtBearerDefaults.AuthenticationScheme, "JWT Default", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    // Tenta extrair o token para decidir qual esquema usar
                    var authHeader = context.Request.Headers["Authorization"]
                        .FirstOrDefault()?.Split(" ").Last();
                    
                    if (!string.IsNullOrEmpty(authHeader))
                    {
                        try
                        {
                            // Decodifica o token para ler o tipo (sem validar)
                            var handler = new JwtSecurityTokenHandler();
                            var jwtToken = handler.ReadJwtToken(authHeader);
                            
                            var tokenType = jwtToken.Claims
                                .FirstOrDefault(c => c.Type == "token_type")?.Value;
                            
                            // Redireciona para o esquema correto
                            if (tokenType == "client")
                                return "ClientScheme";
                            else if (tokenType == "user")
                                return "UserScheme";
                        }
                        catch
                        {
                            // Se não conseguir ler, usa o padrão
                        }
                    }
                    
                    // Padrão: tenta como usuário primeiro
                    return "UserScheme";
                };
            });

            // ==========================================
            // CONFIGURAÇÃO DE AUTORIZAÇÃO
            // ==========================================

            builder.Services.AddAuthorization(options =>
            {
                // Política padrão: aceita usuário OU cliente
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes("UserScheme", "ClientScheme")
                    .Build();
                
                // Política exclusiva para usuários
                options.AddPolicy("UserOnly", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.AddAuthenticationSchemes("UserScheme");
                    policy.RequireClaim("token_type", "user");
                });
                
                // Política exclusiva para clientes
                options.AddPolicy("ClientOnly", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.AddAuthenticationSchemes("ClientScheme");
                    policy.RequireClaim("token_type", "client");
                });
                
                // Política para administradores (usuários admin)
                options.AddPolicy("AdminOnly", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.AddAuthenticationSchemes("UserScheme");
                    policy.RequireClaim("token_type", "user");
                    policy.RequireRole("Admin"); // Se tiver roles
                });
            });

            // Services
            builder.Services.AddScoped<BookService>();
            builder.Services.AddScoped<LoanService>();
            builder.Services.AddScoped<AuthorService>();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<AuthServiceUser>();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

            // Registrar serviços de cliente
            builder.Services.AddScoped<AuthServiceClient>();
            builder.Services.AddScoped<ClienteService>();

            // CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline. Para testar em produção(ou container simulando produção, mude .IsDevelopment() para .IsProduction())
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            //  IMPORTANTE: Authentication antes de Authorization
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseClientRateLimit(); // Rate limiting para clientes

            //app.UseHttpsRedirection();
            app.UseCors("AllowAll");
            
            
            app.MapControllers();
            
            // Apply migrations automatically
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
                
                var retries = 10;
                var connected = false;
                
                while (retries > 0 && !connected)
                {
                    try
                    {
                        Console.WriteLine($" Tentando conectar ao banco... ({retries} tentativas restantes)");
                        
                        if (db.Database.CanConnect())
                        {
                            Console.WriteLine(" Conectado ao banco de dados!");
                            Console.WriteLine("Aplicando migrations...");
                            db.Database.Migrate();
                            Console.WriteLine(" Migrations aplicadas com sucesso!");
                            connected = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($" Tentativa falhou: {ex.Message}");
                        retries--;
                        
                        if (retries > 0)
                        {
                            Console.WriteLine($" Aguardando 3 segundos para próxima tentativa...");
                            Thread.Sleep(3000);
                        }
                    }
                }
                
                if (!connected)
                {
                    Console.WriteLine(" NÃO FOI POSSÍVEL CONECTAR AO BANCO APÓS VÁRIAS TENTATIVAS!");
                    Console.WriteLine(" A API iniciará sem aplicar migrations.");
                    // NÃO USE throw aqui - deixa a API iniciar mesmo sem banco
                }
            }   

            app.MapGet("/", () =>
            {
                return("Olá Esse é o endpoint raiz da API");
            });

            app.Run();

        }
    }
}