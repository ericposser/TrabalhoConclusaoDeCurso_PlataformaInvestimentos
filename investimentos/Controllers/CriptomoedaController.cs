using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaInvestimentos.Interfaces;
using PlataformaInvestimentos.Models;

namespace PlataformaInvestimentos.Controllers
{
    [Authorize]
    public class CriptomoedaController : UtilController
    {
        private readonly Context _context;

        public CriptomoedaController(Context context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string sortOrder)
        {
            var usuarioId = ObterUsuarioId();
            var criptos = _context.Criptomoeda.Where(c => c.UsuarioId == usuarioId);

            ViewBag.TickerSort = string.IsNullOrEmpty(sortOrder) ? "ticker_desc" : "";
            ViewBag.NomeSort = sortOrder == "Nome" ? "nome_desc" : "Nome";
            ViewBag.PrecoSort = sortOrder == "Preco" ? "preco_desc" : "Preco";
            ViewBag.QuantidadeSort = sortOrder == "Quantidade" ? "quantidade_desc" : "Quantidade";

            criptos = sortOrder switch
            {
                "ticker_desc" => criptos.OrderByDescending(c => c.Ticker),
                "Nome" => criptos.OrderBy(c => c.Nome),
                "nome_desc" => criptos.OrderByDescending(c => c.Nome),
                "Preco" => criptos.OrderBy(c => c.PrecoAtual),
                "preco_desc" => criptos.OrderByDescending(c => c.PrecoAtual),
                "Quantidade" => criptos.OrderBy(c => c.Quantidade),
                "quantidade_desc" => criptos.OrderByDescending(c => c.Quantidade),
                _ => criptos.OrderBy(c => c.Ticker),
            };

            return View(await criptos.ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> ListarTodasCriptos([FromServices] IBrapi brapi)
        {
            var criptos = await brapi.ObterTodasCripto();
            var result = criptos.Select(c => new
            {
                id = c.Ticker,
                text = $"{c.Ticker} - {c.Nome}",
                logo = c.Logo
            });
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> BuscarCripto(string cripto, [FromServices] IBrapi brapi)
        {
            var criptomoeda = await brapi.ObterCripto(cripto);
            if (criptomoeda is null)
                return NotFound(new { mensagem = "Criptomoeda não encontrada." });

            return Json(new
            {
                nome = criptomoeda.Nome,
                precoAtual = criptomoeda.PrecoAtual
            });
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string Ticker, string Quantidade, string PrecoAtual,
            DateTime DataCompra, [FromServices] IBrapi brapi)
        {
            var culture = CultureInfo.GetCultureInfo("en-US");
            if (!TryParseDecimal(Quantidade, culture, out var quantidadeParsed) ||
                !TryParseDecimal(PrecoAtual, culture, out var precoParsed))
            {
                ViewBag.Erro = "Quantidade ou preço inválidos.";
                return View();
            }

            var novaCripto = new Criptomoeda
            {
                Ticker = Ticker,
                Quantidade = quantidadeParsed,
                PrecoAtual = precoParsed,
                DataCompra = DataCompra
            };


            var valido = ValidarCripto(novaCripto);
            if (!valido.valido)
            {
                ModelState.AddModelError(string.Empty, valido.mensagemErro);
                return View(novaCripto);
            }

            var usuarioId = ObterUsuarioId();
            var ativo = await brapi.ObterCripto(Ticker);

            var existente = await _context.Criptomoeda
                .FirstOrDefaultAsync(c => c.Ticker == Ticker && c.UsuarioId == usuarioId);

            if (existente != null)
            {
                existente.Quantidade += novaCripto.Quantidade;
                existente.PrecoAtual += novaCripto.PrecoAtual;
                existente.DataCompra = novaCripto.DataCompra;
                existente.Nome = ativo.Nome;
                _context.Update(existente);
            }
            else
            {
                novaCripto.UsuarioId = usuarioId;
                novaCripto.Nome = ativo.Nome;
                _context.Add(novaCripto);
            }

            var lancamento = new Lancamento
            {
                Movimentacao = "Compra",
                Produto = Ticker,
                Quantidade = novaCripto.Quantidade,
                ValorTotal = novaCripto.PrecoAtual,
                Data = DataCompra,
                UsuarioId = usuarioId
            };
            _context.Lancamento.Add(lancamento);

            await _context.SaveChangesAsync();
            TempData["Sucesso"] = true;
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            var usuarioId = ObterUsuarioId();
            if (id == null)
                return NotFound();

            var cripto = await _context.Criptomoeda.FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId);
            if (cripto == null)
                return NotFound();

            return View(cripto);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, string quantidade, string preco)
        {
            var invariantCulture = CultureInfo.InvariantCulture;

            if (!TryParseDecimal(quantidade, invariantCulture, out var quantidadeDecimal))
            {
                TempData["Erro"] = "O valor da quantidade é inválido.";
                return RedirectToAction(nameof(Index));
            }

            if (!TryParseDecimal(preco, invariantCulture, out var precoDecimal))
            {
                TempData["Erro"] = "O valor do preço é inválido.";
                return RedirectToAction(nameof(Index));
            }

            var usuarioId = ObterUsuarioId();
            var cripto = await _context.Criptomoeda.FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId);

            decimal valorDaVenda = quantidadeDecimal * precoDecimal;

            var lancamento = new Lancamento
            {
                Movimentacao = "Venda",
                Produto = cripto.Ticker,
                Quantidade = quantidadeDecimal,
                ValorTotal = precoDecimal,
                Data = DateTime.Now,
                UsuarioId = usuarioId
            };

            _context.Lancamento.Add(lancamento);

            if (quantidadeDecimal >= cripto.Quantidade)
            {
                _context.Criptomoeda.Remove(cripto);
            }
            else
            {
                if (valorDaVenda > cripto.PrecoAtual)
                {
                    TempData["Erro"] = "O valor da venda não pode ser maior que o valor total do seu ativo";
                    return RedirectToAction(nameof(Index));
                }

                cripto.Quantidade -= quantidadeDecimal;
                cripto.PrecoAtual -= valorDaVenda;
                _context.Criptomoeda.Update(cripto);
            }

            await _context.SaveChangesAsync();
            TempData["Removido"] = true;
            return RedirectToAction(nameof(Index));
        }

        // Métodos auxiliares privados

        private bool TryParseDecimal(string value, CultureInfo culture, out decimal result)
        {
            return decimal.TryParse(value.Replace(",", "."), NumberStyles.Any, culture, out result);
        }

        private (bool valido, string mensagemErro) ValidarCripto(Criptomoeda cripto)
        {
            if (cripto.Quantidade <= 0)
                return (false, "Selecione uma quantidade válida.");
            if (cripto.PrecoAtual <= 0)
                return (false, "O valor deve ser maior que zero.");
            // if (cripto.DataCompra.Date > DateTime.Today || cripto.DataCompra == DateTime.MinValue)
            //     return (false, "Selecione uma data válida.");
            return (true, string.Empty);
        }
    }
}