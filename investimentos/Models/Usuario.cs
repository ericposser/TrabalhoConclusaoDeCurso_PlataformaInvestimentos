using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PlataformaInvestimentos.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Usuario
{
    [Key]
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Login { get; set; }

    [Required]
    public string Senha { get; set; }

    [NotMapped]
    [Required(ErrorMessage = "Confirme sua senha")]
    [Compare("SenhaTexto", ErrorMessage = "As senhas não coincidem.")]
    public string ConfirmarSenha { get; set; }
    
    [NotMapped]
    [Required(ErrorMessage = "A senha é obrigatória.")]
    [DataType(DataType.Password)]
    public string SenhaTexto { get; set; } 

    [ValidateNever]
    public virtual ICollection<Acao> Acoes { get; set; }
    [ValidateNever]
    public virtual ICollection<FundoImobiliario> FundosImobiliarios { get; set; }
    [ValidateNever]
    public virtual ICollection<Criptomoeda> Criptomoedas { get; set; }
}