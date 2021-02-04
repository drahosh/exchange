using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication1.Models
{
    [Table("orders")]
    public class StandingOrder
    {
        public long id { get; set; }

        public long user_id { get; set; }   // don't know how to link them better, after lots of problems with db stopped caring

        [Column("is_buy")]
        public bool isBuy { get; set; }
        [Column("remaining_bitcoin_amount")]

        public long remainingBitcoinAmount { get; set; }
        [Column("total_bitcoin_amount")]

        public long totalBitcoinAmount { get; set; }
        [Column("dollar_rate")]

        public long dollarRate { get; set; }

        public string status { get; set; }  //LIVE, FULFILLED, CANCELLED

        public StandingOrder(long user_id, bool isBuy, long remainingBitcoinAmount, long totalBitcoinAmount, long dollarRate, 
            string status, DateTime createdAt, DateTime? doneAt = null)
        {
            this.user_id = user_id;
            this.isBuy = isBuy;
            this.remainingBitcoinAmount = remainingBitcoinAmount;
            this.totalBitcoinAmount = totalBitcoinAmount;
            this.dollarRate = dollarRate;
            this.status = status;
            //this.createdAt = createdAt;
            //this.doneAt = doneAt;
        }
        public StandingOrder()
        {
            // empty constructor - used for serialization
        }
    }

    public class MarketOrderDTO
    {
        public long quantity { get; set; }
        public decimal avg_price { get; set; }

        public MarketOrderDTO(long quantity, decimal avg_price)
        {
            this.quantity = quantity;
            this.avg_price = avg_price;
        }

    }
}
