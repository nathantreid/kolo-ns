﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ARSoft.Tools.Net.Dns;
using Kolo.Core.DataAccess;
using Kolo.Core.DataAccess.Repositories;
using Kolo.Core.Models;

namespace Kolo.Core.Services
{
    public class DnsResolver : IDnsResolver
    {
        static readonly Dictionary<string, DnsEntry> Cache = new Dictionary<string, DnsEntry>();

        private readonly IDnsEntriesRepository dnsEntriesRepository;
        private readonly IUnitOfWorkProvider unitOfWorkProvider;
        private readonly IForwardingServersRepository forwardingServersRepository;

        public DnsResolver(IDnsEntriesRepository dnsEntriesRepository, IUnitOfWorkProvider unitOfWorkProvider, IForwardingServersRepository forwardingServersRepository)
        {
            this.dnsEntriesRepository = dnsEntriesRepository;
            this.unitOfWorkProvider = unitOfWorkProvider;
            this.forwardingServersRepository = forwardingServersRepository;
        }

        public DnsResolutionResult Resolve(DnsRequest dnsRequest)
        {
            var result = new DnsResolutionResult();
            using (var uow = unitOfWorkProvider.GetUnitOfWork())
            {
                result.DnsEntry = (dnsEntriesRepository.FindDnsEntry(uow, dnsRequest)
                                   ?? dnsEntriesRepository.FindDnsEntryWithWildcard(uow, dnsRequest))
                                   ?? CheckCache(dnsRequest)
                                  ?? ForwardRequest(dnsRequest);
            }

            return result;
        }

        private DnsEntry CheckCache(DnsRequest dnsRequest)
        {
            DnsEntry dnsEntry;
            return Cache.TryGetValue(dnsRequest.Name, out dnsEntry) ? dnsEntry : null;
        }

        private DnsEntry ForwardRequest(DnsRequest dnsRequest)
        {
            using (var uow = unitOfWorkProvider.GetUnitOfWork())
            {
                var recordType = ParseRecordType(dnsRequest.Type);
                var forwardingServers = forwardingServersRepository.GetAllForwardingServers(uow);
                foreach (var forwardingServer in forwardingServers)
                {
                    var dnsEntry = ForwardRequestToServer(dnsRequest, forwardingServer, recordType);
                    if (dnsEntry != null)
                    {
                        if (!Cache.ContainsKey(dnsEntry.Name))
                            Cache.Add(dnsEntry.Name, dnsEntry);
                        else
                            Cache[dnsEntry.Name] = dnsEntry;
                        return dnsEntry;
                    }
                }
                return null;
            }
        }


        private DnsEntry ForwardRequestToServer(DnsRequest dnsRequest, ForwardingServer forwardingServer, RecordType recordType)
        {
            var client = new DnsClient(IPAddress.Parse(forwardingServer.IpV4), 2000);
            var answer = client.Resolve(dnsRequest.Name, recordType);
            if (answer != null && answer.AnswerRecords.Any())
            {
                var record = answer.AnswerRecords.FirstOrDefault(r => r.RecordType == recordType);
                //if (record.RecordType != RecordType.A)
                //    return null;
                if (record == null)
                    return null;
                if (record.RecordType != RecordType.A)
                    return null;
                var dnsEntry = new DnsEntry() { Name = record.Name, IpV4 = ((ARecord)record).Address.ToString() };
                return dnsEntry;
            }
            return null;
        }

        private RecordType ParseRecordType(string type)
        {
            return (RecordType)Enum.Parse(typeof(RecordType), type);
        }
    }
}
