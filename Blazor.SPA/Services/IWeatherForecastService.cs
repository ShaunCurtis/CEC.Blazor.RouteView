/// =================================
/// Author: Shaun Curtis, Cold Elm Coders
/// License: MIT
/// ==================================

using Blazor.SPA.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blazor.SPA.Services
{
    public interface IWeatherForecastService
    {
        public Task<List<WeatherForecast>> GetRecordsAsync();

        public WeatherForecast Record { get; set; }

        public int FormStep { get; set; }
    }
}
