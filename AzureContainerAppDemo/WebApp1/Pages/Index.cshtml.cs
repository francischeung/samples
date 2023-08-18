using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApp1.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IConfiguration _config;

        public string WeatherForecast { get; set; }

        public IndexModel(ILogger<IndexModel> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
            WeatherForecast = string.Empty;
        }

        public async Task OnGet()
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(_config["WebAPIBaseAddress"]);
            this.WeatherForecast = await httpClient.GetStringAsync("/weatherforecast");
        }
    }
}