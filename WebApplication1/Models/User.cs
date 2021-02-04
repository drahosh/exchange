using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication1.Models
{
    [Table("users")]  //specifying here because otherwise c# would make table name 'Users' while psql can have only lower case table names
    public class User
    {
        public long id { get; set; }
        public string name { get; set; }
        public string token { get; set; }
        public long dollars { get; set; }  // not including dollars reserved for standing orders
        public long bitcoins { get; set; } // not including bitcoins reserved for standing orders

        public UserDTO _converToUserDTO()
        {
            return new UserDTO(this.id, this.name, this.token);
        }
        public UserBalanceDTO _convertToUserBalanceDTO()
        {
            return new UserBalanceDTO(this.dollars, this.bitcoins);
        }


    }

    public class UserDTO
    {
        public long id { get; }
        public string name { get; set; }
        public string token { get; }

        public UserDTO(long n_id, string n_name, string n_token)
        {
            id = n_id;
            name = n_name;
            token = n_token;
        }

    }
    public class UserBalanceDTO
    {
        public long dollars { get; set; }
        public long bitcoins { get; set; }

        public decimal dollar_equivalent
        {
            get
            {
                return Utils.GetBitcoinPrice(bitcoins);
            }
        }
        public UserBalanceDTO(long n_dollars, long n_bitcoins)
        {
            dollars = n_dollars;
            bitcoins = n_bitcoins;
        }
    }
}
