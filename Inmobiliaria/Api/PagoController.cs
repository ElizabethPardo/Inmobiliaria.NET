using System;
using System.Collections.Generic;
using System.Linq;
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
    public class PagoController : ControllerBase
    {
        private readonly DataContext contexto;

        public PagoController(DataContext contexto)
        {
            this.contexto = contexto;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var usuario = User.Identity.Name;
                var res = contexto.Pago.Include(e => e.Contrato)
                                       .Where(e => e.Contrato.Inmueble.Duenio.Email == usuario)
                                       .Select(x => new { x.NroPago, x.FechaPago, x.Importe});

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
                var res = contexto.Pago.Include(e => e.Contrato)
                                       .Where(e => e.Contrato.Inmueble.Duenio.Email == usuario && e.ContratoId == id)
                                       .Select(x => new {x.Id, x.NroPago, x.FechaPago, x.Importe,x.ContratoId});
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

  
        [HttpPost]
        public async Task<IActionResult> Post([FromForm] Pago pago)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var usuario = User.Identity.Name;
                    pago.ContratoId = contexto.Contrato.FirstOrDefault(e => e.Inmueble.Duenio.Email == User.Identity.Name).Id;
                    contexto.Pago.Add(pago);
                    contexto.SaveChanges();
                    return CreatedAtAction(nameof(Get), new { id = pago.Id }, pago);
                }

                return BadRequest();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
    }


        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromForm] Pago pago)
        {
            try
            {
                if (ModelState.IsValid && contexto.Pago.AsNoTracking().Include(e => e.Contrato.Inmueble).ThenInclude(x => x.Duenio).FirstOrDefault(e => e.Id == id && e.Contrato.Inmueble.Duenio.Email == User.Identity.Name) != null)
                {

                    pago.Id = id;
                    contexto.Pago.Update(pago);
                    contexto.SaveChanges();
                    return Ok(pago);
                }
                return BadRequest();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

      
       
    }
}
