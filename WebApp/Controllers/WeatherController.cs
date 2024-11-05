using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using WebApp.Models;

namespace WebApp.Controllers
{
    public class WeatherController : Controller
    {
        private readonly string _apiKey = "D8AAdAqU7Ee8C8TE67Iy2lGgkowbFhVg"; //не треба так робити
        private readonly string _defaultCity = "Kyiv";
        private string _currentCity;
        private DateTime _lastWarningDate;

        public IActionResult Index()
        {
            _currentCity = TempData["LastCity"] as string ?? _defaultCity;
            return View(new WeatherForecastModel { City = _currentCity });
        }

        [HttpPost]
        public async Task<IActionResult> GetForecast(WeatherForecastModel model)
        {
            _currentCity = model.City;
            TempData["LastCity"] = _currentCity;

            try
            {
                var currentWeatherData = await GetCurrentWeatherDataAsync(_currentCity);
                var forecastData = await GetForecastDataAsync(_currentCity);

                if (currentWeatherData != null)
                {
                    model.Temperature = (int)currentWeatherData["Temperature"]["Metric"]["Value"];
                }

                if (forecastData != null)
                {
                    model.TemperatureMin = (int)forecastData["Temperature"]["Minimum"]["Value"];
                    model.TemperatureMax = (int)forecastData["Temperature"]["Maximum"]["Value"];
                    model.Precipitation = forecastData["Day"]["HasPrecipitation"].Value<bool>() ? 1 : 0;

                    if (model.Precipitation > 0 && (_lastWarningDate.Date != DateTime.Now.Date))
                    {
                        _lastWarningDate = DateTime.Now;
                        TempData["Warning"] = $"Rain is expected in {_currentCity} today!";
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                TempData["Warning"] = "Unable to retrieve weather data. Please check the city name and try again.";
                Debug.WriteLine($"HTTP Request Exception: {ex.Message}");
            }
            catch (Exception ex)
            {
                TempData["Warning"] = "An error occurred while processing your request. Please try again.";
                Debug.WriteLine($"General Exception: {ex.Message}");
            }

            return View("Index", model);
        }

        private async Task<JObject> GetCurrentWeatherDataAsync(string city)
        {
            string locationUrl = $"http://dataservice.accuweather.com/locations/v1/cities/search?apikey={_apiKey}&q={city}";
            
            using (var httpClient = new HttpClient())
            {
                var locationResponse = await httpClient.GetAsync(locationUrl);
                locationResponse.EnsureSuccessStatusCode();
                
                var locationData = JArray.Parse(await locationResponse.Content.ReadAsStringAsync());

                if (locationData.Count == 0)
                {
                    throw new Exception($"Location key not found for city: {city}");
                }

                string locationKey = locationData[0]["Key"].ToString();

                string currentConditionsUrl = $"http://dataservice.accuweather.com/currentconditions/v1/{locationKey}?apikey={_apiKey}&details=true";
                
                var currentConditionsResponse = await httpClient.GetAsync(currentConditionsUrl);
                currentConditionsResponse.EnsureSuccessStatusCode();

                var currentWeatherData = JArray.Parse(await currentConditionsResponse.Content.ReadAsStringAsync());
                return (JObject)currentWeatherData[0];
            }
        }

        private async Task<JObject> GetForecastDataAsync(string city)
        {
            string locationUrl = $"http://dataservice.accuweather.com/locations/v1/cities/search?apikey={_apiKey}&q={city}";
            
            using (var httpClient = new HttpClient())
            {
                var locationResponse = await httpClient.GetAsync(locationUrl);
                locationResponse.EnsureSuccessStatusCode();

                var locationData = JArray.Parse(await locationResponse.Content.ReadAsStringAsync());

                if (locationData.Count == 0)
                {
                    throw new Exception($"Location key not found for city: {city}");
                }

                string locationKey = locationData[0]["Key"].ToString();

                string forecastUrl = $"http://dataservice.accuweather.com/forecasts/v1/daily/1day/{locationKey}?apikey={_apiKey}&metric=true";
                
                var forecastResponse = await httpClient.GetAsync(forecastUrl);
                forecastResponse.EnsureSuccessStatusCode();

                var forecastData = JObject.Parse(await forecastResponse.Content.ReadAsStringAsync());
                return (JObject)forecastData["DailyForecasts"][0];
            }
        }
    }
}
