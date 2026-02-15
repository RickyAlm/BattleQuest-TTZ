using BattleQuest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BattleQuest.Infrastructure.Database.Configurations
{
	public class ActionTypeConfiguration : IEntityTypeConfiguration<ActionType>
	{
		public void Configure(EntityTypeBuilder<ActionType> builder)
		{
			builder.ToTable("ActionType");

			builder.HasKey(at => at.ActionTypeId);

			builder.Property(at => at.Name)
				.IsRequired()
				.HasColumnType("varchar(50)");

			builder.HasIndex(at => at.Name)
				.IsUnique();
		}
	}
}
