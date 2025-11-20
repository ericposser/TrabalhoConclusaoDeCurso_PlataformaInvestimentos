using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PlataformaInvestimentos.Interfaces;
using PlataformaInvestimentos.Models;

namespace PlataformaInvestimentos.Controllers
{
    [Authorize]
    public class AcaoController : UtilController
    {
        private readonly Context _context;
        private readonly IBrapi _brapi;

        public AcaoController(Context context, IBrapi brapi)
        {
            _context = context;
            _brapi = brapi;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string sortOrder)
        {
            var usuarioId = ObterUsuarioId();

            // Inicia a consulta no banco, filtrando apenas as ações que pertencem ao usuário logado.
            var acoes = _context.Acao.Where(a => a.UsuarioId == usuarioId);

            // Prepara as ViewBags que serão usadas nos links de cabeçalho da tabela na View.
            // Isso permite inverter a ordem (ascendente/descendente) a cada clique.
            ViewBag.TickerSort = string.IsNullOrEmpty(sortOrder) ? "ticker_desc" : "";
            ViewBag.NomeSort = sortOrder == "Nome" ? "nome_desc" : "Nome";
            ViewBag.PrecoSort = sortOrder == "Preco" ? "preco_desc" : "Preco";
            ViewBag.QuantidadeSort = sortOrder == "Quantidade" ? "quantidade_desc" : "Quantidade";

            // O 'switch' aplica a ordenação desejada à consulta do Entity Framework.
            acoes = sortOrder switch
            {
                "ticker_desc" => acoes.OrderByDescending(a => a.Ticker),
                "Nome" => acoes.OrderBy(a => a.Nome),
                "nome_desc" => acoes.OrderByDescending(a => a.Nome),
                "Preco" => acoes.OrderBy(a => a.PrecoAtual),
                "preco_desc" => acoes.OrderByDescending(a => a.PrecoAtual),
                "Quantidade" => acoes.OrderBy(a => a.Quantidade),
                "quantidade_desc" => acoes.OrderByDescending(a => a.Quantidade),
                _ => acoes.OrderBy(a => a.Ticker), // Caso nenhum parâmetro seja passado, ordena pelo Ticker.
            };

            return View(await acoes.ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> ListarTodasAcoes()
        {
            // Busca na API externa todas as ações disponíveis.
            var tickers = await _brapi.ObterTodasAcoes();

            // Transforma a lista de tickers para o formato JSON que a biblioteca front-end espera (ex: Select2).
            var result = tickers.Select(t => new
            {
                id = t.Ticker,
                text = $"{t.Ticker} - {t.Nome}",
                logo = t.Logo
            });

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> BuscarAtivo(string acao)
        {
            // Busca na API externa os detalhes de um ativo específico.
            var ativo = await _brapi.ObterAtivo(acao);

            // Se o ativo não for encontrado na API externa, retorna um erro 404 (Not Found).
            if (ativo is null)
            {
                return NotFound(new { mensagem = "Ativo não encontrado." });
            }

            // Retorna os dados do ativo em formato JSON para o JavaScript.
            return Json(new
            {
                nome = ativo.Nome,
                precoAtual = ativo.PrecoAtual,
                logo = ativo.Logo
            });
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Ticker,Quantidade,PrecoMedio,DataCompra,PrecoAtual")]
            Acao novaAcao, [FromServices] IBrapi brapi)
        {
            var validacao = ValidarAcao(novaAcao);
            if (!validacao.IsValid)
            {
                ModelState.AddModelError(string.Empty, validacao.ErrorMessage);
                return View(novaAcao);
            }

            try

            {
                var usuarioId = ObterUsuarioId();


                var ativo = await brapi.ObterAtivo(novaAcao.Ticker);


                var valorNovaCompra = novaAcao.Quantidade * novaAcao.PrecoAtual;


                var acaoExistente = await _context.Acao
                    .FirstOrDefaultAsync(a => a.Ticker == novaAcao.Ticker && a.UsuarioId == usuarioId);


                if (acaoExistente != null)

                {
                    acaoExistente.Quantidade += novaAcao.Quantidade;

                    acaoExistente.PrecoAtual += valorNovaCompra;

                    acaoExistente.DataCompra = novaAcao.DataCompra;

                    acaoExistente.Logo = ativo.Logo;

                    acaoExistente.Nome = ativo.Nome;


                    _context.Update(acaoExistente);
                }

                else

                {
                    novaAcao.UsuarioId = usuarioId;

                    novaAcao.Nome = ativo.Nome;

                    novaAcao.Logo = ativo.Logo;

                    novaAcao.PrecoAtual = valorNovaCompra;


                    _context.Add(novaAcao);
                }


                var lancamento = new Lancamento

                {
                    Movimentacao = "Compra",

                    Produto = novaAcao.Ticker,

                    Quantidade = novaAcao.Quantidade,

                    ValorTotal = valorNovaCompra,

                    Data = novaAcao.DataCompra,

                    UsuarioId = usuarioId
                };

                _context.Add(lancamento);


                await _context.SaveChangesAsync();

                TempData["Sucesso"] = true;

                return RedirectToAction(nameof(Index));
            }

            catch (Exception)

            {
                ModelState.AddModelError(string.Empty, "Ocorreu um erro ao salvar a ação. Tente novamente.");

                return View(novaAcao);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var usuarioId = ObterUsuarioId();

            // Medida de segurança: Busca a ação garantindo que o ID e o ID do usuário correspondem.
            var acao = await _context.Acao.FirstOrDefaultAsync(a => a.Id == id && a.UsuarioId == usuarioId);

            if (acao == null) return NotFound();

            return View(acao);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, int quantidade, decimal preco)
        {
            var usuarioId = ObterUsuarioId();

            // Busca o ativo no banco, garantindo que ele pertence ao usuário logado.
            var acao = await _context.Acao.FirstOrDefaultAsync(a => a.Id == id && a.UsuarioId == usuarioId);

            // O 'out valorDaVenda' recebe o valor da venda se a validação passar.
            if (!ValidarOperacaoDeVenda(acao, quantidade, preco, out decimal valorDaVenda))
            {
                // Se a validação falhar, o método auxiliar já preparou a mensagem de erro no TempData.
                // Apenas precisamos redirecionar.
                return RedirectToAction(nameof(Index));
            }

            // Cria o registro da transação de venda com todos os detalhes.
            var lancamento = new Lancamento
            {
                Movimentacao = "Venda",
                Produto = acao.Ticker,
                Quantidade = quantidade,
                ValorTotal = valorDaVenda,
                Data = DateTime.Now,
                UsuarioId = usuarioId
            };
            _context.Lancamento.Add(lancamento);

            // Se a venda é da quantidade total de ações, remove o registro da carteira.
            if (quantidade == acao.Quantidade)
            {
                _context.Acao.Remove(acao);
            }
            else // Se a venda é parcial...
            {
                // ...apenas subtrai a quantidade e o valor correspondente.
                acao.Quantidade -= quantidade;
                acao.PrecoAtual -= valorDaVenda;
                _context.Acao.Update(acao);
            }

            // Salva todas as alterações (o Lançamento e a Ação) no banco de dados.
            await _context.SaveChangesAsync();
            TempData["Removido"] = true; // Define a mensagem de sucesso.

            return RedirectToAction(nameof(Index));
        }

        // ===============================================================
        // MÉTODOS PRIVADOS AUXILIARES
        // ===============================================================

        private (bool IsValid, string ErrorMessage) ValidarAcao(Acao acao)
        {
            if (acao.Quantidade <= 0)
            {
                return (false, "A quantidade deve ser maior que zero");
            }

            if (acao.PrecoAtual <= 0)
            {
                return (false, "O valor deve ser maior que zero");
            }

            // if (acao.DataCompra.Date > DateTime.Today || acao.DataCompra == DateTime.MinValue)
            // {
            //     return (false, "Selecione uma data válida");
            // }

            return (true, null);
        }

        /// <summary>
        /// Executa todas as validações necessárias para uma operação de venda de ativo.
        /// </summary>
        private bool ValidarOperacaoDeVenda(Acao acao, int quantidade, decimal preco, out decimal valorDaVenda)
        {
            // Inicializa o valor de saída.
            valorDaVenda = 0;

            // Validação de existência
            if (acao == null)
            {
                TempData["Erro"] = "Ativo não encontrado ou não pertence a você.";
                return false;
            }

            // Validação de entrada
            if (quantidade <= 0 || preco <= 0)
            {
                TempData["Erro"] = "A quantidade e o preço da venda devem ser maiores que zero.";
                return false;
            }

            // Validação de regra de negócio (Quantidade)
            if (quantidade > acao.Quantidade)
            {
                TempData["Erro"] =
                    $"Você não pode vender {quantidade} unidades de {acao.Ticker}, pois possui apenas {acao.Quantidade}.";
                return false;
            }

            // Calcula o valor total da venda para a próxima validação.
            valorDaVenda = quantidade * preco;

            // Validação de regra de negócio (Valor)
            if (valorDaVenda > acao.PrecoAtual)
            {
                TempData["Erro"] =
                    $"O valor da venda (R$ {valorDaVenda:N2}) não pode ser maior que o valor total do seu ativo (R$ {acao.PrecoAtual:N2}).";
                return false;
            }

            // Se passou por todas as verificações, a operação é válida.
            return true;
        }
    }
}