using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;
using CoronelExpress.Models;
using CoronelExpress.Data; // Asegúrate de usar el namespace correcto para RegisterViewModel

public class AccountController : Controller



{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AccountController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        ApplicationDbContext dbContext,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
        _roleManager = roleManager;
    }

    // Método para enviar la notificación al usuario que se convierte en Admin vía SMTP
    private async Task SendAdminCredentialsEmailAsync(string toEmail, string username, string notificationMessage)
    {
        try
        {
            var fromAddress = new MailAddress("noreply@tuservidor.com", "Sistema Admin");
            var toAddress = new MailAddress(toEmail);
            string subject = "Notificación: Asignación de Rol Administrador";
            string body = $@"
                <p>Felicidades, se te ha asignado el rol de Administrador.</p>
                <p><strong>Usuario:</strong> {username}</p>
                <p>{notificationMessage}</p>
                <p>Te recomendamos cambiar tu contraseña lo antes posible.</p>";

            using (var smtpClient = new SmtpClient("smtp.gmail.com", 587))
            {
                // Reemplaza las credenciales SMTP por las tuyas
                smtpClient.Credentials = new NetworkCredential("manzanocoroneljulioanthony@gmail.com", "rada kwab gevv erkw");
                smtpClient.EnableSsl = true;

                using (var mailMessage = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                })
                {
                    await smtpClient.SendMailAsync(mailMessage);
                }
            }
        }
        catch (System.Exception ex)
        {
            // Registra el error sin afectar el flujo del login
        }
    }

    private async Task EnsureUserRolesAsync(IdentityUser user)
    {
        // Verifica y crea los roles "Admin" y "User" si no existen
        if (!await _roleManager.RoleExistsAsync("Admin"))
        {
            await _roleManager.CreateAsync(new IdentityRole("Admin"));
        }
        if (!await _roleManager.RoleExistsAsync("User"))
        {
            await _roleManager.CreateAsync(new IdentityRole("User"));
        }

        // Obtiene la lista de usuarios que ya tienen rol Admin
        var admins = await _userManager.GetUsersInRoleAsync("Admin");
        if (!admins.Any())
        {
            // Si no hay Admin, asigna el rol Admin al usuario actual
            var result = await _userManager.AddToRoleAsync(user, "Admin");
            if (result.Succeeded)
            {
                await SendAdminCredentialsEmailAsync(user.Email, user.UserName, "Se te ha asignado el rol de Administrador.");
            }
            else
            {
                throw new System.Exception("No se pudo asignar el rol Admin: " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            // Si ya existe un Admin, asigna el rol User al usuario actual (si aún no lo tiene)
            if (!await _userManager.IsInRoleAsync(user, "User"))
            {
                var result = await _userManager.AddToRoleAsync(user, "User");
                if (!result.Succeeded)
                {
                    throw new System.Exception("No se pudo asignar el rol User: " +
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login(string email, string password, bool rememberMe, string returnUrl = null)
    {
        // Buscar el usuario por email
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Inicio de sesión inválido.");
            return View();
        }

        // Verificar si el correo electrónico está confirmado
        if (!await _userManager.IsEmailConfirmedAsync(user))
        {
            ModelState.AddModelError(string.Empty, "Por favor, confirme su correo electrónico antes de iniciar sesión.");
            return View();
        }

        // Asigna roles: si es el primer usuario se le asigna Admin; de lo contrario, User
        await EnsureUserRolesAsync(user);

        // Iniciar sesión usando el UserName obtenido
        var result = await _signInManager.PasswordSignInAsync(user.UserName, password, rememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            var customer = await _dbContext.Customers
                .Include(c => c.TermsAcceptances)
                .FirstOrDefaultAsync(c => c.Email == email);

            if (customer == null)
            {
                customer = new Customer
                {
                    Email = user.Email,
                    FullName = user.UserName,
                    Phone = user.PhoneNumber,
                    Address = string.Empty,
                    RUC = string.Empty,
                    TermsAcceptances = new System.Collections.Generic.List<TermsAcceptance>()
                };

                _dbContext.Customers.Add(customer);
                await _dbContext.SaveChangesAsync();
            }

            if (customer.TermsAcceptances == null || !customer.TermsAcceptances.Any())
            {
                TempData["ReturnUrl"] = returnUrl;
                return RedirectToAction("Terminos", "Home");
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(string.Empty, "Inicio de sesión inválido.");
        return View();
    }





    // =============== REGISTER ===============
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = new IdentityUser
            {
                UserName = model.UserName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Guardar la pregunta y respuesta de seguridad como Claims
                await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("SecurityQuestion", model.SecurityQuestion));
                await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("SecurityAnswer", model.SecurityAnswer.ToLower()));

                // Generar token de confirmación de correo
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, token = token }, Request.Scheme);

                // Enviar correo de confirmación (aquí deberás implementar tu método de envío de correo)
                await SendEmailConfirmationAsync(user.Email, confirmationLink);

                // Opcional: puedes mostrar una vista informando que se envió el correo
                ViewBag.Message = "Registro exitoso. Por favor, confirme su correo electrónico haciendo clic en el enlace enviado.";
                return View("Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(model);
    }
    private async Task SendEmailConfirmationAsync(string email, string confirmationLink)
    {
        // Configura y envía el correo con el enlace de confirmación
        var fromAddress = new MailAddress("noreply@tuservidor.com", "Don Julio Super");
        var toAddress = new MailAddress(email);
        string subject = "Confirmación de Correo Electrónico";
        string body = $@"
        <p>Gracias por registrarte.</p>
        <p>Por favor confirma tu cuenta haciendo clic en el siguiente enlace:</p>
        <p><a href='{confirmationLink}'>Confirmar Correo</a></p>
        <p>Si no solicitaste este registro, ignora este correo.</p>";

        using (var smtpClient = new SmtpClient("smtp.gmail.com"))
        {
            smtpClient.Port = 587;
            smtpClient.Credentials = new NetworkCredential("manzanocoroneljulioanthony@gmail.com", "rada kwab gevv erkw");
            smtpClient.EnableSsl = true;

            using (var mailMessage = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            })
            {
                await smtpClient.SendMailAsync(mailMessage);
            }
        }
    }
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        if (userId == null || token == null)
        {
            return RedirectToAction("Index", "Home");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound($"No se pudo encontrar el usuario con ID '{userId}'.");
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (result.Succeeded)
        {
            ViewBag.Message = "El correo ha sido confirmado exitosamente. Ahora puedes iniciar sesión.";
            return View("ConfirmEmail");
        }
        else
        {
            ViewBag.Message = "Error al confirmar el correo electrónico.";
            return View("Error");
        }
    }


    // =============== LOGOUT ===============
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login", "Account");
    }

    // =============== ACCESS DENIED ===============
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    // =============== RECUPERAR CONTRASEÑA ===============
    [AllowAnonymous]
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            ModelState.AddModelError("", "Por favor, ingresa tu correo.");
            return View();
        }

        // Evitar error por múltiples registros usando FirstOrDefaultAsync
        var user = await _userManager.Users
                       .FirstOrDefaultAsync(u => u.NormalizedEmail == email.ToUpperInvariant());

        if (user == null)
        {
            ModelState.AddModelError("", "No se encontró un usuario con ese correo.");
            return View();
        }

        // Recuperar la pregunta de seguridad
        var claims = await _userManager.GetClaimsAsync(user);
        var securityQuestion = claims.FirstOrDefault(c => c.Type == "SecurityQuestion")?.Value;

        if (string.IsNullOrEmpty(securityQuestion))
        {
            ModelState.AddModelError("", "El usuario no tiene configurada una pregunta de seguridad.");
            return View();
        }

        TempData["ForgotPasswordEmail"] = email;
        TempData["SecurityQuestion"] = securityQuestion;
        return RedirectToAction("ForgotPasswordQuestion");
    }
    [AllowAnonymous]
    [HttpGet]
    public IActionResult ForgotPasswordQuestion()
    {
        ViewData["SecurityQuestion"] = TempData["SecurityQuestion"];
        return View();
    }
    [AllowAnonymous]
[HttpPost]
public async Task<IActionResult> ForgotPasswordQuestion(string answer)
{
    var email = TempData["ForgotPasswordEmail"] as string;
    if (string.IsNullOrEmpty(email))
        return RedirectToAction("ForgotPassword");

    var user = await _userManager.Users
                   .FirstOrDefaultAsync(u => u.NormalizedEmail == email.ToUpperInvariant());
    if (user == null)
        return RedirectToAction("ForgotPassword");

    var claims = await _userManager.GetClaimsAsync(user);
    var securityAnswer = claims.FirstOrDefault(c => c.Type == "SecurityAnswer")?.Value;
    if (!string.Equals(answer?.ToLower(), securityAnswer, StringComparison.Ordinal))
    {
        ModelState.AddModelError("", "Respuesta de seguridad incorrecta.");
        TempData["SecurityQuestion"] = claims.FirstOrDefault(c => c.Type == "SecurityQuestion")?.Value;
        return View();
    }

    // Verificar si ya existe un token almacenado para el usuario
    var storedTokenCombined = await _userManager.GetAuthenticationTokenAsync(user, "Default", "ResetPasswordToken");
        string token = null;
    DateTime creationTime = DateTime.MinValue;
    bool generateNew = true;
    if (!string.IsNullOrEmpty(storedTokenCombined))
    {
        var parts = storedTokenCombined.Split('|');
        if (parts.Length == 2 &&
            DateTime.TryParse(parts[1], null, System.Globalization.DateTimeStyles.RoundtripKind, out creationTime))
        {
            // Si el token fue generado hace menos de 1 día, se reenvía el mismo
            if (DateTime.UtcNow - creationTime < TimeSpan.FromDays(1))
            {
                generateNew = false;
                token = parts[0];
            }
        }
    }

    // Si no existe o ya pasó 1 día, se genera un nuevo token
    if (generateNew)
    {
        token = await _userManager.GeneratePasswordResetTokenAsync(user);
        creationTime = DateTime.UtcNow;
        var tokenCombined = $"{token}|{creationTime:o}";
        await _userManager.SetAuthenticationTokenAsync(user, "Default", "ResetPasswordToken", tokenCombined);
    }

    // Enviar el token por correo electrónico con diseño corporativo
    MailMessage mail = new MailMessage
    {
        From = new MailAddress("comercialdonjulio@example.com", "Comercial Don Julio"),
        Subject = "Restablecer contraseña",
        Body = $@"
            <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; }}
                        .container {{ max-width: 600px; margin: 30px auto; background-color: #ffffff; padding: 20px; border-radius: 8px; box-shadow: 0 0 10px rgba(0,0,0,0.1); }}
                        .header {{ background-color: #003366; color: #ffffff; padding: 10px; text-align: center; border-radius: 8px 8px 0 0; }}
                        .content {{ margin: 20px; }}
                        .footer {{ font-size: 12px; color: #777777; text-align: center; margin-top: 20px; }}
                        .token {{ font-size: 18px; font-weight: bold; color: #003366; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>Cuenta Comercial Don Julio</h2>
                        </div>
                        <div class='content'>
                            <p>Estimado/a usuario,</p>
                            <p>Se ha generado automáticamente un token para el restablecimiento de su contraseña. Utilice el siguiente token:</p>
                            <p class='token'>{token}</p>
                            <p>Recuerde que este token tiene una vigencia de <strong>1 día</strong> a partir de su emisión (Emitido: {creationTime.ToLocalTime():g}).</p>
                            <p>Por favor, ingréselo en la pantalla de restablecimiento de contraseña para continuar con el proceso.</p>
                            <p>Si no solicitó este cambio, por favor ignore este mensaje.</p>
                        </div>
                        <div class='footer'>
                            <p>Este mensaje se envía de forma automática. No responda a este correo.</p>
                        </div>
                    </div>
                </body>
            </html>",
        IsBodyHtml = true
    };

    // Enviar el correo al email registrado en el usuario
    mail.To.Add(user.Email);

    using (SmtpClient client = new SmtpClient("smtp.gmail.com", 587))
    {
        client.Credentials = new NetworkCredential("manzanocoroneljulioanthony@gmail.com", "rada kwab gevv erkw");
        client.EnableSsl = true;
        await client.SendMailAsync(mail);
    }

    // Guardar el email en TempData para continuar el proceso
    TempData["ForgotPasswordEmail"] = user.Email;
    return RedirectToAction("ResetPassword");
}


    [AllowAnonymous]
    [HttpGet]
    public IActionResult ResetPassword()
    {
        // La vista debe contar con campos para que el usuario ingrese:
        // - El token recibido (inputToken)
        // - La nueva contraseña (newPassword)
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> ResetPassword(string inputToken, string newPassword)
    {
        var email = TempData["ForgotPasswordEmail"] as string;
        if (string.IsNullOrEmpty(email))
            return RedirectToAction("ForgotPassword");

        var user = await _userManager.Users
                       .FirstOrDefaultAsync(u => u.NormalizedEmail == email.ToUpperInvariant());
        if (user == null)
            return RedirectToAction("ForgotPassword");

        // Recuperar el token almacenado (que contiene el token y la fecha de creación)
        var storedTokenCombined = await _userManager.GetAuthenticationTokenAsync(user, "Default", "ResetPasswordToken");
        if (string.IsNullOrEmpty(storedTokenCombined))
        {
            ModelState.AddModelError("", "No se encontró token de restablecimiento, por favor solicite uno nuevo.");
            return View();
        }

        // Extraer el token original ignorando la fecha
        var parts = storedTokenCombined.Split('|');
        if (parts.Length != 2)
        {
            ModelState.AddModelError("", "Formato de token incorrecto.");
            return View();
        }
        var storedToken = parts[0];

        // Comparar el token ingresado con el almacenado
        if (!string.Equals(inputToken, storedToken))
        {
            ModelState.AddModelError("", "El token ingresado no es válido.");
            return View();
        }

        // Intentar restablecer la contraseña usando el token original
        var result = await _userManager.ResetPasswordAsync(user, storedToken, newPassword);
        if (result.Succeeded)
        {
            // Eliminar el token después de su uso, para que la próxima solicitud genere uno nuevo
            await _userManager.RemoveAuthenticationTokenAsync(user, "Default", "ResetPasswordToken");
            return RedirectToAction("Login");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }

        return View();
    }
}