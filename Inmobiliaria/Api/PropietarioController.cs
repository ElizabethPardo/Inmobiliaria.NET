using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Inmobiliaria.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using MimeKit;
using MailKit.Net.Smtp;
using System.Text;
using System.Net.Sockets;
using System.Net;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Inmobiliaria.Api
{
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    public class PropietarioController : ControllerBase
    {
        private readonly DataContext contexto;
        private readonly IConfiguration config;

        public PropietarioController(DataContext contexto, IConfiguration config)
        {
            this.contexto = contexto;
            this.config = config;

        }

        [HttpGet]
        public async Task<ActionResult> Get()
        {
            try
            {

                var usuario = User.Identity.Name;
                var propietario = contexto.Propietario.FirstOrDefault(x => x.Email == usuario);

                return Ok(propietario);

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
                if (id <= 0)
                    return NotFound();

                var res = contexto.Propietario.FirstOrDefault(x => x.Id == id);
                if (res != null)
                    return Ok(res);
                else
                    return NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }



        }

        [HttpPost]
        public async Task<ActionResult> Post([FromForm] Propietario propietario)
        {

            try
            {

                var res = contexto.Propietario.FirstOrDefault(x => x.Email == propietario.Email);
                if ((ModelState.IsValid) && (res != null))
                {
                    contexto.Propietario.Update(propietario);
                    contexto.SaveChanges();
                    return Ok(propietario);
                }

                return BadRequest();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PropietarioExists(propietario.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }


        }

        [HttpPut]
        public async Task<IActionResult> Put(Propietario propietario)
        {

            try
            {

                var usuario = User.Identity.Name;
                var res = contexto.Propietario.AsNoTracking().FirstOrDefault(x => x.Email == usuario && x.Email == propietario.Email);
                if ((ModelState.IsValid) && (res != null))
                {


                    propietario.Id = res.Id;
                    contexto.Propietario.Update(propietario);
                    contexto.SaveChanges();



                    return Ok(propietario);
                }

                return BadRequest();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PropietarioExists(propietario.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }


        }

        private bool PropietarioExists(int id)
        {
            return contexto.Propietario.Any(e => e.Id == id);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var p = contexto.Propietario.Find(id);
                    if (p == null)
                        return NotFound();
                    contexto.Propietario.Remove(p);
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



        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromForm] LoginView loginView)
        {
            try
            {
                string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                    password: loginView.Clave,
                    salt: System.Text.Encoding.ASCII.GetBytes(config["Salt"]),
                    prf: KeyDerivationPrf.HMACSHA1,
                    iterationCount: 1000,
                    numBytesRequested: 256 / 8));
                var p = contexto.Propietario.FirstOrDefault(x => x.Email == loginView.Usuario);
                if (p == null || p.Clave != hashed)
                {
                    return BadRequest("Nombre de usuario o clave incorrecta");
                }
                else
                {

                    var key = new SymmetricSecurityKey(System.Text.Encoding.ASCII.GetBytes(config["TokenAuthentication:SecretKey"]));
                    var credenciales = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, p.Email),
                        new Claim("FullName", p.Nombre + " " + p.Apellido),
                        new Claim(ClaimTypes.Role, "Propietario"),
                    };

                    var token = new JwtSecurityToken(
                        issuer: config["TokenAuthentication:Issuer"],
                        audience: config["TokenAuthentication:Audience"],
                        claims: claims,
                        expires: DateTime.Now.AddMinutes(60),
                        signingCredentials: credenciales
                    );
                    return Ok(new JwtSecurityTokenHandler().WriteToken(token));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }


        [HttpPut("editarPass")]
        public ActionResult CambiarPass([FromForm] CambioClaveView cambio)
        {

            try
            {
                var prop = contexto.Propietario.FirstOrDefault(x => x.Email == User.Identity.Name);

                // verificar clave antigüa
                var pass = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                        password: cambio.ClaveVieja ?? "",
                        salt: System.Text.Encoding.ASCII.GetBytes(config["Salt"]),
                        prf: KeyDerivationPrf.HMACSHA1,
                        iterationCount: 1000,
                        numBytesRequested: 256 / 8));


                if (ModelState.IsValid)
                {
                    if (prop.Clave != pass)
                    {

                        return BadRequest("Clave actual incorrecta");

                    }

                    if (cambio.ClaveNueva != cambio.ClaveRepeticion) 
                    {
                        return BadRequest("Las claves no coinciden");
                    }



                    prop.Clave = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                         password: cambio.ClaveNueva,
                         salt: System.Text.Encoding.ASCII.GetBytes(config["Salt"]),
                         prf: KeyDerivationPrf.HMACSHA1,
                         iterationCount: 1000,
                         numBytesRequested: 256 / 8));


                    contexto.Propietario.Update(prop);
                    contexto.SaveChanges();


                    return Ok(prop);
                }
                else
                {

                    return BadRequest();


                }


            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }


        [HttpGet("ResetearPass")]
        public async Task<IActionResult> ResetearPass()
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(config["TokenAuthentication:SecretKey"]);
                var symmetricKey = new SymmetricSecurityKey(key);
                Random rand = new Random(Environment.TickCount);
                string randomChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789";
                string nuevaClave = "";
                for (int i = 0; i < 8; i++)
                {
                    nuevaClave += randomChars[rand.Next(0, randomChars.Length)];
                }
                    string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                        password: nuevaClave,
                        salt: Encoding.ASCII.GetBytes(config["Salt"]),
                        prf: KeyDerivationPrf.HMACSHA1,
                        iterationCount: 1000,
                        numBytesRequested: 256 / 8));
                    var p = await contexto.Propietario.FirstOrDefaultAsync(x => x.Email == User.Identity.Name);

                if (p == null)
                {
                    return BadRequest("Nombre de usuario incorrecto");
                }
                else
                {
                    p.Clave = hashed;
                    contexto.Propietario.Update(p);
                    await contexto.SaveChangesAsync();
                    var message = new MimeMessage();
                    message.To.Add(new MailboxAddress(p.Nombre, p.Email));
                    message.From.Add(new MailboxAddress("Sistema", "ac7c37917fa391"));
                    message.Subject = "Restablecimiento de Contraseña";
                    message.Body = new TextPart("html")
                    {
                        Text = $"<h1>Hola {p.Nombre},</h1>" +
                               $"<p>Has cambiado tu contraseña de forma correcta. " +
                               $"Tu nueva contraseña es la siguiente: {nuevaClave}</p>"
                    };
                    using var client = new SmtpClient();
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    await client.ConnectAsync("sandbox.smtp.mailtrap.io", 587, MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync("ac7c37917fa391", "78e0f98eaa30b2");
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);

                    return Ok("Se ha restablecido la contraseña correctamente.");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        

        }

        [HttpPost("envioClave")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByEmail([FromForm] string Usuario)
        {
            try
            { // busca el propietario x email
                var p = await contexto.Propietario.FirstOrDefaultAsync(x => x.Email == Usuario);

                var key = new SymmetricSecurityKey(System.Text.Encoding.ASCII.GetBytes(config["TokenAuthentication:SecretKey"]));
                var credenciales = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, p.Email),
                        new Claim("FullName", p.Nombre + " " + p.Apellido),
                        new Claim(ClaimTypes.Role, "Propietario"),
                    };

                var token = new JwtSecurityToken(
                    issuer: config["TokenAuthentication:Issuer"],
                    audience: config["TokenAuthentication:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(60),
                    signingCredentials: credenciales
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
                var dominio = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();
                var LinkUrl = Url.Action("ResetearPass", "Propietario");
                // var rutaCompleta = Request.Scheme + "://" + GetLocalIpAddress() + ":" + Request.Host.Port + LinkUrl;

                var rutaCompleta = Request.Scheme + "://" + GetLocalIpAddress() + ":" + 45457 + LinkUrl;

                var message = new MimeKit.MimeMessage();
                message.To.Add(new MailboxAddress(p.Nombre, p.Email));
                message.From.Add(new MailboxAddress("Sistema", "ac7c37917fa391"));
                message.Subject = "Reseteo de Password";
                message.Body = new TextPart("html")
                {
                    Text = $@"<h1>Hola {p.Nombre},</h1>						   <p>Hemos recibido una solicitud para restablecer la contraseña de tu cuenta.							<p>Por favor, haz clic en el siguiente enlace para crear una nueva contraseña:</p>						   <a href='{rutaCompleta}?access_token={tokenString}'>{rutaCompleta}?access_token={tokenString}</a>"
                };

                SmtpClient client = new SmtpClient();
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                await client.ConnectAsync("sandbox.smtp.mailtrap.io", 587, MailKit.Security.SecureSocketOptions.Auto);
                await client.AuthenticateAsync("ac7c37917fa391", "78e0f98eaa30b2");//estas credenciales deben estar en el user secrets
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
                return Ok(new JwtSecurityTokenHandler().WriteToken(token));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        private string GetLocalIpAddress()
        {
            string localIp = null;
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIp = ip.ToString();
                    break;
                }
            }
            return localIp;
        }
    } 
}
