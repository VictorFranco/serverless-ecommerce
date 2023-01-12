using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using MySql.Data.MySqlClient;

namespace T10_2020630109
{
    public static class alta_articulo
    {
        class Articulo
        {
            public string nombre;
            public string descripcion;
            public float precio;
            public int cantidad;
            public string? fotografia;  
        }
        class Error
        {
            public string mensaje;
            public Error(string mensaje)
            {
                this.mensaje = mensaje;
            }
        }
        [FunctionName("alta_articulo")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req, ILogger log)
        {
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                Articulo articulo = JsonConvert.DeserializeObject<Articulo>(requestBody);

                log.LogInformation("C# HTTP trigger function processed a request.");

                string Server = Environment.GetEnvironmentVariable("Server");
                string UserID = Environment.GetEnvironmentVariable("UserID");
                string Password = Environment.GetEnvironmentVariable("Password");
                string Database = Environment.GetEnvironmentVariable("Database");

                string sc = "Server=" + Server + ";UserID=" + UserID + ";Password=" + Password + ";" + "Database=" + Database + ";SslMode=Preferred;";
                var conexion = new MySqlConnection(sc);
                conexion.Open();

                log.LogInformation(requestBody);

                MySqlTransaction transaccion = conexion.BeginTransaction();

                try
                {
                    var cmd = new MySqlCommand();
                    cmd.Connection = conexion;
                    cmd.Transaction = transaccion;
                    cmd.CommandText = "insert into articulos(id, nombre, descripcion, precio, cantidad, fotografia) values(0, @nombre, @descripcion, @precio, @cantidad, @fotografia)";
                    cmd.Parameters.AddWithValue("@nombre", articulo.nombre);
                    cmd.Parameters.AddWithValue("@descripcion", articulo.descripcion);
                    cmd.Parameters.AddWithValue("@precio", articulo.precio);
                    cmd.Parameters.AddWithValue("@cantidad", articulo.cantidad);
                    cmd.Parameters.AddWithValue("@fotografia", Convert.FromBase64String(articulo.fotografia));
                    cmd.ExecuteNonQuery();

                    transaccion.Commit();
                    return new OkObjectResult("Ok");
                }
                catch (Exception e)
                {
                    transaccion.Rollback();
                    throw new Exception(e.Message);
                }
                finally
                {
                    conexion.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error(e.Message)));
            }
        }
    }
}
