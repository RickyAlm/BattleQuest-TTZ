using BattleQuest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BattleQuest.Infrastructure.Database.Configurations
{
	public class ChannelConfiguration : IEntityTypeConfiguration<Channel>
	{
		public void Configure(EntityTypeBuilder<Channel> builder)
		{
			builder.ToTable("channels");

			// Keys
			builder.HasKey(c => c.ChannelId);

			builder.Property(c => c.ChannelId)
				.HasColumnName("channel_id")
				.ValueGeneratedOnAdd();

			// Properties
			builder.Property(c => c.Name)
				.HasColumnName("name")
				.IsRequired()
				.HasColumnType("varchar(30)");

			// Indexes
			builder.HasIndex(c => c.Name)
				.IsUnique();
		}
	}
}
