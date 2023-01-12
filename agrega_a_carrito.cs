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
    public static class agrega_a_carrito
    {
        class ParamAgregarACarrito
        {
            public int cantidad;
            public string nombre;
        }
        class Error
        {
            public string message;
            public Error(string message)
            {
                this.message = message;
            }
        }
        [FunctionName("agrega_a_carrito")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req, ILogger log)
        {
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                ParamAgregarACarrito p = JsonConvert.DeserializeObject<ParamAgregarACarrito>(requestBody);

                string Server = Environment.GetEnvironmentVariable("Server");
                string UserID = Environment.GetEnvironmentVariable("UserID");
                string Password = Environment.GetEnvironmentVariable("Password");
                string Database = Environment.GetEnvironmentVariable("Database");

                string sc = "Server=" + Server + ";UserID=" + UserID + ";Password=" + Password + ";" + "Database=" + Database + ";SslMode=Preferred;";
                var conexion = new MySqlConnection(sc);
                conexion.Open();

                MySqlTransaction transaccion;
                int id = 0, cantidad = 0;

                try
                {
                    var cmd = new MySqlCommand("SELECT id, cantidad FROM articulos WHERE nombre = @nombre");
                    cmd.Connection = conexion;
                    cmd.Parameters.AddWithValue("@nombre", p.nombre);
                    MySqlDataReader r = cmd.ExecuteReader();

                    try {
                        if (!r.Read())
                            throw new Exception("El articulo no existe");

                        id = r.GetInt32(0);
                        cantidad = r.GetInt32(1);
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

                if (p.cantidad > cantidad) {
                  conexion.Close();
                  throw new Exception("Hay " + cantidad + " productos disponibles");
                }

                cantidad -= p.cantidad;

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

                    transaccion.Commit();
                }
                catch (Exception e)
                {
                    transaccion.Rollback();
                    throw new Exception(e.Message);
                }

                transaccion = conexion.BeginTransaction();

                try
                {
                    var cmd = new MySqlCommand();
                    cmd.Connection = conexion;
                    cmd.Transaction = transaccion;
                    cmd.CommandText = "INSERT INTO carrito_compra(id,cantidad,id_articulo) VALUES (0,@cantidad,@id)";
                    cmd.Parameters.AddWithValue("@cantidad", p.cantidad);
                    cmd.Parameters.AddWithValue("@id", id);
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
