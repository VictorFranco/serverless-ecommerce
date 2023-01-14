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
    public static class borrar_de_carrito
    {
        class ParamBorrarDeCarrito
        {
            public string id;
        }
        class Error
        {
            public string mensaje;
            public Error(string mensaje)
            {
                this.mensaje = mensaje;
            }
        }
        [FunctionName("borrar_de_carrito")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
            ILogger log)
        {
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                ParamBorrarDeCarrito p = JsonConvert.DeserializeObject<ParamBorrarDeCarrito>(requestBody);

                string Server = Environment.GetEnvironmentVariable("Server");
                string UserID = Environment.GetEnvironmentVariable("UserID");
                string Password = Environment.GetEnvironmentVariable("Password");
                string Database = Environment.GetEnvironmentVariable("Database");

                string cs = "Server=" + Server + ";UserID=" + UserID + ";Password=" + Password + ";" + "Database=" + Database + ";SslMode=Preferred;";
                var conexion = new MySqlConnection(cs);
                conexion.Open();

                MySqlTransaction transaccion;
                int id = 0, cantidad = 0;

                try
                {
                    var cmd = new MySqlCommand("SELECT c.id_articulo, c.cantidad, a.cantidad FROM carrito_compra c INNER JOIN articulos a ON c.id_articulo = a.id WHERE c.id = @id");
                    cmd.Connection = conexion;
                    cmd.Parameters.AddWithValue("@id", p.id);
                    MySqlDataReader r = cmd.ExecuteReader();

                    try {
                        if (!r.Read())
                            throw new Exception("El articulo no existe");

                        id = r.GetInt32(0);
                        cantidad = r.GetInt32(1) + r.GetInt32(2);
                    }
                    finally
                    {
                        r.Close();
                    }
                }
                catch
                {
                    conexion.Close();
                }

                transaccion = conexion.BeginTransaction();

                try
                {
                    var cmd = new MySqlCommand();
                    cmd.Connection = conexion;
                    cmd.Transaction = transaccion;
                    cmd.CommandText = "UPDATE articulos SET cantidad = @cantidad WHERE id = @id";
                    cmd.Parameters.AddWithValue("@cantidad", cantidad);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();

                    cmd = new MySqlCommand();
                    cmd.Connection = conexion;
                    cmd.Transaction = transaccion;
                    cmd.CommandText = "DELETE FROM carrito_compra WHERE id = @id";
                    cmd.Parameters.AddWithValue("@id", p.id);
                    cmd.ExecuteNonQuery();

                    transaccion.Commit();
                    return new OkObjectResult("Articulo borrado");
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
