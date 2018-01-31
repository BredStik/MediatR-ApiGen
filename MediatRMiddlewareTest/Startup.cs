using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Swagger;

namespace MediatRMiddlewareTest
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                //.AddCookie(options => {
                //    options.SlidingExpiration = true;
                //    options.LoginPath
                //})
                .AddJwtBearer(options =>
                {
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = "yourdomain.com",
                        ValidAudience = "yourdomain.com",
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("my_super_power_is_awesome")),
                    };
                });


            services.AddRouting();

            services.AddMvcCore()
                .AddApiExplorer()
                .AddJsonFormatters()
                .AddAuthorization();

            services.AddMediatR(GetType());

            services.AddSwaggerGen(c =>
            {
                //c..SwaggerDoc("v1", new Info { Title = "Contacts API", Version = "v1" });
                //c.AddSecurityDefinition("bearer", new BasicAuthScheme());                
            });

            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            return services.BuildServiceProvider();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();
            app.UseMiddleware<SwaggerDefinitionMiddleware>();
            app.UseSwagger();
            app.UseSwaggerUI(opt => { opt.SwaggerEndpoint("/api/swagger", "Mediatr API"); opt.InjectOnCompleteJavaScript("js/CustomSwagger.js"); });
            app.UseAuthentication();


            var routeBuilder = new RouteBuilder(app);


            //app.UseMediatRMiddleware(routeBuilder);
            app.ConfigureMediatRRoutes(routeBuilder);
            app.UseRouter(routeBuilder.Build());
            app.UseMvc();
        }
    }

    public static class Extensions
    {
        public static IEnumerable<object> GetRequiredServices(this IServiceProvider provider, Type serviceType)
        {
            return (IEnumerable<object>)provider.GetRequiredService(typeof(IEnumerable<>).MakeGenericType(serviceType));
        }
    }

    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        public LoggingBehavior(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<LoggingBehavior<TRequest, TResponse>>();
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            _logger.LogInformation($"Handling {typeof(TRequest).Name}");
            var response = await next();
            _logger.LogInformation($"Handled {typeof(TResponse).Name}");
            return response;
        }
    }

    public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthorizationBehavior(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            var requiresAuthenticated = typeof(TRequest).GetCustomAttributes(typeof(AuthorizeAttribute), false).Length > 0;

            if(requiresAuthenticated && !_httpContextAccessor.HttpContext.User.Identity.IsAuthenticated)
            {
                throw new UnauthorizedAccessException("User is not authenticated");                
            }

            return next();
        }
    }

    public class UnauthorizedAccessException: ApplicationException
    {
        public UnauthorizedAccessException(string message) : base(message)
        {
        }

        public UnauthorizedAccessException() : base()
        {
        }

        public UnauthorizedAccessException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected UnauthorizedAccessException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }
    }
}
