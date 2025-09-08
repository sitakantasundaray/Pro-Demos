
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using System.Security.Cryptography.X509Certificates;

namespace MTLSCertValidation
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("TEST: Client Cert Validation Test:");
                Directory.SetCurrentDirectory(AppContext.BaseDirectory);
                CreateHostBuilder(args)
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .Build().Run();
            }

            catch (Exception ex)
            {
                Console.WriteLine("Unhandled exception while starting the service." + ex.Message);
                Console.WriteLine(ex.ToString());
                throw;
            }
        }
        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
        WebHost.CreateDefaultBuilder(args)
        .UseStartup<Startup>();
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory)
                      .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
                      .AddEnvironmentVariables();
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureKestrel((context, options) =>
                {
                    int defaultPort = 5001;
                    string dSubjectName = "localhost";
                    var config = context.Configuration;

                    var httpsPort = config.GetValue<int>("PortSettings:WEB_PORT", defaultPort);
                    var subjectName = config.GetValue<string>("PortSettings:SubjectName") ?? dSubjectName;

                    Console.WriteLine($"1. Kestrel HTTPS Port: {httpsPort}");
                    Console.WriteLine($"2. Kestrel Certificate SubjectName: {subjectName}");

                    using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                    store.Open(OpenFlags.ReadOnly);

                    var serverCert = store.Certificates
                    .Find(X509FindType.FindBySubjectName, subjectName, validOnly: true)
                    .OfType<X509Certificate2>()
                    .Where(c =>
                     c.Issuer.Contains("UPSIssuingCA", StringComparison.OrdinalIgnoreCase) &&
                     c.NotAfter > DateTime.Now)
                    .OrderByDescending(c => c.NotAfter)
                    .FirstOrDefault();

                    if (serverCert == null)
                    {
                        Console.WriteLine($"Certificate not found for subject name: {subjectName}");
                        throw new Exception($"Certificate not found for subject name: {subjectName}");
                    }

                    Console.WriteLine("*** Private key  =" + serverCert.HasPrivateKey);
                    Console.WriteLine($" **Loaded Certificate: Subject={serverCert.Subject}, Issuer={serverCert.Issuer}, Thumbprint={serverCert.Thumbprint}, NotAfter={serverCert.NotAfter}");

                    var certModeString = config["Kestrel:ClientCertificateMode"] ?? "AllowCertificate";
                    if (!Enum.TryParse<ClientCertificateMode>(certModeString, out var certMode))
                    {
                        certMode = ClientCertificateMode.AllowCertificate;
                    }

                    options.ListenAnyIP(httpsPort, listenOptions =>
                    {
                        listenOptions.UseHttps(httpsOptions =>
                        {
                            httpsOptions.ServerCertificate = serverCert;
                            httpsOptions.ClientCertificateMode = certMode;
                            // httpsOptions.AllowAnyClientCertificate();

                            httpsOptions.ClientCertificateValidation = (certificate, chain, errors) =>
                            {
                                Console.WriteLine("<< ClientCertificateValidation Callback hit >>");
                                Console.WriteLine($"<< Client certificate received: Subject={certificate.Subject}, Issuer={certificate.Issuer}, Errors={errors}");

                                if (errors != System.Net.Security.SslPolicyErrors.None)
                                {
                                    Console.WriteLine($"Client certificate validation failed: {errors}");
                                    return false;
                                }

                                if (!certificate.Issuer.Contains("CN=UPSIssuingCA", StringComparison.OrdinalIgnoreCase))
                                {
                                    Console.WriteLine($"Invalid certificate issuer: {certificate.Issuer}");
                                    return false;
                                }
                                Console.WriteLine("<< Client certificate validation Success >>");
                                return true;
                            };
                        });
                    });
                });
                webBuilder.UseStartup<Startup>();
            })
            .UseWindowsService();
    }
}
