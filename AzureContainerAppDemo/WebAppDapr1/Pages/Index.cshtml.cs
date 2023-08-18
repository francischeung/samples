using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApp1.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IConfiguration _config;

        public IEnumerable<WeatherForecast>? WeatherForecast { get; set; }

        public IndexModel(ILogger<IndexModel> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public async Task OnGet()
        {
            var daprClient = new DaprClientBuilder().Build();
            var result = daprClient.CreateInvokeMethodRequest(HttpMethod.Get, "webapiapp-dapr", "WeatherForecast");
            this.WeatherForecast = await daprClient.InvokeMethodAsync<IEnumerable<WeatherForecast>>(result);
        }
    }

    public class WeatherForecast
    {
        public DateTime Date { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        public string? Summary { get; set; }
    }
}