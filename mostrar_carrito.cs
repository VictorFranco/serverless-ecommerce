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
using System.Collections.Generic;

namespace T10_2020630109
{
    public static class mostrar_carrito
    {
        class Articulo
        {
            public int id;
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

        [FunctionName("mostrar_carrito")]
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
                var conexion = new MySqlConnection(cs);
                conexion.Open();

                try
                {
                    var cmd = new MySqlCommand("SELECT c.id, a.nombre, a.descripcion, a.precio, c.cantidad, a.fotografia, length(a.fotografia) FROM carrito_compra c INNER JOIN articulos a ON c.id_articulo=a.id");
                    cmd.Connection = conexion;
                    MySqlDataReader r = cmd.ExecuteReader();

                    try
                    {
                        List<Articulo> articulos = new List<Articulo>();
                        while (r.Read()) {
                            var articulo = new Articulo();
                            articulo.id = r.GetInt32(0);
                            articulo.nombre = r.GetString(1);
                            articulo.descripcion = r.GetString(2);
                            articulo.precio = r.GetFloat(3);
                            articulo.cantidad = r.GetInt32(4);

                            if (!r.IsDBNull(5))
                            {
                                var longitud = r.GetInt32(6);
                                byte[] foto = new byte[longitud];
                                r.GetBytes(5, 0, foto, 0, longitud);
                                articulo.fotografia = Convert.ToBase64String(foto);
                            }

                            articulos.Add(articulo);
                        }

                        return new ContentResult { Content = JsonConvert.SerializeObject(articulos), ContentType = "application/json" };
                    }
                    finally
                    {
                        r.Close();
                    }
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
