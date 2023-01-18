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
    public static class borrar_carrito
    {
        class Error
        {
            public string mensaje;
            public Error(string mensaje)
            {
                this.mensaje = mensaje;
            }
        }
        [FunctionName("borrar_carrito")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
            ILogger log)
        {
            try
            {
                string Server = Environment.GetEnvironmentVariable("Server");
                string UserID = Environment.GetEnvironmentVariable("UserID");
                string Password = Environment.GetEnvironmentVariable("Password");
                string Database = Environment.GetEnvironmentVariable("Database");

                string cs = "Server=" + Server + ";UserID=" + UserID + ";Password=" + Password + ";" + "Database=" + Database + ";SslMode=Preferred;";
                var conexion1 = new MySqlConnection(cs);
                conexion1.Open();

                MySqlTransaction transaccion;
                int id = 0, cantidad = 0;

                try
                {
                    var cmd = new MySqlCommand("SELECT c.id_articulo, c.cantidad FROM carrito_compra c INNER JOIN articulos a ON c.id_articulo = a.id");
                    cmd.Connection = conexion1;
                    MySqlDataReader r = cmd.ExecuteReader();

                    var conexion2 = new MySqlConnection(cs);
                    conexion2.Open();
                    try {
                        while (r.Read()) {
                            id = r.GetInt32(0);
                            cantidad = r.GetInt32(1);
                            transaccion = conexion2.BeginTransaction();
                            try
                            {
                                var cmd_1 = new MySqlCommand();
                                cmd_1.Connection = conexion2;
                                cmd_1.Transaction = transaccion;
                                cmd_1.CommandText = "UPDATE articulos SET cantidad = cantidad + @cantidad WHERE id = @id";
                                cmd_1.Parameters.AddWithValue("@cantidad", cantidad);
                                cmd_1.Parameters.AddWithValue("@id", id);
                                cmd_1.ExecuteNonQuery();

                                transaccion.Commit();
                            }
                            catch (Exception e)
                            {
                                transaccion.Rollback();
                                throw new Exception(e.Message);
                            }
                        }
                    }
                    finally
                    {
                        r.Close();
                        conexion2.Close();
                    }
                }
                catch
                {
                    conexion1.Close();
                }

                transaccion = conexion1.BeginTransaction();

                try
                {
                    var cmd = new MySqlCommand();
                    cmd.Connection = conexion1;
                    cmd.Transaction = transaccion;
                    cmd.CommandText = "DELETE FROM carrito_compra";
                    cmd.ExecuteNonQuery();

                    transaccion.Commit();
                    return new OkObjectResult("Carrito borrado");
                }
                catch (Exception e)
                {
                    transaccion.Rollback();
                    throw new Exception(e.Message);
                }
                finally
                {
                    conexion1.Close();
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
