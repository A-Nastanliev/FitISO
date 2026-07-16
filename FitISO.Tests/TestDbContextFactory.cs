using FitISO.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace FitISO.Tests
{
    internal class TestDbContextFactory : IDbContextFactory<FitDbContext>
    {
        private readonly DbContextOptions<FitDbContext> _options;
        public TestDbContextFactory(DbContextOptions<FitDbContext> options) => _options = options;
        public FitDbContext CreateDbContext() => new FitDbContext(_options);
    }
}
