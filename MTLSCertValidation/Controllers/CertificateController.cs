using Microsoft.AspNetCore.Mvc;

namespace MTLSCertValidation.Controllers
{
    [ApiController]
    [Route("validateCert")]
    public class CertificateController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Get()
        {

            var cert = HttpContext.Connection.ClientCertificate
               ?? await HttpContext.Connection.GetClientCertificateAsync();

            Console.WriteLine(cert != null ? $"Cert: {cert.Subject}" : "No cert");
            if (cert == null)
                return Unauthorized("No client certificate provided.");

            Console.WriteLine(cert != null ? $"Cert: {cert.Subject}" : "No cert");
            var certInfo = new
            {
                Subject = cert.Subject,
                Thumbprint = cert.Thumbprint,
                Issuer = cert.Issuer,
                ValidFrom = cert.NotBefore,
                ValidTo = cert.NotAfter
            };
            return Ok(certInfo);
        }
    }
}
