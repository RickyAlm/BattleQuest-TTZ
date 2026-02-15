using BattleQuest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BattleQuest.Infrastructure.Database.Configurations
{
	public class ItemConfiguration : IEntityTypeConfiguration<Item>
	{
		public void Configure(EntityTypeBuilder<Item> builder)
		{
			builder.ToTable("items");

			// Keys
			builder.HasKey(i => i.ItemId);

			builder.Property(i => i.ItemId)
				.HasColumnName("item_id")
				.ValueGeneratedOnAdd();

			// Properties
			builder.Property(i => i.Name)
				.HasColumnName("name")
				.IsRequired()
				.HasColumnType("varchar(100)");

			// Indexes
			builder.HasIndex(i => i.Name)
				.IsUnique();
		}
	}
}
