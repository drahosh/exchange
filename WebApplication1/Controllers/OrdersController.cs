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
    public class OrdersController : ControllerBase
    {
        public static readonly object tradeLock = new object();

        private readonly Context _context;



        public OrdersController(Context context)
        {
            _context = context;
        }

        // GET: api/StandingOrders/5
        [HttpGet("{id}")]
        public async Task<ActionResult<StandingOrder>> GetOrder(long id)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
            {
                return NotFound();
            }
            User user = Utils.GetUserFromToken(_context, Request.Headers);
            if (user == null || user.id != order.user_id)
            {
                // would rather do this inside the GetUserFromToken function, but i don't know how to do that with this framework
                return Unauthorized();
            }

            var StandingOrder = await _context.Orders.FindAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            return order;
        }

        // POST: api/MarketOrders
        [HttpPost("MarketOrders")]
        public async Task<ActionResult<MarketOrderDTO>> PostMarketOrder(long quantity, string type)
        {
            // buy/sell quantity of bitcoin at lowest/highest price available
            User user = Utils.GetUserFromToken(_context, Request.Headers);
            if (user == null)
            {
                // would rather do this inside the GetUserFromToken function, but i don't know how to do that with this framework
                return Unauthorized();
            }
            bool isBuy;
            IQueryable<StandingOrder> query;
            switch (type)
            {
                case "BUY":
                    isBuy = true;
                    query = from o in _context.Orders
                            where (o.isBuy == false) && (o.status == "LIVE") && (o.user_id != user.id)
                            orderby o.dollarRate ascending
                            select o;
                    break;
                case "SELL":
                    isBuy = false;
                    query = from o in _context.Orders
                            where (o.isBuy == true) && (o.status == "LIVE") && (o.user_id != user.id)
                            orderby o.dollarRate descending
                            select o;
                    break;
                default:
                    return BadRequest("type must be `BUY` or `SELL`");
            }
            lock (OrdersController.tradeLock)
            {
                long remaining_quantity = quantity, money = 0;
                while (remaining_quantity > 0)
                {
                    StandingOrder standingOrder = query.FirstOrDefault();
                    if (standingOrder == null)
                    {
                        break;
                    }
                    User anotherUser = _context.Users.Find(standingOrder.user_id);
                    long canAfford;
                    if (isBuy)
                    {
                        canAfford = user.dollars / standingOrder.dollarRate; // integer division (rounding down)
                    }
                    else
                    {
                        canAfford = user.bitcoins;
                    }
                    if (canAfford == 0)
                    {
                        break;
                    }
                    long amount_trade = Math.Min(remaining_quantity, Math.Min(standingOrder.remainingBitcoinAmount, canAfford));
                    remaining_quantity -= amount_trade;
                    long money_trade = amount_trade * standingOrder.dollarRate;
                    money += money_trade;
                    standingOrder.remainingBitcoinAmount -= amount_trade;
                    if (standingOrder.remainingBitcoinAmount == 0)
                    {
                        standingOrder.status = "FULFILLED";
                    }
                    if (isBuy)
                    {
                        user.dollars -= money_trade;
                        user.bitcoins += amount_trade;
                        anotherUser.dollars += money_trade;
                    }
                    else
                    {
                        user.bitcoins -= amount_trade;
                        user.dollars += money_trade;
                        anotherUser.bitcoins += amount_trade;
                    }
                    _context.SaveChanges();
                }
                long total_amount_traded = quantity - remaining_quantity;
                decimal average_rate = total_amount_traded==0 ? 0 : Decimal.Divide(money,total_amount_traded);
                return new MarketOrderDTO(total_amount_traded, average_rate);
            }
        }

        // POST: api/Orders
        [HttpPost]
        public async Task<ActionResult<StandingOrder>> PostOrder(long quantity, string type, long limit_price)
        {
            // in this endpoint, we simultaneously create a standing order and try to fulfill it similarly to market order

            User user = Utils.GetUserFromToken(_context, Request.Headers);
            if (user == null)
            {
                // would rather do this inside the GetUserFromToken function, but i don't know how to do that with this framework
                return Unauthorized();
            }

            bool isBuy;
            IQueryable<StandingOrder> query;
            lock (OrdersController.tradeLock)
            {

                switch (type)
                {
                    case "BUY":
                        isBuy = true;
                        query = from o in _context.Orders
                                where (o.isBuy == false) && (o.status == "LIVE") && (o.dollarRate <= limit_price) && (o.user_id != user.id)
                                orderby o.dollarRate ascending
                                select o;
                        break;
                    case "SELL":
                        isBuy = false;
                        query = from o in _context.Orders
                                where (o.isBuy == true) && (o.status == "LIVE") && (o.dollarRate >= limit_price) && (o.user_id != user.id)
                                orderby o.dollarRate descending
                                select o;
                        break;
                    default:
                        return BadRequest("type must be `BUY` or `SELL`");
                }

                var debug = query.ToQueryString();

                if ((isBuy && quantity * limit_price > user.dollars) || (!isBuy && quantity > user.bitcoins))
                {
                    return BadRequest("Not enough assets on account for this order");
                }
                long remaining_quantity = quantity;
                while (remaining_quantity > 0)
                {
                    StandingOrder anotherOrder = query.FirstOrDefault();
                    if (anotherOrder == null)
                    {
                        break;
                    }
                    User anotherUser = _context.Users.Find(anotherOrder.user_id);
                    long amount_trade = Math.Min(anotherOrder.remainingBitcoinAmount, remaining_quantity);

                    remaining_quantity -= amount_trade;
                    long money_trade = amount_trade * anotherOrder.dollarRate;
                    // we trade at the limit of the older standing order (to mimic first doing market order up to limit and then posting a standing order)
                    if (isBuy)
                    {
                        user.bitcoins += amount_trade;
                        user.dollars -= money_trade;
                        anotherUser.dollars += money_trade;
                    }
                    else
                    {
                        user.bitcoins -= amount_trade;
                        user.dollars += money_trade;
                        anotherUser.bitcoins += amount_trade;
                    }
                    anotherOrder.remainingBitcoinAmount -= amount_trade;
                    if (anotherOrder.remainingBitcoinAmount == 0)
                    {
                        anotherOrder.status = "FULFILLED";
                    }
                    _context.SaveChanges();
                }

                StandingOrder order = order = new StandingOrder(user.id, isBuy, remaining_quantity, quantity, limit_price,
                                        remaining_quantity == 0 ? "FULFILLED" : "LIVE", DateTime.UtcNow);
                // reserve assets
                if (isBuy)
                {
                    user.dollars -= remaining_quantity * limit_price;
                }
                else
                {
                    user.bitcoins -= remaining_quantity;
                }
                _context.Orders.Add(order);
                _context.SaveChanges();

                return order;
            }
        }

        // DELETE: api/Orders/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<StandingOrder>> DeleteOrder(long id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            User user = _context.Users.Find(order.user_id);
            if (!Utils.VerifyHeaders(Request.Headers, user))
            {
                return Unauthorized();
            }
            if (order.isBuy)
            {
                user.dollars += order.remainingBitcoinAmount * order.dollarRate;
            }
            else
            {
                user.bitcoins += order.remainingBitcoinAmount;
            }
            order.status = "CANCELLED";
            await _context.SaveChangesAsync();

            return order;
        }

        private bool OrderExists(long id)
        {
            return _context.Orders.Any(e => e.id == id);
        }
    }
}
