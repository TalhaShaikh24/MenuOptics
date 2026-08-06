using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using MenuOptics.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Data;
using System.Security.Claims;

namespace MenuOptics.API.Middleware
{
    public class SubscriptionRequiredAttribute : TypeFilterAttribute
    {
        public SubscriptionRequiredAttribute()
        : base(typeof(SubscriptionRequiredFilter))
        {
        }
    }
}
