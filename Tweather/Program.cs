// Tweather Bot, Mikołaj Henzel, 2022
// WIEM, ŻE KLUCZE API NIE POWINNY BYĆ WIDOCZNE W KODZIE (ale tak jest najłatwiej zademonstrować działanie)
using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;
using Tweetinvi.Core.Web;
using OpenWeatherAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tweather
{
    class Program
    {
        // Zamienianie dwuliterowej nazwy państwa na emoji flagi tego państwa na Twitterze
        private static string ToEmoji(string country)
        {
            return char.ConvertFromUtf32(country[0] + 0x1F1A5) + char.ConvertFromUtf32(country[1] + 0x1F1A5);
        }

        public class TweetsV2Poster
        {
            private readonly ITwitterClient client;

            public TweetsV2Poster(ITwitterClient client)
            {
                this.client = client;
            }

            // Odkąd Twitter wypuścił API 2.0, ciężko jest opierać się wyłącznie na predefiniowanych funkcjach z Tweetinvi, dlatego trzeba tworzyć własne

            public Task<ITwitterResult> PostTweet(TweetV2PostRequest tweetParams)
            {
                return this.client.Execute.AdvanceRequestAsync(
                    (ITwitterRequest request) =>
                    {
                        var jsonBody = this.client.Json.Serialize(tweetParams);

                        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                        request.Query.Url = "https://api.twitter.com/2/tweets";
                        request.Query.HttpMethod = Tweetinvi.Models.HttpMethod.POST;
                        request.Query.HttpContent = content;
                    }
                );
            }
        }
       
        public class TweetV2PostRequest
        {
            // Treść Tweeta

            [JsonProperty("text")]
            public string Text { get; set; } = string.Empty;
        }

        static async Task Main(string[] args)
        {
            // Twitter:
            // Klucze umożliwiające automatyczne publikowanie Tweetów
            string APIKey = "4I5zRtlWdmGq4muNtRGqs4P0S";
            string APIKeySecret = "19IdB3w54W4epldNC8hQBxKMZySYe8qyceuYa0YIL7wjnW8LVI";
            string AccessToken = "1595227350588952577-0hNeSI0kGy9mXnQ0moZ59oAwrmzT3n";
            string AccessTokenSecret = "qUIAQhvIAPspIr1HD3EfMd9GleQd5bQd3dze0mhrZZaBz";

            Console.WriteLine("Witaj w Tweather!");
            Console.WriteLine();
            Console.WriteLine("Podaj swoją nazwę na Twitterze (np. @mikolajhenzel): ");
            string user = Console.ReadLine();

            Console.WriteLine();
            Console.WriteLine("Dla jakiego miasta chcesz zobaczyć pogodę (podaj angielską nazwę, np. Warsaw): ");
            string city = Console.ReadLine();
            Console.WriteLine();

            // OpenWeatherMap API:
            // Łączenie się z serwisem przez klucz API
            var client = new OpenWeatherAPI.OpenWeatherApiClient("fcc5dae6f6220576a30a8ef74bb9e146");
            Console.WriteLine("Pobieranie danych...");
            Console.WriteLine();

            // Pobieranie aktualnych informacji o pogodzie dla podanego miasta
            var query = await client.QueryAsync(city);

            string message = $"Cześć {user} 👋, oto wygenerowane dla Ciebie aktualne ({DateTime.Now.ToString("dd-MM-yyyy, HH:mm:ss")}) dane pogodowe dla miasta {query.Name}, {ToEmoji(query.Sys.Country)}:\n\n🌡 Temperatura: {Math.Round(query.Main.Temperature.CelsiusCurrent / 100 - 270.15)}°C\n💨 Wiatr: {query.Wind.SpeedMetersPerSecond / 100}m/s\n💦 Wilgotność: {query.Main.Humidity}%\n📈 Ciśnienie: {query.Main.Pressure}hPa\n☀⬆ Wschód słońca: {query.Sys.Sunrise.ToString().Substring(11)}\n☀⬇ Zachód słońca: {query.Sys.Sunset.ToString().Substring(11)}";

            Console.WriteLine(message);

            Console.WriteLine("Przesyłanie Tweeta...");
            Console.WriteLine();

            // Ustanawianie klienta Twittera
            TwitterClient userClient = new TwitterClient(APIKey, APIKeySecret, AccessToken, AccessTokenSecret);

            var poster = new TweetsV2Poster(userClient);

            // Publikowanie Tweeta
            ITwitterResult result = await poster.PostTweet(
                new TweetV2PostRequest
                {
                    Text = message
                }
            );

            if(result.Response.IsSuccessStatusCode==false)
            {
                throw new Exception(
                    "Błąd podczas publikowania Tweeta: " + Environment.NewLine + result.Content
                );
            }

            dynamic tweetData = JObject.Parse(result.Content);
            string tweetUrl = "https://twitter.com/tweather_henzel/status/" + tweetData.data.id;

            Console.WriteLine();
            Console.WriteLine($"Pomyślnie przesłano Tweet: {tweetUrl}");
        }
    }
}
