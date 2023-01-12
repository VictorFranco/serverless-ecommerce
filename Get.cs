// Carlos Pineda Guerrero. 2021-2023
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using System.IO;
using Newtonsoft.Json;

namespace FunctionApp1
{
    public static class Get
    {
        [FunctionName("Get")]
        [ResponseCache(Duration = 86400, NoStore = false)]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req,
            ILogger log)
        {
            try
            {
                string path = (string)req.Query["nombre"];
                bool descargar = ((string)req.Query["descargar"] ?? "NO").ToUpper() == "SI";
                string home = Environment.GetEnvironmentVariable("HOME");

                try
                {
                    byte[] contenido = File.ReadAllBytes(home + "/data" + path);
                    string nombre = Path.GetFileName(path);
                    string tipo_mime = MimeMapping.GetMimeMapping(path);
                    DateTime fecha_modificacion = File.GetLastWriteTime(home + "/data" + path);

                    if (descargar)
                        return new FileContentResult(contenido, tipo_mime) { FileDownloadName = nombre };
                    else
                        return new FileContentResult(contenido, tipo_mime) { LastModified = fecha_modificacion };
                }
                catch (FileNotFoundException)
                {
                    return new NotFoundResult();
                }
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult(e.Message);
            }
        }
    }
}
