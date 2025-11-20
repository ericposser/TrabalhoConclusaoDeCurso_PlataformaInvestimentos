using System;
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
    public class FundoImobiliarioController : UtilController
    {
        private readonly Context _context;

        public FundoImobiliarioController(Context context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string sortOrder)
        {
            var usuarioId = ObterUsuarioId();
            var fundos = _context.FundoImobiliario.Where(f => f.UsuarioId == usuarioId);

            ViewBag.TickerSort = string.IsNullOrEmpty(sortOrder) ? "ticker_desc" : "";
            ViewBag.NomeSort = sortOrder == "Nome" ? "nome_desc" : "Nome";
            ViewBag.PrecoSort = sortOrder == "Preco" ? "preco_desc" : "Preco";
            ViewBag.QuantidadeSort = sortOrder == "Quantidade" ? "quantidade_desc" : "Quantidade";
            ViewBag.DataCompraSort = sortOrder == "DataCompra" ? "data_desc" : "DataCompra";

            fundos = sortOrder switch
            {
                "ticker_desc" => fundos.OrderByDescending(f => f.Ticker),
                "Nome" => fundos.OrderBy(f => f.Nome),
                "nome_desc" => fundos.OrderByDescending(f => f.Nome),
                "Preco" => fundos.OrderBy(f => f.PrecoAtual),
                "preco_desc" => fundos.OrderByDescending(f => f.PrecoAtual),
                "Quantidade" => fundos.OrderBy(f => f.Quantidade),
                "quantidade_desc" => fundos.OrderByDescending(f => f.Quantidade),
                "DataCompra" => fundos.OrderBy(f => f.DataCompra),
                "data_desc" => fundos.OrderByDescending(f => f.DataCompra),
                _ => fundos.OrderBy(f => f.Ticker),
            };

            return View(await fundos.ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> ListarTodosFundos([FromServices] IBrapi brapi)
        {
            var fundos = await brapi.ObterTodosFundos();
            var result = fundos.Select(f => new
            {
                id = f.Ticker,
                text = $"{f.Ticker} - {f.Nome}",
                logo = f.Logo
            });
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> BuscarAtivo(string fundo, [FromServices] IBrapi brapi)
        {
            if (string.IsNullOrWhiteSpace(fundo))
                return BadRequest("Fundo é obrigatório");

            var ativo = await brapi.ObterAtivo(fundo);
            if (ativo == null)
                return NotFound(new { mensagem = "Fundo não encontrado." });

            return Json(new
            {
                nome = ativo.Nome,
                precoAtual = ativo.PrecoAtual
            });
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Ticker,Quantidade,PrecoMedio,DataCompra,PrecoAtual")]
            FundoImobiliario novoFundo,
            [FromServices] IBrapi brapi)
        {
            var valido = ValidarFundo(novoFundo);
            if (!valido.valido)
            {
                ModelState.AddModelError(string.Empty, valido.mensagemErro);
                return View(novoFundo);
            }

            var usuarioId = ObterUsuarioId();
            var ativo = await brapi.ObterAtivo(novoFundo.Ticker);

            var fundoExistente = await _context.FundoImobiliario
                .FirstOrDefaultAsync(f => f.Ticker == novoFundo.Ticker && f.UsuarioId == usuarioId);

            AtualizarOuAdicionarFundo(fundoExistente, novoFundo, ativo, usuarioId);

            AdicionarLancamento("Compra", novoFundo.Ticker, novoFundo.Quantidade,
                novoFundo.PrecoAtual, novoFundo.DataCompra, usuarioId);

            await _context.SaveChangesAsync();
            TempData["Sucesso"] = true;
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            var usuarioId = ObterUsuarioId();
            if (id == null)
                return NotFound();

            var fundo = await _context.FundoImobiliario.FirstOrDefaultAsync(f =>
                f.Id == id && f.UsuarioId == usuarioId);
            if (fundo == null)
                return NotFound();

            return View(fundo);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, int quantidade, decimal preco)
        {
            var usuarioId = ObterUsuarioId();
            var fundo = await _context.FundoImobiliario.FirstOrDefaultAsync(f =>
                f.Id == id && f.UsuarioId == usuarioId);

            if (fundo == null)
                return NotFound();

            var valorDaVenda = quantidade * preco;
            AdicionarLancamento("Venda", fundo.Ticker, quantidade, preco, DateTime.Now, usuarioId);

            if (quantidade >= fundo.Quantidade)
            {
                _context.FundoImobiliario.Remove(fundo);
            }
            else
            {
                if (valorDaVenda > fundo.PrecoAtual)
                {
                    ModelState.AddModelError(string.Empty,
                        "O valor da venda não pode ser maior que o valor total do seu ativo");
                    return RedirectToAction(nameof(Index));
                }

                fundo.Quantidade -= quantidade;
                fundo.PrecoAtual -= valorDaVenda;
                _context.FundoImobiliario.Update(fundo);
            }

            await _context.SaveChangesAsync();
            TempData["Removido"] = true;
            return RedirectToAction(nameof(Index));
        }

        // Métodos auxiliares privados

        private (bool valido, string mensagemErro) ValidarFundo(FundoImobiliario fundo)
        {
            if (fundo.Quantidade <= 0) return (false, "A quantidade deve ser maior que zero.");
            if (fundo.PrecoAtual <= 0) return (false, "O valor deve ser maior que zero.");
            // if (fundo.DataCompra.Date > DateTime.Today || fundo.DataCompra == DateTime.MinValue)
            //     return (false, "Selecione uma data válida.");
            return (true, string.Empty);
        }


        private void AtualizarOuAdicionarFundo(FundoImobiliario existente, FundoImobiliario novoFundo, dynamic ativo,
            int usuarioId)
        {
            var valorNovaCompra = novoFundo.Quantidade * novoFundo.PrecoAtual;
            if (existente != null)
            {
                existente.Quantidade += novoFundo.Quantidade;
                existente.PrecoAtual += valorNovaCompra;
                existente.DataCompra = novoFundo.DataCompra;
                existente.Logo = ativo.Logo;
                existente.Nome = ativo.Nome;
                _context.Update(existente);
            }
            else
            {
                novoFundo.UsuarioId = usuarioId;
                novoFundo.Nome = ativo.Nome;
                novoFundo.Logo = ativo.Logo;
                novoFundo.PrecoAtual = valorNovaCompra;
                _context.Add(novoFundo);
            }
        }

        private void AdicionarLancamento(string movimentacao, string produto, decimal quantidade, decimal valorTotal,
            DateTime data, int usuarioId)
        {
            var lancamento = new Lancamento
            {
                Movimentacao = movimentacao,
                Produto = produto,
                Quantidade = quantidade,
                ValorTotal = valorTotal,
                Data = data,
                UsuarioId = usuarioId
            };
            _context.Lancamento.Add(lancamento);
        }
    }
}