using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

using NodaTime;

using PluralKit.Core;

namespace PluralKit.API
{
    public class PKControllerBase: ControllerBase
    {
        private readonly Guid _requestId = Guid.NewGuid();
        private readonly Regex _shortIdRegex = new Regex("^[a-z]{5}$");
        private readonly Regex _snowflakeRegex = new Regex("^[0-9]{17,19}$");

        protected readonly ApiConfig _config;
        protected readonly IDatabase _db;
        protected readonly ModelRepository _repo;

        public PKControllerBase(IServiceProvider svc)
        {
            _config = svc.GetRequiredService<ApiConfig>();
            _db = svc.GetRequiredService<IDatabase>();
            _repo = svc.GetRequiredService<ModelRepository>();
        }

        protected Task<PKSystem?> ResolveSystem(string systemRef)
        {
            if (systemRef == "@me")
            {
                HttpContext.Items.TryGetValue("SystemId", out var systemId);
                if (systemId == null)
                    throw Errors.GenericAuthError;
                return _repo.GetSystem((SystemId)systemId);
            }

            if (Guid.TryParse(systemRef, out var guid))
                return _repo.GetSystemByGuid(guid);

            if (_snowflakeRegex.IsMatch(systemRef))
                return _repo.GetSystemByAccount(ulong.Parse(systemRef));

            if (_shortIdRegex.IsMatch(systemRef))
                return _repo.GetSystemByHid(systemRef);

            return Task.FromResult<PKSystem?>(null);
        }

        protected Task<PKMember?> ResolveMember(string memberRef)
        {
            if (Guid.TryParse(memberRef, out var guid))
                return _repo.GetMemberByGuid(guid);

            if (_shortIdRegex.IsMatch(memberRef))
                return _repo.GetMemberByHid(memberRef);

            return Task.FromResult<PKMember?>(null);
        }

        protected Task<PKGroup?> ResolveGroup(string groupRef)
        {
            if (Guid.TryParse(groupRef, out var guid))
                return _repo.GetGroupByGuid(guid);

            if (_shortIdRegex.IsMatch(groupRef))
                return _repo.GetGroupByHid(groupRef);

            return Task.FromResult<PKGroup?>(null);
        }

        protected LookupContext ContextFor(PKSystem system)
        {
            HttpContext.Items.TryGetValue("SystemId", out var systemId);
            if (systemId == null) return LookupContext.ByNonOwner;
            return ((SystemId)systemId) == system.Id ? LookupContext.ByOwner : LookupContext.ByNonOwner;
        }

        protected LookupContext ContextFor(PKMember member)
        {
            HttpContext.Items.TryGetValue("SystemId", out var systemId);
            if (systemId == null) return LookupContext.ByNonOwner;
            return ((SystemId)systemId) == member.System ? LookupContext.ByOwner : LookupContext.ByNonOwner;
        }

        protected LookupContext ContextFor(PKGroup group)
        {
            HttpContext.Items.TryGetValue("SystemId", out var systemId);
            if (systemId == null) return LookupContext.ByNonOwner;
            return ((SystemId)systemId) == group.System ? LookupContext.ByOwner : LookupContext.ByNonOwner;
        }
    }
}