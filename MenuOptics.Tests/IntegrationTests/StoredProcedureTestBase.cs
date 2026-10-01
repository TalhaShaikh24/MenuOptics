using Dapper;
using MenuOptics.Data;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Transactions;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace MenuOptics.Tests.IntegrationTests
{
    public abstract class StoredProcedureTestBase
    {
        protected readonly string ConnectionString;
        protected readonly IConfiguration Configuration;
        protected readonly DapperContext DapperContext;

        protected int TestRestaurantId { get; private set; }
        protected int TestUserId { get; private set; }

        protected StoredProcedureTestBase()
        {
            var configBuilder = new ConfigurationBuilder();
            
            // Try loading from API appsettings if available
            var apiAppSettingsPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "MenuOptics.API", "appsettings.json");
            if (File.Exists(apiAppSettingsPath))
            {
                configBuilder.AddJsonFile(apiAppSettingsPath, optional: true);
            }

            var inMemorySettings = new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Data Source=localhost\\MSSQLSERVER01;Initial Catalog=MenuOptics;Integrated Security=True;TrustServerCertificate=True" }
            };
            configBuilder.AddInMemoryCollection(inMemorySettings);

            Configuration = configBuilder.Build();
            ConnectionString = Configuration.GetConnectionString("DefaultConnection") 
                ?? "Data Source=localhost\\MSSQLSERVER01;Initial Catalog=MenuOptics;Integrated Security=True;TrustServerCertificate=True";

            DapperContext = new DapperContext(Configuration);

            TestUserId = 28730;
            TestRestaurantId = 28730;
        }

        /// <summary>
        /// Executes a test delegate inside an isolated TransactionScope with a SqlConnection.
        /// When the action completes, the scope is disposed WITHOUT calling Complete(),
        /// ensuring 100% rollback so database records are NEVER permanently modified.
        /// </summary>
        protected async Task RunInRollbackScopeAsync(Func<SqlConnection, Task> testAction)
        {
            using var scope = new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
                TransactionScopeAsyncFlowOption.Enabled);

            using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            await testAction(connection);

            // =========================================================================================
            // ROLLBACK / COMMIT SWITCH:
            // - Filhal scope.Complete() chalu hai taake actual DB me records save (commit) hon.
            // - Jab wapas Rollback chahiye ho, to sirf neeche wali line ko comment (//) kar dein:
            // =========================================================================================
            try { scope.Complete(); } catch (TransactionException) { /* negative tests ignore */ }
        }

        /// <summary>
        /// Executes a test delegate inside an isolated TransactionScope (e.g. For testing Service layer methods).
        /// </summary>
        protected async Task RunInRollbackScopeAsync(Func<Task> testAction)
        {
            using var scope = new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
                TransactionScopeAsyncFlowOption.Enabled);

            await testAction();

            // =========================================================================================
            // ROLLBACK / COMMIT SWITCH:
            // - Filhal scope.Complete() chalu hai taake actual DB me records save (commit) hon.
            // - Jab wapas Rollback chahiye ho, to sirf neeche wali line ko comment (//) kar dein:
            // =========================================================================================
            try { scope.Complete(); } catch (TransactionException) { /* negative tests ignore */ }
        }
    }
}
