using LinqToDB.AspNet;
using LinqToDB.AspNet.Logging;
using LinqToDB.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AspPix
{
    public class Startup
    {


        public IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();

            services.AddControllers();

            services.AddHttpClient<PixImgGetHttp>();
        
            services.AddHttpClient<PixGetHtmlHttp>()
                .ConfigurePrimaryHttpMessageHandler((iser) =>
                {
                    var info = iser.GetRequiredService<IConfiguration>().GetSection(AspPixInfo.Key_Name).Get<AspPixInfo>();

                    return MyHttp.CreateSocketsHttpHandler(info.DNS, info.PORT, info.SNI);
                });

            services.AddLinqToDBContext<AppDataConnection>((provider, options) => {
                options
                .UseSQLiteMicrosoft(_configuration.GetAspPixInfo().CONNECT_STRING)
                .UseDefaultLogging(provider);
            });

            services.AddTransient<PixCrawlingService>();
            services.AddTransient<IntoSqliteService>();
            services.AddTransient<InsertImgService>();

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseStaticFiles(new StaticFileOptions {
                OnPrepareResponse = (v) =>
                {
                    v.Context.Response.Headers.Add("Cache-control", "no-store");
                }
            });

            app.UseRouting();


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();


                endpoints.MapControllers();
            });
        }
    }
}