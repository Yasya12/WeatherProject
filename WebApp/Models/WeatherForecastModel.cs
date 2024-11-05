namespace WebApp.Models;

public class WeatherForecastModel
{
    public string City { get; set; }
    public int Temperature { get; set; }
    public int TemperatureMin { get; set; }
    public int TemperatureMax { get; set; }
    public double Precipitation { get; set; }
}