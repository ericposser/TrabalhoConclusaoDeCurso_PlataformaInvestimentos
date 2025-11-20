using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PlataformaInvestimentos.Models;

public class FundoImobiliario
{
    [Key]
    public int Id { get; set; }

    [Required, StringLength(10)]
    public string Ticker { get; set; }

    [Required, StringLength(100)]
    public string Nome { get; set; }

    public string Logo { get; set; }

    [Required]
    public int Quantidade { get; set; }

    [Required]
    public decimal PrecoAtual { get; set; }

    [Required]
    public DateTime DataCompra { get; set; }

    [ForeignKey("Usuario")]
    public int UsuarioId { get; set; }
    [ValidateNever]
    public virtual Usuario Usuario { get; set; }
}