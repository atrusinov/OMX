﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OMX.Models
{
    public class Address
    {
        public Address()
        {
            this.Properties = new List<Property>();
        }
        public int Id { get; set; }        
        [Required]
        public string City { get; set; }      

        public ICollection<Property> Properties { get; set; }
    }
}
