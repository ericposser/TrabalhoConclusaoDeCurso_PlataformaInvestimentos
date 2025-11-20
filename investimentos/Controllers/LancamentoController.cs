using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PlataformaInvestimentos.Models;
using X.PagedList;

namespace PlataformaInvestimentos.Controllers
{
    [Authorize]
    public class LancamentoController : UtilController
    {
        private readonly Context _context;

        public LancamentoController(Context context)
        {
            _context = context;
        }
        
        public async Task<IActionResult> Index(string sortOrder, int? page)
        {
            ViewBag.CurrentSort = sortOrder;

            ViewBag.MovSort = String.IsNullOrEmpty(sortOrder) ? "mov_desc" : "";
            ViewBag.ProdSort = sortOrder == "Produto" ? "produto_desc" : "Produto";
            ViewBag.QtdSort = sortOrder == "Quantidade" ? "quantidade_desc" : "Quantidade";
            ViewBag.TotalSort = sortOrder == "ValorTotal" ? "valortotal_desc" : "ValorTotal";
            ViewBag.DataSort = sortOrder == "Data" ? "data_desc" : "Data";

            var usuarioId = ObterUsuarioId();
            var lancamentos = _context.Lancamento
                .Where(l => l.UsuarioId == usuarioId);

            lancamentos = sortOrder switch
            {
                "mov_desc" => lancamentos.OrderByDescending(l => l.Movimentacao),
                "Produto" => lancamentos.OrderBy(l => l.Produto),
                "produto_desc" => lancamentos.OrderByDescending(l => l.Produto),
                "Quantidade" => lancamentos.OrderBy(l => l.Quantidade),
                "quantidade_desc" => lancamentos.OrderByDescending(l => l.Quantidade),
                "ValorTotal" => lancamentos.OrderBy(l => l.ValorTotal),
                "valortotal_desc" => lancamentos.OrderByDescending(l => l.ValorTotal),
                "Data" => lancamentos.OrderBy(l => l.Data),
                "data_desc" => lancamentos.OrderByDescending(l => l.Data),
                _ => lancamentos.OrderBy(l => l.Movimentacao)
            };

            int pageSize = 12;
            int pageNumber = page ?? 1;

            return View(await lancamentos.ToPagedListAsync(pageNumber, pageSize));
        }
    }
}
