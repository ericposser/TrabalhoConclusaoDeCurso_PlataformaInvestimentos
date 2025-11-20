using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PlataformaInvestimentos.Models;

public class RendaFixa
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Emissor")]
    public string Emissor { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Data da Compra")]
    public DateTime DataCompra { get; set; }
    
    [DataType(DataType.Date)]
    [Display(Name = "Data de Vencimento")]
    public DateTime? DataVencimento { get; set; }

    [Required]
    [Display(Name = "Valor Total(R$)")]
    public decimal Valor { get; set; }

    [Required]
    [Display(Name = "Taxa (% do CDI)")]
    public double Taxa { get; set; }
    
    public int UsuarioId { get; set; }
    [ValidateNever]
    public Usuario Usuario { get; set; }
}