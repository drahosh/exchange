using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Web;
using System.Web.Http;
using WebApplication1.Models;

namespace WebApplication1
{
    public static class Utils
    {
        public static bool VerifyHeaders(IHeaderDictionary headers, User user)
        {
            if (user == null)
            {
                return false;
            }
            StringValues token;
            return headers.TryGetValue("token", out token)
                && token.Count() == 1
                && token.ToString() == user.token;
        }
        public static User GetUserFromToken(Context context, IHeaderDictionary headers)
        {
            StringValues token;
            headers.TryGetValue("token", out token);
            if (token == default(StringValues)){
                return null;
            }
            return context.Users.Where(u => u.token == token.ToString()).FirstOrDefault();
        }

        public static decimal GetBitcoinPrice(long amount)
        {
            if (amount == 0)
            {
                return 0;
            }
            string api_key = "ea3c0e02-7145-4552-88d2-8c81354701de";

            var URL = new UriBuilder("https://pro-api.coinmarketcap.com/v1/tools/price-conversion");

            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["symbol"] = "BTC";
            queryString["amount"] = amount.ToString();
            queryString["convert"] = "USD";

            URL.Query = queryString.ToString();

            var client = new WebClient();
            client.Headers.Add("X-CMC_PRO_API_KEY", api_key);
            client.Headers.Add("Accepts", "application/json");
            try
            {
                string result = client.DownloadString(URL.ToString());
                JsonElement root = JsonDocument.Parse(result).RootElement;
                return root.GetProperty("data").GetProperty("quote").GetProperty("USD").GetProperty("price").GetDecimal();
            }
            catch (WebException e)
            {   
                Console.WriteLine(e.Message);
            }
            return 0;

        }
    }
}
