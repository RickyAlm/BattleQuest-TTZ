using BattleQuest.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BattleQuest.Infrastructure.Database
{
	public class BattleQuestDbContext : DbContext
	{
		public BattleQuestDbContext(DbContextOptions<BattleQuestDbContext> options) : base(options)
		{
			
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.ApplyConfigurationsFromAssembly(typeof(BattleQuestDbContext).Assembly);
		}

		public DbSet<ActionType> ActionTypes { get; set; }
		public DbSet<Boss> Bosses { get; set; }
		public DbSet<Channel> Channels { get; set; }
		public DbSet<Event> Events { get; set; }
		public DbSet<Item> Items { get; set; }
		public DbSet<Player> Players { get; set; }
		public DbSet<Quest> Quests { get; set; }
		public DbSet<Zone> Zones { get; set; }
	}
}
