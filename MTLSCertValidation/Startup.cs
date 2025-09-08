namespace MTLSCertValidation
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        // Configure services here
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSwaggerGen();// Or whatever you need
        }
        // Configure the HTTP pipeline here
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                app.UseSwagger(); // Serves the swagger JSON
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MTLSCertValidation API V1");
                    // Optionally set route prefix here (default is "swagger")
                });
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();  // Optional
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers(); // Or MapDefaultControllerRoute()
            });
        }
    }
}
