using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaInvestimentos.Models;
using Microsoft.AspNetCore.Identity;

namespace PlataformaInvestimentos.Controllers
{
   public class UsuarioController : UtilController
{
    private readonly Context _context;
    
    // Instância única do serviço de hashing de senhas para ser reutilizado.
    private readonly IPasswordHasher<Usuario> _passwordHasher;
    
    // Uma constante para o nome do cookie. Evita erros de digitação e centraliza o nome.
    private const string AuthScheme = "LoginCookie";

    public UsuarioController(Context context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<Usuario>();
    }
    
    [HttpGet]
    public IActionResult Login()
    {
        // Verifica se o usuário já tem um cookie de autenticação válido.
        if (User.Identity.IsAuthenticated)
        {
            // Se sim, redireciona para a página principal para não mostrar o login novamente.
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken] // Atributo de segurança contra ataques CSRF.
    public async Task<IActionResult> Login(string login, string senha)
    {
        // Busca o usuário no banco de dados pelo login fornecido.
        var usuario = await _context.Usuario.FirstOrDefaultAsync(u => u.Login == login);
        
        // 1. O usuário não foi encontrado (usuario == null) OU
        // 2. A senha fornecida não corresponde à senha hasheada no banco.
        if (usuario == null || _passwordHasher.VerifyHashedPassword(usuario, usuario.Senha, senha) != PasswordVerificationResult.Success)
        {
            // Se qualquer uma das condições for verdadeira, chama o método privado para retornar a View com erro.
            return GerarMensagemDeErroLogin();
        }

        // Se passou na verificação, chama o método privado para criar o cookie de login.
        await RealizarLoginAsync(usuario);
        return RedirectToAction("Index", "Home");
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        // Remove o cookie de autenticação do navegador do usuário.
        await HttpContext.SignOutAsync(AuthScheme);
        return RedirectToAction("Login", "Usuario");
    }
    
    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Login,SenhaTexto,ConfirmarSenha")] Usuario usuario)
    {
        // Verifica se já existe alguém com o mesmo nome de usuário.
        if (await _context.Usuario.AnyAsync(u => u.Login == usuario.Login))
        {
            ModelState.AddModelError("Login", "Este nome de usuário já está em uso.");
            return View(usuario);
        }

        // Transforma a senha digitada (texto puro) em um hash seguro.
        usuario.Senha = _passwordHasher.HashPassword(usuario, usuario.SenhaTexto);
        
        _context.Add(usuario);
        await _context.SaveChangesAsync();

        TempData["Cadastro"] = true;
        // Após cadastrar, já realiza o login para melhorar a experiência do usuário.
        await RealizarLoginAsync(usuario); 
        return RedirectToAction("Index", "Home");
    }
    
    public async Task<IActionResult> TrocarNome()
    {
        // Busca os dados completos do usuário que está logado no momento.
        var usuario = await ObterUsuarioLogadoAsync();
        if (usuario == null) return NotFound(); // Medida de segurança caso o usuário não seja encontrado.

        return View(usuario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TrocarNome(string novoNome)
    {
        var usuario = await ObterUsuarioLogadoAsync();

        // Valida se o novo nome é igual ao antigo ou se já está em uso por outra pessoa.
        if (novoNome == usuario.Login || await _context.Usuario.AnyAsync(u => u.Login == novoNome))
        {
            // Adiciona uma mensagem de erro específica para cada caso.
            ModelState.AddModelError("Login", novoNome == usuario.Login 
                ? "Digite um nome de usuário diferente." 
                : "Este nome de usuário já está em uso.");
            
            return View();
        }

        // Atribui o novo nome ao objeto do usuário.
        usuario.Login = novoNome;
        _context.Update(usuario);
        await _context.SaveChangesAsync();

        // Atualiza o cookie de login com o novo nome.
        await RealizarLoginAsync(usuario, isRefresh: true);

        TempData["Sucesso"] = "Nome atualizado com sucesso!";
        return RedirectToAction("Index", "Home");
    }

    public IActionResult TrocarSenha() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TrocarSenha(string novaSenha, string confirmarSenha)
    {
        // Validação básica para garantir que o usuário digitou a mesma senha nos dois campos.
        if (novaSenha != confirmarSenha)
        {
            ModelState.AddModelError("", "As senhas não coincidem.");
            return View();
        }
        
        var usuario = await ObterUsuarioLogadoAsync();
        // Gera o hash da nova senha.
        usuario.Senha = _passwordHasher.HashPassword(usuario, novaSenha);
        
        _context.Update(usuario);
        await _context.SaveChangesAsync();

        TempData["Sucesso"] = "Senha alterada com sucesso!";
        return RedirectToAction("Index", "Home");
    }

    // ===============================================================
    // MÉTODOS PRIVADOS AUXILIARES
    // ===============================================================

    /// <summary>
    /// Busca no banco de dados o usuário que está atualmente logado.
    /// </summary>
    private async Task<Usuario> ObterUsuarioLogadoAsync()
    {
        // Chama o método melhorado da classe base.
        var usuarioId = ObterUsuarioId();

        // Se o ID for nulo (usuário não logado ou claim inválido), não há o que buscar.
        if (usuarioId == null)
        {
            return null;
        }
        
        return await _context.Usuario.FindAsync(usuarioId);
    }

    /// <summary>
    /// Cria o cookie de autenticação para um determinado usuário.
    /// O parâmetro 'isRefresh' é usado para fazer logout antes de logar novamente,
    /// garantindo que as informações do cookie sejam atualizadas.
    /// </summary>
    private async Task RealizarLoginAsync(Usuario usuario, bool isRefresh = false)
    {
        if (isRefresh)
        {
            await HttpContext.SignOutAsync(AuthScheme);
        }
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Login)
        };

        var identity = new ClaimsIdentity(claims, AuthScheme);
        var principal = new ClaimsPrincipal(identity);

        // Realiza o "SignIn", que efetivamente cria e envia o cookie para o navegador.
        await HttpContext.SignInAsync(AuthScheme, principal);
    }
    
    /// <summary>
    /// Centraliza a criação da mensagem de erro e o retorno da View para falhas de login.
    /// </summary>
    private IActionResult GerarMensagemDeErroLogin()
    {
        ModelState.AddModelError("", "Usuário ou senha inválidos.");
        TempData["Erro"] = true;
        return View();
    }
}
}
