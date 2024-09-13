using BlazorAut.Data;
using Blazored.LocalStorage;
using Blazored.SessionStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.JSInterop;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
     private readonly ApplicationDbContext _context;
    private readonly string _secretKey;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly int _tokenExpirationDays;
    private readonly IJSRuntime _jsRuntime;
    private readonly DotNetObjectReference<CustomAuthenticationStateProvider> _dotNetRef;


    public CustomAuthenticationStateProvider(IServiceScopeFactory serviceScopeFactory, ApplicationDbContext context, IHttpContextAccessor httpContextAccessor,
       IJSRuntime iJSRuntime, string secretKey, int tokenExpirationDays)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _context = context;
        _secretKey = secretKey;
        _tokenExpirationDays = tokenExpirationDays;
        _httpContextAccessor= httpContextAccessor;
        _jsRuntime = iJSRuntime;
        _dotNetRef = DotNetObjectReference.Create(this);
    }



    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var scopedContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var identity = new ClaimsIdentity();
            // var token = await scopedContext.UserTokens.OrderByDescending(t => t.CreatedAt).FirstOrDefaultAsync();
            var tokenFromCookies = _httpContextAccessor.HttpContext.Request.Cookies.TryGetValue("Kticket000", out var cookieValue) ?  cookieValue : null;


            if (!string.IsNullOrEmpty(tokenFromCookies))
            {


                // Используем токен из cookies для поиска в базе данных
                var token = await scopedContext.UserTokens
                                .Where(t => t.Token == tokenFromCookies)
                                .OrderByDescending(t => t.CreatedAt)
                                .FirstOrDefaultAsync();

                if (token != null && token.Expiration > DateTime.UtcNow)
                {
                    var claims = ParseClaimsFromJwt(token.Token).ToList();


                    var user = await scopedContext.Users.SingleOrDefaultAsync(u => u.Id == token.UserId);
                    if (user != null)
                    {

                        claims.Add(new Claim(ClaimTypes.Name, user.Email));
                        identity = new ClaimsIdentity(claims, "jwt");
                    }
                }
            }
            var userPrincipal = new ClaimsPrincipal(identity);
            return await Task.FromResult(new AuthenticationState(userPrincipal));
        }
    }



    public async Task MarkUserAsAuthenticated(string email)
    {
        var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            // Create a new user
            user = new User
            {
                Email = email,
                Tokens = new List<UserToken>()
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        // Generate JWT token
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_secretKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
            new Claim(ClaimTypes.Name, email)
            }),
            Expires = DateTime.UtcNow.AddDays(_tokenExpirationDays),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        // Save or update the token in the database
        var userToken = await _context.UserTokens.SingleOrDefaultAsync(ut => ut.UserId == user.Id);

        if (userToken == null)
        {
            userToken = new UserToken
            {
                UserId = user.Id,
                Token = tokenString,
                Expiration = tokenDescriptor.Expires.Value,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserTokens.Add(userToken);
        }
        else
        {
            tokenString=userToken.Token;
           // userToken.Token = tokenString;
            userToken.Expiration = tokenDescriptor.Expires.Value;
            userToken.CreatedAt = DateTime.UtcNow;

            _context.UserTokens.Update(userToken);
        }

        await _context.SaveChangesAsync();

        
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = tokenDescriptor.Expires,
            Secure = true,
            SameSite = SameSiteMode.Strict
        };
        //var dotNetHelper = DotNetObjectReference.Create(this);
        await _jsRuntime.InvokeVoidAsync("setCookie", "Kticket000", tokenString, _tokenExpirationDays, _dotNetRef);
        // _httpContextAccessor.HttpContext.Response.Cookies.Append("Kticket000", tokenString, cookieOptions);


        //NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    [JSInvokable]
    public async Task RefreshAuthenticationState()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
      
    }
    public async Task LogoutAsync()
    {
        var token = await _context.UserTokens.OrderByDescending(t => t.CreatedAt).FirstOrDefaultAsync();
        if (token != null)
        {
            _context.UserTokens.Remove(token);
            await _context.SaveChangesAsync();
        }

        var identity = new ClaimsIdentity();
        var user = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public async Task LogoutThisDeviceAsync()
    {
        // delete from this device
        await _jsRuntime.InvokeVoidAsync("deleteCookie", "Kticket000", _dotNetRef);

        // refresh
        var identity = new ClaimsIdentity();
        var user = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }




    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadToken(jwt) as JwtSecurityToken;
        return jsonToken?.Claims;
    }
    public void Dispose()
    {
        _dotNetRef.Dispose();
    }
}
