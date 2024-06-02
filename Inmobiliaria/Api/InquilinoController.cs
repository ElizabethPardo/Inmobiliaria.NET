using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inmobiliaria.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Inmobiliaria.Api
{
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    public class InquilinoController : ControllerBase
    {
        private readonly DataContext contexto;
     

        public InquilinoController(DataContext contexto)
        {
            this.contexto = contexto;
            
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var usuario = User.Identity.Name;
               
                var res = contexto.Contrato.Include(x => x.Inquilino)
                    .Include(x => x.Inmueble)
                    .ThenInclude(x => x.Duenio)
                    .Where(c => c.Inmueble.Duenio.Email == usuario)
                    .Select(x => x.Inquilino).Distinct()
                    .Single(e => e.Id == id);

                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

      
        [HttpPost]
        public async Task<IActionResult> Post([FromForm] Inquilino inquilino)
        {
            try
            {

                if (ModelState.IsValid)
                {
                    contexto.Inquilino.Add(inquilino);
                    contexto.SaveChanges();
                    return CreatedAtAction(nameof(Get), new { id = inquilino.Id }, inquilino);
                }

                return BadRequest();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromForm] Inquilino inquilino)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    contexto.Inquilino.Update(inquilino);
                    contexto.SaveChanges();
                    return Ok(inquilino);
                }

                return BadRequest();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InquilinoExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }
        private bool InquilinoExists(int id)
        {
            return contexto.Inquilino.Any(e => e.Id == id);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var p = contexto.Inquilino.Find(id);
                    if (p == null)
                        return NotFound();
                    contexto.Inquilino.Remove(p);
                    contexto.SaveChanges();
                    return Ok(p);
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
