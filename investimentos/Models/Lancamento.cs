using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PlataformaInvestimentos.Models;

public class Lancamento
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Display(Name = "Movimentação")]
    public string Movimentacao { get; set; } // Compra ou Venda

    [Required]
    [StringLength(100)]
    [Display(Name = "Produto")]
    public string Produto { get; set; }

    [Required]
    [Display(Name = "Quantidade")]
    public decimal Quantidade { get; set; }

    [Required]
    [Display(Name = "Valor Total")]
    public decimal ValorTotal { get; set; }
    
    [Display(Name = "Data")]
    public DateTime Data { get; set; }
    
    [ForeignKey("Usuario")]
    public int UsuarioId { get; set; }
    [ValidateNever]
    public virtual Usuario Usuario { get; set; }
}