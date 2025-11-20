using Microsoft.EntityFrameworkCore;

namespace PlataformaInvestimentos.Models;

public class Context : DbContext
{
    public DbSet<Usuario> Usuario { get; set; }
    public DbSet<Acao> Acao { get; set; }
    public DbSet<FundoImobiliario> FundoImobiliario { get; set; }
    public DbSet<Criptomoeda> Criptomoeda { get; set; }
    
    public DbSet<Lancamento> Lancamento { get; set; }
    
    public DbSet<RendaFixa> RendaFixa { get; set; }
    
    public Context(DbContextOptions<Context> options) : base(options)
    {
    }
}