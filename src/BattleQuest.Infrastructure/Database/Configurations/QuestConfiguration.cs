using BattleQuest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BattleQuest.Infrastructure.Database.Configurations
{
	public class QuestConfiguration : IEntityTypeConfiguration<Quest>
	{
		public void Configure(EntityTypeBuilder<Quest> builder)
		{
			builder.ToTable("quests");

			// Keys
			builder.HasKey(q => q.QuestId);

			builder.Property(q => q.QuestId)
				.IsRequired()
				.HasColumnName("quest_id")
				.HasColumnType("varchar(20)");

			// Properties
			builder.Property(q => q.Name)
				.HasColumnName("name")
				.HasColumnType("varchar(100)");
		}
	}
}
