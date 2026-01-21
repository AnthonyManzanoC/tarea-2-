using System;
using System.IO;
using System.Threading.Tasks;
using CoronelExpress.Data;
using CoronelExpress.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Net.Mail;
using System.Net;

namespace CoronelExpress.Controllers
{
    [Route("TrabajaConNosotros")]
    public class TrabajaConNosotrosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TrabajaConNosotrosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: TrabajaConNosotros/Create
        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View();
        }

        // GET: TrabajaConNosotros/CreateSuccess
        [HttpGet("CreateSuccess")]
        public IActionResult CreateSuccess()
        {
            ViewData["SuccessMessage"] = "La solicitud se ha enviado correctamente.";
            return View("Create");
        }

        // POST: TrabajaConNosotros/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TrabajaConNosotros model, IFormFile hojaVida)
        {
            // Eliminamos la validación para HojaVidaPath pues la asignaremos programáticamente
            ModelState.Remove("HojaVidaPath");

            if (hojaVida == null || hojaVida.Length == 0)
            {
                ViewData["ErrorMessage"] = "Debe adjuntar un archivo PDF.";
            }
            else if (!hojaVida.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                ViewData["ErrorMessage"] = "El archivo debe ser un PDF.";
            }
            else
            {
                try
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(hojaVida.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await hojaVida.CopyToAsync(stream);
                    }

                    model.HojaVidaPath = "/uploads/" + uniqueFileName;
                }
                catch (Exception ex)
                {
                    ViewData["ErrorMessage"] = "Error al subir el archivo: " + ex.Message;
                }
            }

            if (ModelState.IsValid && ViewData["ErrorMessage"] == null)
            {
                _context.TrabajaConNosotros.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(CreateSuccess));
            }

            return View(model);
        }

        // GET: TrabajaConNosotros/Index
        [HttpGet("Index")]
        public IActionResult Index()
        {
            var solicitudes = _context.TrabajaConNosotros;
            return View(solicitudes);
        }

        // POST: TrabajaConNosotros/SendSmtp
        [HttpPost("SendSmtp")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendSmtp(int Id, string Email, string MensajeEstado, string MensajeEntrevista)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Email))
                {
                    TempData["SmtpError"] = "El correo del destinatario no es válido.";
                    return RedirectToAction("Dashboard", "Admin");
                }

                // Crear el mensaje de correo (se adapta la información recibida)
                // Crear el mensaje de correo con un HTML bonito acorde al requerimiento
                var mail = new MailMessage
                {
                    From = new MailAddress("noreply@donjuliosuper.com", "Don Julio Súper"),
                    Subject = "Notificación sobre su solicitud de empleo",
                    IsBodyHtml = true,
                    Body = $@"
                    <html>
                      <head>
                        <meta charset='utf-8'>
                        <style>
                          body {{
                            font-family: 'Poppins', sans-serif;
                            background-color: #f8f9fa;
                            padding: 20px;
                            color: #333;
                          }}
                          .container {{
                            background: #ffffff;
                            border-radius: 8px;
                            padding: 20px;
                            box-shadow: 0 0 10px rgba(0,0,0,0.1);
                            max-width: 600px;
                            margin: auto;
                          }}
                          h2 {{
                            color: #0275d8;
                          }}
                          p {{
                            font-size: 16px;
                            line-height: 1.5;
                          }}
                          .highlight {{
                            font-weight: bold;
                            color: #5cb85c;
                          }}
                        </style>
                      </head>
                      <body>
                        <div class='container'>
                          <h2>Notificación sobre su solicitud de empleo</h2>
                          <p>Estimado(a),</p>
                          <p>{MensajeEstado}</p>
                          <p>{MensajeEntrevista}</p>
                          <p>Le agradecemos por postularse. Estamos al pendiente de su Solicitud para la seleccion y pronto nos pondremos en contacto para informarle el estado de la misma.</p>
                          <p>Atentamente,</p>
                          <p><strong>Equipo Don Julio Súper</strong></p>
                        </div>
                      </body>
                    </html>"
                };

                mail.To.Add(Email);

                // Configurar el cliente SMTP (se utiliza Gmail)
                using (var smtpClient = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential("manzanocoroneljulioanthony@gmail.com", "rada kwab gevv erkw");
                    smtpClient.EnableSsl = true;
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtpClient.Timeout = 10000; // 10 segundos de timeout

                    await smtpClient.SendMailAsync(mail);
                }

                TempData["SmtpSuccess"] = "Correo enviado correctamente a " + Email;
                return RedirectToAction("Dashboard", "Admin");
            }
            catch (SmtpException smtpEx)
            {
                // Log en consola y retorno de error específico
                Console.WriteLine($"Error SMTP: {smtpEx.StatusCode} - {smtpEx.Message}");
                TempData["SmtpError"] = "Error SMTP: " + smtpEx.Message;
                return RedirectToAction("Dashboard", "Admin");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar correo: {ex.Message}");
                TempData["SmtpError"] = "Ocurrió un error al enviar el correo: " + ex.Message;
                return RedirectToAction("Dashboard", "Admin");
            }
        }
    }
}