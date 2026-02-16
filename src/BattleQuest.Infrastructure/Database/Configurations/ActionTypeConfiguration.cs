using BattleQuest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BattleQuest.Infrastructure.Database.Configurations;

public class ActionTypeConfiguration : IEntityTypeConfiguration<ActionType>
{
	public void Configure(EntityTypeBuilder<ActionType> builder)
	{
		builder.ToTable("action_types");

		// Keys
		builder.HasKey(at => at.ActionTypeId);

		builder.Property(at => at.ActionTypeId)
			.HasColumnName("action_type_id")
			.ValueGeneratedOnAdd();

		// Properties
		builder.Property(at => at.Name)
			.HasColumnName("name")
			.IsRequired()
			.HasColumnType("varchar(50)");

		// Indexes
		builder.HasIndex(at => at.Name)
			.IsUnique();
	}
}
