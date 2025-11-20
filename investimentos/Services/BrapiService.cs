using System.Globalization;
using System.Text.Json;
using PlataformaInvestimentos.Interfaces;
using PlataformaInvestimentos.Models;

namespace PlataformaInvestimentos.Services;

public class BrapiService : IBrapi
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public static readonly string TodasAsCriptos = string.Join(",", new[]
    {
        "BTC", "ETH", "ADA", "BNB", "USDT", "XRP", "HEX", "DOGE", "SOL", "SOL1", "USDC", "DOT1", "UNI3", "LUNA1",
        "BCH", "LTC", "LINK", "ICP1", "MATIC", "AVAX", "ETC", "XLM", "VET", "FIL", "THETA", "TRX", "XMR", "XTZ",
        "EOS", "AAVE", "ATOM1", "GRT2", "CRO", "NEO", "BSV", "ALGO", "MKR", "MIOTA", "SHIB", "BTT1", "EGLD", "WAVES",
        "KSM", "AMP1", "CTC1", "HBAR", "DASH", "DCR", "QNT", "COMP", "HNT1", "RUNE", "CHZ", "HOT1", "ZEC", "CCXX",
        "ENJ", "MANA", "STX1", "XEM", "TFUEL", "XDC", "SUSHI", "AR", "BTG", "TUSD", "YFI", "CEL", "SNX", "ZIL", "QTUM",
        "RVN", "CELO", "BAT", "ONE2", "SC", "ZEN", "BNT", "DGB", "ONT", "OMG", "ZRX", "ICX", "CRV", "NANO", "SAND",
        "DFI", "UMA", "ANKR", "XWC", "LRC", "VGX", "IOTX", "IOST", "ARRR", "KAVA", "RSR", "WAXP", "ERG", "LSK", "BCD",
        "GLM", "STORJ", "DAG", "XVG", "FET", "VTHO", "GNO", "MED", "CKB", "SRM", "RLC", "FUN", "ACH", "SNT", "MIR1",
        "BAND", "COTI", "EWT", "NKN", "REP", "STRAX", "OXT", "TOMO", "ARDR", "CVC", "ETN", "HIVE", "STEEM", "TWT",
        "NU", "PHA", "MAID", "MLN", "SAPP", "ANT", "KIN", "BAL", "MTL", "AVA", "ARK", "BTS", "WAN", "VRA", "META",
        "MCO", "SYS", "DERO", "ABBC", "KMD", "PPT", "IRIS", "WOZX", "XNC", "KDA", "VLX", "BTM", "DNT", "DIVI", "GAS",
        "HNS", "MONA", "RBTC", "NYE", "PAC", "CRU", "DMCH", "FIRO", "AION", "RDD", "TT", "BEPRO", "FIO", "NRG", "GRS",
        "MARO", "WTC", "BCN", "ELA", "ADX", "SBD", "MASS", "BEAM", "XHV", "NULS", "REV", "ZNN", "SERO", "NXS", "AXEL",
        "CET", "PIVX", "DGD", "APL", "GXC", "VSYS", "AE", "ATRI", "PCX", "DNA1", "VITE", "FO", "LOKI", "MWC", "CTXC",
        "HC", "GO", "NIM", "PAI", "SRK", "KRT", "MHC", "VTC", "FSN", "NAV", "WICC", "VERI", "SOLVE", "ZEL", "SKY",
        "CUT", "ZANO", "NAS", "VAL1", "GRIN", "XSN", "NEBL", "QASH", "PPC", "WABI", "GAME", "PZM", "NMC", "SALT",
        "LBC", "GBYTE", "ETP", "NXT", "RSTR", "PART", "ADK", "FCT", "OBSR", "MAN", "QRL", "BIP", "DCN", "BHD", "DTEP",
        "PAY", "NVT", "TRUE", "ACT", "UBQ", "TRTL", "BHP", "EMC2", "NLG", "AEON", "BTC2", "XDN", "BLOCK", "POA",
        "CHI", "PLC", "CMT1", "DMD", "YOYOW", "GHOST1", "HPB", "SMART", "MRX", "HTDF", "AMB", "LCC", "SCC3", "WGR",
        "HTML", "PI", "XMC", "NYZO", "XMY", "INT", "VIA", "RINGX", "IDNA", "SFT", "ZYN", "FLO", "VEX", "FTC", "WINGS",
        "BTX", "DYN", "QRK", "INSTAR", "USNBT", "BLK", "XST", "MIR", "TERA"
    });

    public BrapiService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<Brapi?> ObterAtivo(string ticker)
    {
        var token = _config["Brapi:Token"];

        var url = $"https://brapi.dev/api/quote/{ticker}?token={token}";

        var response = await _httpClient.GetAsync(url);

        var jsonParaString = await response.Content.ReadAsStringAsync();
        using var documentoJson = JsonDocument.Parse(jsonParaString);
        var ativoJson = documentoJson.RootElement.GetProperty("results")[0];

        return new Brapi
        {
            Ticker = ativoJson.GetProperty("symbol").GetString() ?? string.Empty,
            Nome = ativoJson.GetProperty("longName").GetString() ?? string.Empty,
            Logo = ativoJson.GetProperty("logourl").GetString() ?? string.Empty,
            PrecoAtual = ativoJson.GetProperty("regularMarketPrice").GetDecimal()
        };
    }

    public async Task<List<Brapi>> ObterTodasAcoes()
    {
        var token = _config["Brapi:Token"];
        var url = $"https://brapi.dev/api/quote/list?token={token}&type=stock";

        var response = await _httpClient.GetAsync(url);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var resultado = new List<Brapi>();

        if (doc.RootElement.TryGetProperty("stocks", out var stocks) && stocks.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in stocks.EnumerateArray())
            {
                resultado.Add(new Brapi
                {
                    Ticker = item.GetProperty("stock").GetString(),
                    Nome = item.GetProperty("name").GetString(),
                    Logo = item.TryGetProperty("logo", out var logoProp) && logoProp.ValueKind == JsonValueKind.String
                        ? logoProp.GetString()
                        : ""
                });
            }
        }

        return resultado;
    }

    public async Task<List<Brapi>> ObterTodosFundos()
    {
        var token = _config["Brapi:Token"];
        var url = $"https://brapi.dev/api/quote/list?token={token}&type=fund";

        var response = await _httpClient.GetAsync(url);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var resultado = new List<Brapi>();

        if (doc.RootElement.TryGetProperty("stocks", out var stocks) && stocks.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in stocks.EnumerateArray())
            {
                resultado.Add(new Brapi
                {
                    Ticker = item.GetProperty("stock").GetString(),
                    Nome = item.GetProperty("name").GetString(),
                    Logo = item.TryGetProperty("logo", out var logoProp) && logoProp.ValueKind == JsonValueKind.String
                        ? logoProp.GetString()
                        : ""
                });
            }
        }

        return resultado;
    }

    public async Task<List<Brapi>> ObterTodasCripto()
    {
        var token = _config["Brapi:Token"];
        var resultado = new List<Brapi>();

        var todasAsMoedas = TodasAsCriptos.Split(',').Select(m => m.Trim()).ToList();
        var lotes = todasAsMoedas.Chunk(10);

        var tasks = lotes.Select(async lote =>
        {
            var moedas = string.Join(",", lote);
            var url = $"https://brapi.dev/api/v2/crypto?coin={moedas}&token={token}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode(); // lança exceção se falhar

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var list = new List<Brapi>();

            if (doc.RootElement.TryGetProperty("coins", out var coins) && coins.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in coins.EnumerateArray())
                {
                    if (!item.TryGetProperty("coin", out var coinProp) ||
                        !item.TryGetProperty("coinName", out var nameProp))
                        continue;

                    list.Add(new Brapi
                    {
                        Ticker = coinProp.GetString(),
                        Nome = nameProp.GetString()
                    });
                }
            }

            return list;
        });

        var resultados = await Task.WhenAll(tasks);

        // Flatten: combinar todas as listas em uma
        return resultados.SelectMany(r => r).ToList();
    }


    public async Task<Brapi?> ObterCripto(string ticker)
    {
        var token = _config["Brapi:Token"];
        var url = $"https://brapi.dev/api/v2/crypto?coin={ticker}&currency=BRL&token={token}";

        var response = await _httpClient.GetAsync(url);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("coins", out var coins) || coins.ValueKind != JsonValueKind.Array ||
            coins.GetArrayLength() == 0)
            return null;

        var cripto = coins[0];

        return new Brapi
        {
            Ticker = cripto.GetProperty("coin").GetString() ?? string.Empty,
            Nome = cripto.GetProperty("coinName").GetString() ?? string.Empty,
            PrecoAtual = cripto.TryGetProperty("regularMarketPrice", out var preco) &&
                         preco.TryGetDecimal(out var precoDec)
                ? precoDec
                : 0
        };
    }

    public async Task<decimal?> ObterSelicAtual()
    {
        var token = _config["Brapi:Token"];
        var url = $"https://brapi.dev/api/v2/prime-rate?country=brazil&token={token}";

        var response = await _httpClient.GetAsync(url);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("prime-rate", out JsonElement primeRateArray) &&
            primeRateArray.ValueKind == JsonValueKind.Array &&
            primeRateArray.GetArrayLength() > 0)
        {
            var taxaElement = primeRateArray[0];

            if (taxaElement.TryGetProperty("value", out JsonElement valueElement))
            {
                var valorString = valueElement.GetString();

                if (decimal.TryParse(valorString, NumberStyles.Any, CultureInfo.InvariantCulture,
                        out decimal taxaDecimal))
                {
                    return taxaDecimal;
                }
            }
        }

        return null;
    }
}