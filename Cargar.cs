using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FunctionApp1
{
    public static class Cargar
    {
        [FunctionName("Cargar")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
            ILogger log)
        {
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                string home = Environment.GetEnvironmentVariable("HOME");
                string ruta = data.ruta;
                string nombre = data.nombre;
                byte[] contenido = Convert.FromBase64String((string)data.contenido);

                if (!Directory.Exists(home + "/data" + ruta))
                    Directory.CreateDirectory(home + "/data" + ruta);

                File.WriteAllBytes(home + "/data" + ruta + "/" + nombre, contenido);
                return new OkObjectResult("OK");
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult(e.Message);
            }
        }
    }
}
