using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaInvestimentos.Models;

namespace PlataformaInvestimentos.Controllers;

[Authorize]
public class HomeController : UtilController
{
    private readonly ILogger<HomeController> _logger;
    private readonly Context _context;

    public HomeController(ILogger<HomeController> logger, Context context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var usuarioId = ObterUsuarioId();
        
        var ultimosLanc = await _context.Lancamento
            .Where(l => l.UsuarioId == usuarioId)
            .OrderByDescending(l => l.Data)
            .Take(5)
            .Select(l => new { l.Movimentacao, l.Produto, l.ValorTotal, l.Data })
            .ToListAsync();

        ViewBag.UltimosLanc = ultimosLanc;
        
        var anoAtual = DateTime.Today.Year;

        var lancs = await _context.Lancamento
            .Where(l => l.UsuarioId == usuarioId && l.Data.Year == anoAtual)
            .Select(l => new { l.Data, l.Movimentacao, l.ValorTotal })
            .ToListAsync();
        
        var somaMes = new decimal[12]; 
        foreach (var l in lancs)
        {
            int m = l.Data.Month - 1;
            if (l.Movimentacao.Equals("Compra", StringComparison.OrdinalIgnoreCase))
                somaMes[m] += l.ValorTotal;
            else if (l.Movimentacao.Equals("Venda", StringComparison.OrdinalIgnoreCase))
                somaMes[m] -= l.ValorTotal;
        }
        
        // Descobre meses com movimento
        int firstMov = -1, lastMov = -1;
        for (int i = 0; i < 12; i++)
        {
            if (somaMes[i] != 0m)
            {
                if (firstMov == -1) firstMov = i;
                lastMov = i;
            }
        }
        
        var evoData = new double?[12];
        decimal acumulado = 0m;
        bool teveMovimento = false;

        for (int i = 0; i < 12; i++)
        {
            acumulado += somaMes[i];

            if (acumulado != 0m || somaMes[i] != 0m)
                teveMovimento = true;

            // Mostra pontos só depois do primeiro movimento real
            evoData[i] = (teveMovimento ? (double?)acumulado : null);
        }

        ViewBag.EvoData = evoData;
        
        var totalAcoes = await _context.Acao
            .Where(a => a.UsuarioId == usuarioId)
            .SumAsync(a => (decimal?)a.PrecoAtual) ?? 0m;

        var totalFiis = await _context.FundoImobiliario
            .Where(f => f.UsuarioId == usuarioId)
            .SumAsync(f => (decimal?)f.PrecoAtual) ?? 0m;

        var totalCriptos = await _context.Criptomoeda
            .Where(c => c.UsuarioId == usuarioId)
            .SumAsync(c => (decimal?)c.PrecoAtual) ?? 0m;

        var totalRendaFixa = await _context.RendaFixa
            .Where(r => r.UsuarioId == usuarioId)
            .SumAsync(r => (decimal?)r.Valor) ?? 0m;

        var labels = new[] { "Ações", "FIIs", "Criptos", "Renda Fixa" };
        var data = new[] { totalAcoes, totalFiis, totalCriptos, totalRendaFixa };
        var totalGeral = data.Sum();

        ViewBag.ChartLabels = labels;
        ViewBag.ChartData = data;
        ViewBag.TotalGeral = totalGeral;

        ViewBag.ValorAcoes = totalAcoes;
        ViewBag.ValorFiis = totalFiis;
        ViewBag.ValorCriptos = totalCriptos;
        ViewBag.ValorRendaFixa = totalRendaFixa;
        
        var qtdAcoes = await _context.Acao.CountAsync(a => a.UsuarioId == usuarioId);
        var qtdFiis = await _context.FundoImobiliario.CountAsync(f => f.UsuarioId == usuarioId);
        var qtdCriptos = await _context.Criptomoeda.CountAsync(c => c.UsuarioId == usuarioId);
        var qtdRendaFixa = await _context.RendaFixa.CountAsync(r => r.UsuarioId == usuarioId);
        
        var quantidadesPorTipo = new Dictionary<string, int>
        {
            { "Ações", qtdAcoes },
            { "FIIs", qtdFiis },
            { "Criptomoedas", qtdCriptos },
            { "Renda Fixa", qtdRendaFixa }
        };
        ViewBag.QuantidadesPorTipo = quantidadesPorTipo;

        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}