using System;
using System.Linq;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Twilio.TwiML;
using Twilio.TwiML.Messaging;
using IWhatsappHandlerService = CommunicationApi.Interfaces.IWhatsappHandlerService;
using WhatsappResponse = CommunicationApi.Models.WhatsappResponse;

namespace CommunicationApi.Controllers
{
    /// <summary>
    /// API endpoint to interact with WhatsApp.
    /// </summary>
    [ApiController]
    [Route("api/v1/whatsapp")]
    public class WhatsAppController : ControllerBase
    {
        private readonly IWhatsappHandlerService _whatsappHandlerService;
        private ILogger<WhatsAppController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WhatsAppController"/> class.
        /// </summary>
        /// <param name="whatsappHandlerService">The service to handle the WhatsApp requests of the API application.</param>
        public WhatsAppController(IWhatsappHandlerService whatsappHandlerService, ILogger<WhatsAppController> logger)
        {
            Guard.NotNull(whatsappHandlerService, nameof(whatsappHandlerService));
            Guard.NotNull(logger, nameof(logger));
            _whatsappHandlerService = whatsappHandlerService;
            _logger = logger;
        }


        /// <summary>
        ///     Handle Request
        /// </summary>
        /// <remarks>Handles incoming Whatsapp requests.</remarks>
        [HttpPost(Name = "Whatsapp_Process")]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> Post([FromForm] IFormCollection collection)
        {
            WhatsappResponse response;
            try
            {
                _logger.LogTrace($"Request received in the Whatsapp Controller");
                var pars = collection.Keys
                    .Select(key => new {Key = key, Value = collection[key]})
                    .ToDictionary(p => p.Key, p => p.Value.ToString());

                response = await _whatsappHandlerService.ProcessRequest(pars);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error occurred when processing the whatsapp message {e.Message}");
                response = new WhatsappResponse
                {
                    Accepted = false,
                    ResponseMessage = $"Error: {e.ToString()}"
                };
            }

            var twiml = GenerateTwilioResponse(response);
            return new ContentResult
            {
                Content = twiml.ToString(),
                ContentType = "application/xml",
                StatusCode = 200
            };
        }

        private MessagingResponse GenerateTwilioResponse(WhatsappResponse response)
        {
            var twiml = new MessagingResponse();
            var twimlMessage = new Message();
            twimlMessage.Body(response.ResponseMessage);
            if(!string.IsNullOrEmpty(response.ImageUrl))
            {
                twimlMessage.Media(new Uri(response.ImageUrl));
            }
            twiml.Append(twimlMessage);
            return twiml;
        }
    }
}