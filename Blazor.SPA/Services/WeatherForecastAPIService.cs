/// =================================
/// Author: Shaun Curtis, Cold Elm Coders
/// License: MIT
/// ==================================

using Blazor.SPA.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;

namespace Blazor.SPA.Services
{
    public class WeatherForecastAPIService : IWeatherForecastService
    {
        protected HttpClient HttpClient { get; set; }

        public WeatherForecastAPIService(HttpClient httpClient)
            => this.HttpClient = httpClient;

        public async Task<List<WeatherForecast>> GetRecordsAsync()
            => await this.HttpClient.GetFromJsonAsync<List<WeatherForecast>>($"/api/weatherforecast/list");

        public WeatherForecast Record { get; set; } = new WeatherForecast();

        public int FormStep { get; set; } = 1;


    }
}
