using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly Context _context;

        public UsersController(Context context)
        {
            _context = context;
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDTO>> GetUser(long id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user._converToUserDTO();
        }

        // GET: api/Usesr/Balance
        [HttpGet("Balance")]
        public async Task<ActionResult<UserBalanceDTO>> GetBalance()
        {
            
            User user = Utils.GetUserFromToken(_context, Request.Headers);
            if (user == null)
            {
                // would rather do this inside the GetUserFromToken function, but i don't know how to do that with this framework
                return Unauthorized();
            }

            if (!Utils.VerifyHeaders(Request.Headers, user))
            {
                return Unauthorized();
            }

            return user._convertToUserBalanceDTO();
            
        }

        // PUT: api/Balance

        [HttpPut("Balance")]
        public async Task<ActionResult<UserBalanceDTO>> PutBalance(string currency, long amount)
        {

            User user = Utils.GetUserFromToken(_context, Request.Headers);
            if (user == null)
            {
                // would rather do this inside the GetUserFromToken function, but i don't know how to do that with this framework
                return Unauthorized();
            }
            lock (OrdersController.tradeLock)
            {
                if (currency == "USD")
                {
                    user.dollars = amount;
                }else if (currency == "BTC")
                {
                    user.bitcoins = amount;
                }
                _context.Entry(user).State = EntityState.Modified;

                _context.SaveChanges();
               
            }
            return user._convertToUserBalanceDTO();
        }

        // POST: api/Users/Register
        [HttpPost("Register")]
        public async Task<ActionResult<UserDTO>> PostUser(string name)
        {
            
            string CreateToken()
            {
                var allChar = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                var random = new Random();
                var resultToken = new string(
                   Enumerable.Repeat(allChar, 8)
                   .Select(token => token[random.Next(token.Length)]).ToArray());

                return resultToken.ToString();
            }
            var user = new User
            {
                name = name,

            };

            user.token = CreateToken();

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user._converToUserDTO();
        } 



        private bool UserExists(long id)
        {
            return _context.Users.Any(e => e.id == id);
        }
    }
}
