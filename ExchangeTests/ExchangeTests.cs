using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net.Http;
using System.Web.Http.Controllers;
using WebApplication1;
using WebApplication1.Controllers;
using WebApplication1.Models;

namespace ExchangeTests
{
    [TestClass]
    public class ExchangeTests
    {
        public class TestContext : Context
        {
            public TestContext(DbContextOptions<Context> options)
            : base(options)
            {
            }
            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.UseInMemoryDatabase("exchange");
            }
        }

        private (UsersController, OrdersController, HttpContext) setupControllers()
        {
            //sets up empty in-memory database 
            var httpContext = new DefaultHttpContext();
            var options = new DbContextOptionsBuilder<Context>()
                 .UseInMemoryDatabase("exchange")
                 .Options;
            var context = new TestContext(options);
            var userController = new UsersController(context){
                ControllerContext = new ControllerContext()
                {
                    HttpContext = httpContext,
                }
            };
            var ordersController = new OrdersController(context)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = httpContext,
                }
            };
            return (userController, ordersController, httpContext);
        }
        [TestMethod]
        public async System.Threading.Tasks.Task TestBalanceEdit()
        {

            (UsersController usersController, OrdersController ordersController, HttpContext httpContext) = setupControllers();

            var result = await usersController.PostUser("testUser");
            UserDTO user = result.Value;
            Console.WriteLine(user);
            Assert.AreEqual("testUser", user.name);
            httpContext.Request.Headers["token"] = user.token;
            await usersController.PutBalance("USD", 50);
            await usersController.PutBalance("BTC", 2);

            UserBalanceDTO balance = usersController.GetBalance().Result.Value;

            Assert.AreEqual(balance.dollars, 50);
            Assert.AreEqual(balance.bitcoins, 2);
            Assert.IsTrue(balance.dollar_equivalent > 0);


        }

        [TestMethod]
        public async System.Threading.Tasks.Task ComplexTest()
        {
            //taken from project definition

            (UsersController usersController, OrdersController ordersController, HttpContext httpContext) = setupControllers();
            
            //4 users set up balance            
            UserDTO a = (await usersController.PostUser("A")).Value;
            UserDTO b = (await usersController.PostUser("B")).Value;
            UserDTO c = (await usersController.PostUser("C")).Value;
            UserDTO d = (await usersController.PostUser("D")).Value;
            httpContext.Request.Headers["token"] = a.token;
            await usersController.PutBalance("BTC", 1);
            httpContext.Request.Headers["token"] = b.token;
            await usersController.PutBalance("BTC", 10);
            httpContext.Request.Headers["token"] = c.token;
            await usersController.PutBalance("USD", 250000);
            httpContext.Request.Headers["token"] = d.token;
            await usersController.PutBalance("USD", 300000);

            // user a tries to post order, doesn't have enough tokens, gets more tokens and posts order again, this time succesfully
            httpContext.Request.Headers["token"] = a.token;
            var ao1 = (await ordersController.PostOrder(10, "SELL", 10000)).Result as BadRequestObjectResult;
            Assert.AreEqual(400, ao1.StatusCode);
            Assert.AreEqual("Not enough assets on account for this order", ao1.Value);
            await usersController.PutBalance("BTC", 10);
            StandingOrder ao2 = (await ordersController.PostOrder(10, "SELL", 10000)).Value;

            // B makes a standing order too
            httpContext.Request.Headers["token"] = b.token;
            var bo1 = (await ordersController.PostOrder(10, "SELL", 20000)).Value;

            // C makes a market order - check average price, balance
            httpContext.Request.Headers["token"] = c.token;
            MarketOrderDTO cm1 = (await ordersController.PostMarketOrder(15, "BUY")).Value;
            Assert.AreEqual(13333.33, (double)cm1.avg_price,  0.004);  // there is no AreEqual for decimal
            UserBalanceDTO cb1 = (await usersController.GetBalance()).Value;
            Assert.AreEqual(15, cb1.bitcoins);
            Assert.AreEqual(50000, cb1.dollars);

            //check existing standing orders
            httpContext.Request.Headers["token"] = a.token;
            ao2 = (await ordersController.GetOrder(ao2.id)).Value;
            Assert.AreEqual("FULFILLED", ao2.status);
            Assert.AreEqual(0, ao2.remainingBitcoinAmount);

            httpContext.Request.Headers["token"] = b.token;
            bo1 = (await ordersController.GetOrder(bo1.id)).Value;
            Assert.AreEqual("LIVE", bo1.status);
            Assert.AreEqual(5, bo1.remainingBitcoinAmount);

            // D makes a standing buy order thats cheaper than what B is selling for, and then makes another one but doesn't have enough money remaining

            httpContext.Request.Headers["token"] = d.token;
            StandingOrder do1 = (await ordersController.PostOrder(20, "BUY", 10000)).Value;
            var do2 = (await ordersController.PostOrder(10, "BUY", 25000)).Result as BadRequestObjectResult;
            Assert.AreEqual(400, do2.StatusCode);
            Assert.AreEqual("Not enough assets on account for this order", do2.Value);

            // D then deletes the first order and remakes the second order, which is partially fulfilled

            do1 = (await ordersController.DeleteOrder(do1.id)).Value;
            Assert.AreEqual("CANCELLED", do1.status);
            StandingOrder do3 = (await ordersController.PostOrder(10, "BUY", 25000)).Value;
            Assert.AreEqual("LIVE", do3.status);
            Assert.AreEqual(5, do3.remainingBitcoinAmount);
            // since D bought 5 bitcoin at 20k (older standing order), they should have 100k less than at beginning, and onother 125k reserved their standing order
            UserBalanceDTO db = (await usersController.GetBalance()).Value;
            Assert.AreEqual(300000 - 100000 - 125000, db.dollars);
            Assert.AreEqual(5, db.bitcoins);

            // now checking balance and standing order status of B 
            httpContext.Request.Headers["token"] = b.token;
            bo1 = (await ordersController.GetOrder(bo1.id)).Value;
            Assert.AreEqual("FULFILLED", bo1.status);
            UserBalanceDTO bb = (await usersController.GetBalance()).Value;
            Assert.AreEqual(0, bb.bitcoins);
            Assert.AreEqual(200000, bb.dollars);
        }
    }
    

}
