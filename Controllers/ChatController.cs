using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System;

namespace CoronelExpress.Controllers
{
    [Route("api/chat")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IHttpClientFactory clientFactory, ILogger<ChatController> logger)
        {
            _clientFactory = clientFactory;
            _logger = logger;
        }

        // Clases para recibir el request del cliente
        public class ChatRequest
        {
            public string model { get; set; }
            public Message[] messages { get; set; }
            public string brandInfo { get; set; }
        }

        public class Message
        {
            public string role { get; set; }
            public string content { get; set; }
        }

        // Clases para mapear la respuesta de DeepInfra (estructura similar a OpenAI Chat)
        public class ChatResponse
        {
            public List<Choice> choices { get; set; }
        }

        public class Choice
        {
            public Message message { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ChatRequest request)
        {
            // Validación de saludos básicos (ejemplo)
            string userMsg = request.messages?[0]?.content?.ToLower() ?? "";
            if (userMsg.Contains("hola") || userMsg.Contains("buenos"))
            {
                return Ok(new { response = "¡Hola! Soy tu asistente virtual avanzado, listo para ayudarte. ¿Sobre qué tema deseas conversar hoy?" });
            }

            // Configuración de la API
            string apiKey = "fyijob7XzpVfTi2oH8wqs32QsjV9pBiz";
            string deepInfraUrl = "https://api.deepinfra.com/v1/openai/chat/completions";
            string model = string.IsNullOrWhiteSpace(request.model)
                             ? "mistralai/Mistral-7B-Instruct-v0.1"
                             : request.model;

            // Mensaje de sistema enriquecido que refuerza la identidad y tradición de la marca
            var enrichedMessages = new List<Message>
            {
                new Message
                {
                    role = "system",
                    content = "Información de la marca: Don Julio Súper, fundado en Montalvo en 1963, es el único comercio electrónico de alta gama que ha derrotado a la competencia imitadora. " +
                              "Nuestra trayectoria y compromiso con la excelencia nos hacen únicos. Ofrecemos productos exclusivos, ofertas irrepetibles y envíos ultrarrápidos. " +
                              "Además, nos destacamos por brindar una experiencia digital inmersiva e interactiva: si el usuario menciona 'música', 'video', 'animación' o 'interactivo', " +
                              "responde sugiriendo acciones multimedia, efectos visuales y recomendaciones para mejorar su experiencia en el sitio. " +
                              "Para más información o asistencia, contáctanos al 123-456-789 o a través de contacto@donjuliosuper.com. ¡Vívelo y sé parte de la tradición que nos hace invencibles!" + "Información de la marca: Don Julio Súper es un comercio electrónico de alta gama que ofrece productos exclusivos, ofertas únicas y envíos rápidos. " +
                              "Nuestra misión es brindar una experiencia de compra excepcional. " +
                              "Además, nuestro asistente es interactivo y avanzado: puede responder consultas sobre nuestros productos, promociones, métodos de pago, " +
                              "seguimiento de pedidos, horarios de atención y ubicación. Si lo deseas, también puede sugerirte música y recomendaciones interactivas para mejorar tu experiencia " +
                              "en el sitio. Contáctanos al 123-456-789 o en contacto@donjuliosuper.com. Estamos ubicados en el corazón de la ciudad y contamos con atención personalizada." + "Información de la marca: Don Julio Súper es un comercio electrónico de alta gama que redefine la experiencia digital. " +
                              "Ofrecemos productos exclusivos, ofertas únicas y envíos ultrarrápidos. Nuestra misión es brindar una experiencia de compra " +
                              "inmersiva y de vanguardia. Eres un asistente virtual supremo, interactivo y con inteligencia visual avanzada. " +
                              "Si el usuario menciona 'música', 'video', 'animación' o 'interactivo', responde sugiriendo acciones concretas, " +
                              "por ejemplo, indicando cómo reproducir una playlist o activar un reproductor multimedia en la web. " +
                              "Además, incorpora referencias visuales, animaciones y efectos en tus respuestas, y destaca siempre nuestra atención personalizada. " +
                              "Contáctanos al 123-456-789 o en contacto@donjuliosuper.com. Estamos ubicados en el corazón de la ciudad."
                }
            };

            // Agregamos los mensajes enviados por el usuario.
            enrichedMessages.AddRange(request.messages);

            // Construimos el payload para la API de DeepInfra utilizando los mensajes enriquecidos.
            var payload = new
            {
                model = model,
                messages = enrichedMessages
            };
            string jsonPayload = JsonConvert.SerializeObject(payload);

            try
            {
                var client = _clientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(deepInfraUrl, httpContent);
                if (response.IsSuccessStatusCode)
                {
                    var resultContent = await response.Content.ReadAsStringAsync();
                    var chatResponse = JsonConvert.DeserializeObject<ChatResponse>(resultContent);
                    if (chatResponse != null && chatResponse.choices != null && chatResponse.choices.Count > 0)
                    {
                        // Extraemos el mensaje del asistente de la primera opción
                        var assistantMessage = chatResponse.choices[0].message.content;
                        return Ok(new { response = assistantMessage });
                    }
                    else
                    {
                        return BadRequest("La respuesta de la API no tiene el formato esperado.");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error en API DeepInfra: {response.StatusCode} - {errorContent}");
                    return StatusCode((int)response.StatusCode, "Error al llamar a la API de IA.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception durante la solicitud del chat");
                return StatusCode(500, "Error interno en el servidor.");
            }
        }
    }
}