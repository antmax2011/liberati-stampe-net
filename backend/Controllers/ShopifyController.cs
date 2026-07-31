using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace LiberatiStampe.API.Controllers;

[ApiController]
[Route("[controller]")]
public class ShopifyController : ControllerBase
{
    private readonly HttpClient _httpClient;

    public ShopifyController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders(
        [FromQuery] string period = "today",
        [FromQuery] string shop = "",
        [FromQuery] string token = "")
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(shop))
            return Unauthorized();

        var now = DateTime.UtcNow;
        var startDate = period switch
        {
            "2h" => now.AddHours(-2),
            "24h" => now.AddHours(-24),
            _ => new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc)
        };

        var createdAtMin = Uri.EscapeDataString(startDate.ToString("o"));
        var url = $"https://{shop}/admin/api/2026-07/orders.json?status=any&limit=250&created_at_min={createdAtMin}&fields=id,name,customer,shipping_address,line_items,fulfillment_status";

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("X-Shopify-Access-Token", token);

        var response = await _httpClient.GetAsync(url);

        // LOG risposta completa di Shopify
        var body = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"=== SHOPIFY RESPONSE ===");
        Console.WriteLine($"Status: {response.StatusCode}");
        Console.WriteLine($"Body: {body}");
        Console.WriteLine($"Token usato: {token[..15]}...");

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, body);

        return Content(body, "application/json");
    }
}
