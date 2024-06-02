using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Inmobiliaria.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inmobiliaria.Api
{
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    public class ContratoController : ControllerBase
    {
        private readonly DataContext contexto;

        public ContratoController(DataContext contexto)
        {
            this.contexto = contexto;
        }
      
        [HttpGet("PorInmueble/{id}")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var usuario = User.Identity.Name;
                var res = contexto.Contrato.Include(x => x.Inquilino)
                    .Include(x => x.Inmueble)
                    .ThenInclude(x => x.Duenio)
                    .Where(c => c.Inmueble.Duenio.Email == usuario)
                    .Single(x => x.Inmueble.Id == id);

                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromForm] Contrato contrato)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    contrato.InmuebleId = contexto.Inmueble.FirstOrDefault(e => e.Duenio.Email == User.Identity.Name).Id;
                    contrato.InquilinoId = contexto.Inquilino.Single(e => e.Id == contrato.InquilinoId).Id;
                    contexto.Contrato.Add(contrato);
                    contexto.SaveChanges();
                    return CreatedAtAction(nameof(Get), new { id =contrato.Id }, contrato);
                }
                return BadRequest();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromForm] Contrato contrato)
        {
            try
            {
                if (ModelState.IsValid && contexto.Contrato.AsNoTracking().Include(e => e.Inmueble).ThenInclude(x => x.Duenio).FirstOrDefault(e => e.Id == id && e.Inmueble.Duenio.Email == User.Identity.Name) != null)
                {
                   
                    contrato.Id = id;
                    contexto.Contrato.Update(contrato);
                    contexto.SaveChanges();
                    return Ok(contrato);
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
                var entidad = contexto.Contrato.Include(e => e.Inmueble.Duenio).FirstOrDefault(e => e.Id == id && e.Inmueble.Duenio.Email == User.Identity.Name); ;
                if (entidad != null)
                {
                    contexto.Contrato.Remove(entidad);
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

        [HttpGet("GetPropietariosVigentes")]
        public async Task<IActionResult> GetPropietariosVigentes() 
        {

            try
            {

                var user = User.Identity.Name;
                var res = contexto.Contrato
                .Include(x => x.Inquilino)
                .Include(x => x.Inmueble)
                .ThenInclude(x => x.Duenio)
                .Where(y => y.Inmueble.Duenio.Email == user && (y.FechaHasta >= DateTime.Now));
               

            return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
             }
      }

    }
}
