using BattleQuest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BattleQuest.Infrastructure.Database.Configurations
{
	public class QuestConfiguration : IEntityTypeConfiguration<Quest>
	{
		public void Configure(EntityTypeBuilder<Quest> builder)
		{
			builder.ToTable("Quest");

			builder.HasKey(q => q.QuestId);

			builder.Property(q => q.QuestId)
				.IsRequired()
				.HasColumnType("varchar(20)");

			builder.Property(q => q.Name)
				.HasColumnType("varchar(100)");
		}
	}
}
