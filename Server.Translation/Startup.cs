using Microsoft.CognitiveServices.Speech;

namespace Server.Translation
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpc();
            services.AddLogging();
            var secrets = Configuration.GetSection("Secrets").Get<Secrets>();
            services.AddSingleton<TranslationServices>(provider =>
            {
                var loggerFacotry = provider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFacotry.CreateLogger<TranslationServices>();
                return new TranslationServices(logger, secrets);
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<TranslationServices>();
            });
        }
    }
}