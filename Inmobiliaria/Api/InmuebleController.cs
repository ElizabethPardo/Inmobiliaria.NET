using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Inmobiliaria.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Inmobiliaria.Api
{
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    public class InmuebleController : ControllerBase
    {
        private readonly DataContext contexto;
        private readonly IWebHostEnvironment environment;

        public InmuebleController(DataContext contexto, IWebHostEnvironment environment)
        {
            this.contexto = contexto;
            this.environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> GetInmueble()
        {
            try
            {
                var usuario = User.Identity.Name;
                var res = contexto.Inmueble.Include(e => e.Duenio).Where(e => e.Duenio.Email == usuario);
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var usuario = User.Identity.Name;
                return Ok(contexto.Inmueble.Include(e => e.Duenio).Where(e => e.Duenio.Email == usuario).Single(e => e.Id == id));
            }
            catch (Exception ex) 
            {
                return BadRequest(ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromForm] Inmueble inmueble)
        {
            try
            {
                var u = User.Identity.Name;
                //Propietario propietario = await contexto.Propietario.FirstAsync(p => p.Email == u);
                inmueble.PropietarioId = contexto.Propietario.Single(e => e.Email == User.Identity.Name).Id;
                inmueble.Estado = false;
                await contexto.Inmueble.AddAsync(inmueble);
                contexto.SaveChanges();
                if (inmueble.ImagenFile != null && inmueble.Id > 0)
                {
                    string wwwPath = environment.WebRootPath;
                    string path = Path.Combine(wwwPath, "img");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    string fileName = "inmueble_" + inmueble.Id + Path.GetExtension(inmueble.ImagenFile.FileName);
                    string pathCompleto = Path.Combine(path, fileName);
                    inmueble.Imagen = Path.Combine("/img", fileName);
                    using (FileStream stream = new FileStream(pathCompleto, FileMode.Create))
                    {
                        inmueble.ImagenFile.CopyTo(stream);
                    }
                    contexto.Inmueble.Update(inmueble);
                    await contexto.SaveChangesAsync();
                }
                return CreatedAtAction(nameof(GetInmueble), new { id = inmueble.Id }, inmueble);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private async Task<string> CargarImagen(IFormFile imagenFile) 
        {
            if (imagenFile != null && imagenFile.Length > 0)
            {
                var uploadFolder = Path.Combine(environment.WebRootPath, "img");

                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imagenFile.FileName);
                var filePath = Path.Combine(uploadFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath,FileMode.Create))
                {
                    await imagenFile.CopyToAsync(fileStream);

                }

               return "img/" + uniqueFileName;
            }
            return null;
           
        }

        [HttpPut]
        public async Task<IActionResult> Put(Inmueble entidad)
        {
            try
            {
                var user = User.Identity.Name;
                var res = contexto.Inmueble.AsNoTracking().Include(e => e.Duenio).FirstOrDefault(e => e.Id == entidad.Id && e.Duenio.Email == user);

                if (ModelState.IsValid && res != null)
                {
                    //entidad.Id = id;
                    entidad.Duenio = res.Duenio;
                    //  entidad.Duenio = user;
                    contexto.Inmueble.Update(entidad);
                    contexto.SaveChanges();
                    return Ok(entidad);
                }
                return BadRequest();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id)
        {
            try
            {
                var user = User.Identity.Name;
                var res = contexto.Inmueble.AsNoTracking().Include(e => e.Duenio).FirstOrDefault(e => e.Id == id && e.Duenio.Email == user);

                if (ModelState.IsValid && res != null)
                {
                    //entidad.Id = id;
                    //entidad.Duenio = res.Duenio;
                    //  entidad.Duenio = user;
                    if (res.Estado == true)
                    {
                        res.Estado = false;
                        contexto.Inmueble.Update(res);
                    }
                    else {
                        res.Estado = true;
                        contexto.Inmueble.Update(res);
                    }
                    
                    contexto.SaveChanges();
                    return Ok(res);
                }
                return BadRequest();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var entidad = contexto.Inmueble.Include(e => e.Duenio).FirstOrDefault(e => e.Id == id && e.Duenio.Email == User.Identity.Name);
                if (entidad != null)
                {
                    contexto.Inmueble.Remove(entidad);
                    contexto.SaveChanges();
                    return Ok();
                }
                return BadRequest();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpDelete("BajaLogica/{id}")]
        public async Task<IActionResult> BajaLogica(int id)
        {
            try
            {
                var entidad = contexto.Inmueble.Include(e => e.Duenio).FirstOrDefault(e => e.Id == id && e.Duenio.Email == User.Identity.Name);
                if (entidad != null)
                {
                    entidad.Tipo = -1;//cambiar por estado = 0
                    contexto.Inmueble.Update(entidad);
                    contexto.SaveChanges();
                    return Ok();
                }
                return BadRequest();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpGet("disponibles")]
        public async Task<IActionResult> InmueblesDisponibles()
        {
            try
            {
                var usuario = User.Identity.Name;
                return Ok(contexto.Inmueble.Include(e => e.Duenio).Where(e => e.Duenio.Email == usuario && e.Estado));

            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPost("porFechas")]
        public async Task<IActionResult> BuscarInmueblesPorFecha([FromForm]BusquedaPorFechas busqueda)
        {
            try
            {
                var usuario = User.Identity.Name;
                var res = contexto.Contrato.Include(e => e.Inmueble.Duenio)
                    .Where(e => e.Inmueble.Duenio.Email == usuario && (busqueda.FechaInicio <= e.FechaDesde || busqueda.FechaInicio <=  e.FechaHasta) && (busqueda.FechaFin >= e.FechaHasta || busqueda.FechaFin >= e.FechaHasta))
                    .Select(x => new {x.Inmueble.Direccion, x.Inmueble.Ambientes,x.Inmueble.UsoNombre,x.Inmueble.TipoNombre,x.Inquilino.Nombre })
                    .ToList();
                return Ok(res);

            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
    }
}
