using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;
using System.Data.SqlClient;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using GameLib.Sprites;
using System.Numerics;
using GameLib.GameSerialization;
using Npgsql;
using System.Data.Common;
using System.IO;

namespace TableTopSim.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        DbConnection sqlConnection;
        bool isLocal = true;
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {

            //SqlConnectionStringBuilder connectionStringBuilder;
            //connectionStringBuilder = new SqlConnectionStringBuilder();
            //connectionStringBuilder.IntegratedSecurity = true;
            //connectionStringBuilder.DataSource = @"(localdb)\MSSQLLocalDB";
            //connectionStringBuilder.InitialCatalog = "TableTopSimDB";
            //sqlConnection = new SqlConnection(connectionStringBuilder.ConnectionString);
            string dbUrlVariableName = "DATABASE_URL";
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(dbUrlVariableName)))
            {
                isLocal = true;
            }
            if (isLocal)
            {
                var authInfo = File.ReadAllLines("auth.txt");
                Environment.SetEnvironmentVariable(dbUrlVariableName, $"postgres://{authInfo[0]}:{authInfo[1]}@localhost:5432/TableTopSimDB");
            }

            string vble = Environment.GetEnvironmentVariable(dbUrlVariableName);
            Uri dbUri = new Uri(Environment.GetEnvironmentVariable(dbUrlVariableName));
            var splitUserInfo = dbUri.UserInfo.Split(':');
            string npgsqlStr = $"Host={dbUri.Host};Username={splitUserInfo[0]};Password={splitUserInfo[1]};Database={dbUri.AbsolutePath.Substring(1)}";
            sqlConnection = new NpgsqlConnection(npgsqlStr);
            services.AddControllersWithViews();
            services.AddRazorPages();
            services.AddScoped<DbConnection>(sp =>
            {
                return new NpgsqlConnection(npgsqlStr);
                //return new SqlConnection(connectionStringBuilder.ConnectionString);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseWebSockets();

            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/ws")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        await SocketHandler.Get(sqlConnection).StartWebsocket(context, webSocket);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }

            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}
