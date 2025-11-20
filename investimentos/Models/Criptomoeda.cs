using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PlataformaInvestimentos.Models;

public class Criptomoeda
{
    [Key]
    public int Id { get; set; }

    [Required, StringLength(20)]
    public string Ticker { get; set; }

    [Required, StringLength(100)]
    public string Nome { get; set; }

    [Required]
    public decimal Quantidade { get; set; }

    [Required]
    public decimal PrecoAtual { get; set; }

    [Required]
    public DateTime DataCompra { get; set; }

    [ForeignKey("Usuario")]
    public int UsuarioId { get; set; }
    [ValidateNever]
    public virtual Usuario Usuario { get; set; }
}