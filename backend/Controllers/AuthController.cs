using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace LiberatiStampe.API.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly string _apiKey;
    private readonly string _apiSecret;
    private readonly string _scopes;
    private readonly string _hostUrl;
    private readonly HttpClient _httpClient;

    public AuthController(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _apiKey = config["Shopify:ApiKey"]!;
        _apiSecret = config["Shopify:ApiSecret"]!;
        _scopes = config["Shopify:Scopes"]!;
        _hostUrl = config["Shopify:HostUrl"]!;
        _httpClient = httpClientFactory.CreateClient();
    }

    [HttpGet("install")]
    public IActionResult Install([FromQuery] string shop)
    {
        if (string.IsNullOrEmpty(shop))
            return BadRequest("Parametro 'shop' mancante.");

        var redirectUrl = Uri.EscapeDataString($"{_hostUrl}/auth/callback");
        var scopesEncoded = Uri.EscapeDataString(_scopes);

        // Aggiunto access_mode=offline per token permanente
        var authUrl = $"https://{shop}/admin/oauth/authorize?client_id={_apiKey}&scope={scopesEncoded}&redirect_uri={redirectUrl}&grant_options[]=offline";

        return Redirect(authUrl);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string code,
        [FromQuery] string shop,
        [FromQuery] string hmac)
    {
        if (!IsValidHmac(hmac))
            return Unauthorized("HMAC non valido.");

        var tokenUrl = $"https://{shop}/admin/oauth/access_token";
        var payload = new FormUrlEncodedContent(new[]
        {
        new KeyValuePair<string, string>("client_id", _apiKey),
        new KeyValuePair<string, string>("client_secret", _apiSecret),
        new KeyValuePair<string, string>("code", code)
    });

        var response = await _httpClient.PostAsync(tokenUrl, payload);
        if (!response.IsSuccessStatusCode)
            return StatusCode(500, "Errore nel recupero del token.");

        var json = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        var accessToken = json!["access_token"];

        // Redirect diretto ad Angular con il token — senza passare per sessione
        return Redirect($"{_hostUrl}?shop={shop}&token={accessToken}");
    }

    [HttpGet("token")]
    public IActionResult GetToken()
    {
        var token = HttpContext.Session.GetString("ShopifyAccessToken");
        var shop = HttpContext.Session.GetString("ShopifyShop");

        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        return Ok(new { shop, token });
    }

    [HttpGet("/app")]
    public IActionResult App([FromQuery] string shop)
    {
        var token = HttpContext.Session.GetString("ShopifyAccessToken");
        return Redirect($"{_hostUrl}?shop={shop}&token={token}");
    }

    private bool IsValidHmac(string hmac)
    {
        // Ricostruisce la query string senza hmac e verifica la firma
        var queryParams = Request.Query
            .Where(q => q.Key != "hmac")
            .OrderBy(q => q.Key)
            .Select(q => $"{q.Key}={q.Value}")
            .ToList();

        var message = string.Join("&", queryParams);
        using var hmacSha256 = new HMACSHA256(Encoding.UTF8.GetBytes(_apiSecret));
        var hash = hmacSha256.ComputeHash(Encoding.UTF8.GetBytes(message));
        var computed = Convert.ToHexString(hash).ToLower();

        return computed == hmac;
    }
}
