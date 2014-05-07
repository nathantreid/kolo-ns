﻿using NPoco;

namespace Kolo.Core.Models
{
    [TableName("dns_entries")]
    [PrimaryKey("Id")]
    public class DnsEntry
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string IpV4 { get; set; }
        public string Name { get; set; }
    }
}