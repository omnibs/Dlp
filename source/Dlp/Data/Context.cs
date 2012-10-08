using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dlp
{
    using System.Data.Entity;
    using System.Transactions;

    using Dlp.Data;

    using LogPlayer.Master;

    class Context : DbContext
    {
        public Context():base("DLP")
        {   
        }
        static Context()
        {
            Database.SetInitializer(new ForceDropCreateDatabaseIfModelChanges<Context>());
        }

        public DbSet<LogEntry> Logs { get; set; }

        public DbSet<Batch> Batches { get; set; }

        public DbSet<BatchRunner> BatchRunners { get; set; }

        public DbSet<Stat> Stats { get; set; }
    }

    public class ForceDropCreateDatabaseIfModelChanges<TContext> : IDatabaseInitializer<TContext> where TContext : DbContext
    {
        public void InitializeDatabase(TContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            bool flag;
            using (new TransactionScope(TransactionScopeOption.Suppress))
                flag = context.Database.Exists();
            if (flag)
            {
                if (context.Database.CompatibleWithModel(true))
                    return;
                context.Database.ExecuteSqlCommand("ALTER DATABASE " + context.Database.Connection.Database + " SET SINGLE_USER WITH ROLLBACK IMMEDIATE");
                context.Database.Delete();
            }

            context.Database.Create();
            this.Seed(context);
            context.SaveChanges();
        }

        protected virtual void Seed(TContext context)
        {
        }
    }
}
